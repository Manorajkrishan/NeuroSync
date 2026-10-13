using System.Text.Json;
using NeuroSync.Core;
using StackExchange.Redis;

namespace NeuroSync.Api.Services;

/// <summary>
/// Shared session cache. Key is session id plus authenticated user id, with a 45-minute sliding TTL.
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
        if (!EphemeralSessionContextService.TryMakeStorageKey(userId, sessionId, out var key))
            return Array.Empty<CompanionConversationTurn>();

        try
        {
            var db = _redis.GetDatabase();
            var raw = db.StringGet(RedisKey(key));
            if (raw.IsNullOrEmpty)
                return Array.Empty<CompanionConversationTurn>();
            db.KeyExpire(RedisKey(key), EphemeralSessionContextService.SessionTtl);
            return Flatten(Read(raw!));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Session cache read failed");
            return Array.Empty<CompanionConversationTurn>();
        }
    }

    public void AppendTurn(string userId, string sessionId, string userMessage, string assistantMessage)
    {
        if (!EphemeralSessionContextService.TryMakeStorageKey(userId, sessionId, out var key))
            return;

        var user = (userMessage ?? "").Trim();
        var assistant = (assistantMessage ?? "").Trim();
        if (user.Length == 0 && assistant.Length == 0)
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
                var exchanges = db.StringGet(redisKey).IsNullOrEmpty
                    ? new List<StoredExchange>()
                    : Read(db.StringGet(redisKey)!);
                exchanges.Add(new StoredExchange(user, assistant, DateTime.UtcNow));
                while (exchanges.Count > EphemeralSessionContextService.MaxExchanges)
                    exchanges.RemoveAt(0);
                db.StringSet(redisKey, JsonSerializer.Serialize(exchanges, JsonOptions), EphemeralSessionContextService.SessionTtl);
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

    private static string RedisKey(string storageKey) => "ns:sess:" + storageKey;

    private static List<StoredExchange> Read(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<StoredExchange>>(json, JsonOptions) ?? new List<StoredExchange>();
        }
        catch (JsonException)
        {
            return new List<StoredExchange>();
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

    private sealed record StoredExchange(string User, string Assistant, DateTime At);
}
