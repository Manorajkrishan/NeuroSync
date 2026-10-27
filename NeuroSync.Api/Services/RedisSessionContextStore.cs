using System.Text.Json;
using NeuroSync.Core;
using StackExchange.Redis;

namespace NeuroSync.Api.Services;

/// <summary>
/// Shared session cache. Key is session id plus authenticated user id, with a 45-minute sliding TTL.
/// Stores recent turns, structured thread state, and short-lived response metadata for feedback validation.
/// </summary>
public sealed class RedisSessionContextStore : IEphemeralSessionContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisSessionContextStore> _logger;

    public RedisSessionContextStore(IConnectionMultiplexer redis, ILogger<RedisSessionContextStore> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public IReadOnlyList<CompanionConversationTurn> GetRecentTurns(string userId, string sessionId)
    {
        var session = ReadSession(userId, sessionId);
        return session == null
            ? Array.Empty<CompanionConversationTurn>()
            : Flatten(session.Exchanges);
    }

    public CompanionThreadState GetThreadState(string userId, string sessionId)
    {
        var session = ReadSession(userId, sessionId);
        return session?.ThreadState ?? new CompanionThreadState();
    }

    public void UpdateThreadState(string userId, string sessionId, CompanionThreadState state)
    {
        MutateSession(userId, sessionId, session =>
        {
            session.ThreadState = state ?? new CompanionThreadState();
            session.ThreadState.UpdatedAtUtc = DateTime.UtcNow;
        });
    }

    public void RegisterResponse(string userId, string sessionId, CompanionResponseMetadata metadata)
    {
        if (metadata == null || string.IsNullOrWhiteSpace(metadata.ResponseId))
            return;

        MutateSession(userId, sessionId, session =>
        {
            session.Responses.RemoveAll(x =>
                string.Equals(x.ResponseId, metadata.ResponseId, StringComparison.Ordinal));
            session.Responses.Add(metadata);
            while (session.Responses.Count > EphemeralSessionContextService.MaxResponseMetadata)
                session.Responses.RemoveAt(0);
        });
    }

    public CompanionResponseMetadata? GetResponseMetadata(
        string userId,
        string sessionId,
        string responseId)
    {
        if (string.IsNullOrWhiteSpace(responseId))
            return null;

        var session = ReadSession(userId, sessionId);
        return session?.Responses.LastOrDefault(x =>
            string.Equals(x.ResponseId, responseId, StringComparison.Ordinal));
    }

    public void AppendTurn(string userId, string sessionId, string userMessage, string assistantMessage)
    {
        var user = (userMessage ?? "").Trim();
        var assistant = (assistantMessage ?? "").Trim();
        if (user.Length == 0 && assistant.Length == 0)
            return;

        MutateSession(userId, sessionId, session =>
        {
            session.Exchanges.Add(new StoredExchange(user, assistant, DateTime.UtcNow));
            while (session.Exchanges.Count > EphemeralSessionContextService.MaxExchanges)
                session.Exchanges.RemoveAt(0);
        });
    }

    public void Clear(string userId, string sessionId)
    {
        if (!EphemeralSessionContextService.TryMakeStorageKey(userId, sessionId, out var key))
            return;
        try
        {
            _redis.GetDatabase().KeyDelete(RedisKey(key));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Session cache clear failed");
        }
    }

    public void ClearUser(string userId)
    {
        if (!UserIdSanitizer.TryNormalize(userId, out var safe))
            return;
        try
        {
            var prefix = $"ns:sess:{safe}:";
            foreach (var endpoint in _redis.GetEndPoints())
            {
                var server = _redis.GetServer(endpoint);
                if (!server.IsConnected || server.IsReplica)
                    continue;
                foreach (var key in server.Keys(pattern: prefix + "*"))
                    _redis.GetDatabase().KeyDelete(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Session cache user clear failed");
        }
    }

    private StoredSession? ReadSession(string userId, string sessionId)
    {
        if (!EphemeralSessionContextService.TryMakeStorageKey(userId, sessionId, out var key))
            return null;

        try
        {
            var db = _redis.GetDatabase();
            var redisKey = RedisKey(key);
            var raw = db.StringGet(redisKey);
            if (raw.IsNullOrEmpty)
                return null;
            db.KeyExpire(redisKey, EphemeralSessionContextService.SessionTtl);
            return Deserialize(raw!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Session cache read failed");
            return null;
        }
    }

    private void MutateSession(string userId, string sessionId, Action<StoredSession> mutate)
    {
        if (!EphemeralSessionContextService.TryMakeStorageKey(userId, sessionId, out var key))
            return;

        var redisKey = RedisKey(key);
        var lockKey = redisKey + ":lock";
        var token = Guid.NewGuid().ToString("N");

        try
        {
            var db = _redis.GetDatabase();
            if (!db.LockTake(lockKey, token, TimeSpan.FromSeconds(2)))
            {
                _logger.LogWarning("Session cache lock was busy");
                return;
            }

            try
            {
                var raw = db.StringGet(redisKey);
                var session = raw.IsNullOrEmpty ? new StoredSession() : Deserialize(raw!);
                mutate(session);
                db.StringSet(
                    redisKey,
                    JsonSerializer.Serialize(session, JsonOptions),
                    EphemeralSessionContextService.SessionTtl);
            }
            finally
            {
                db.LockRelease(lockKey, token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Session cache write failed");
        }
    }

    private static string RedisKey(string storageKey) => "ns:sess:" + storageKey;

    private static StoredSession Deserialize(string json)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<StoredSession>(json, JsonOptions);
            if (envelope != null)
            {
                envelope.Exchanges ??= new List<StoredExchange>();
                envelope.Responses ??= new List<CompanionResponseMetadata>();
                envelope.ThreadState ??= new CompanionThreadState();
                return envelope;
            }
        }
        catch (JsonException)
        {
            // Fall through to legacy array format.
        }

        try
        {
            var legacy = JsonSerializer.Deserialize<List<StoredExchange>>(json, JsonOptions)
                         ?? new List<StoredExchange>();
            return new StoredSession { Exchanges = legacy };
        }
        catch (JsonException)
        {
            return new StoredSession();
        }
    }

    private static IReadOnlyList<CompanionConversationTurn> Flatten(List<StoredExchange> exchanges)
    {
        var turns = new List<CompanionConversationTurn>();
        foreach (var ex in exchanges)
        {
            if (!string.IsNullOrWhiteSpace(ex.User))
                turns.Add(new CompanionConversationTurn { Role = "user", Text = ex.User, Timestamp = ex.At });
            if (!string.IsNullOrWhiteSpace(ex.Assistant))
                turns.Add(new CompanionConversationTurn { Role = "assistant", Text = ex.Assistant, Timestamp = ex.At });
        }
        return turns.Count <= 20 ? turns : turns.TakeLast(20).ToList();
    }

    private sealed class StoredSession
    {
        public List<StoredExchange> Exchanges { get; set; } = new();
        public CompanionThreadState ThreadState { get; set; } = new();
        public List<CompanionResponseMetadata> Responses { get; set; } = new();
    }

    private sealed record StoredExchange(string User, string Assistant, DateTime At);
}
