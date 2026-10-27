using System.Collections.Concurrent;
using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// RAM-only conversation context for the current browser session.
/// No consent required; never persisted to disk or database.
/// </summary>
public sealed class EphemeralSessionContextService : IEphemeralSessionContext
{
    public const int MaxExchanges = 10;
    public const int MaxResponseMetadata = 12;
    public static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(45);

    private readonly ConcurrentDictionary<string, SessionBucket> _sessions = new();

    public static string GenerateSessionId() => Guid.NewGuid().ToString("N");

    public IReadOnlyList<CompanionConversationTurn> GetRecentTurns(string userId, string sessionId)
    {
        if (!TryGetActiveBucket(userId, sessionId, out _, out var bucket))
            return Array.Empty<CompanionConversationTurn>();

        lock (bucket.Gate)
        {
            bucket.Touch();
            return Flatten(bucket.Exchanges.ToList());
        }
    }

    public CompanionThreadState GetThreadState(string userId, string sessionId)
    {
        if (!TryGetActiveBucket(userId, sessionId, out _, out var bucket))
            return new CompanionThreadState();

        lock (bucket.Gate)
        {
            bucket.Touch();
            return Clone(bucket.ThreadState);
        }
    }

    public void UpdateThreadState(string userId, string sessionId, CompanionThreadState state)
    {
        if (!TryMakeStorageKey(userId, sessionId, out var key))
            return;

        var bucket = _sessions.GetOrAdd(key, _ => new SessionBucket());
        lock (bucket.Gate)
        {
            ResetIfExpired(bucket);
            bucket.ThreadState = Clone(state);
            bucket.ThreadState.UpdatedAtUtc = DateTime.UtcNow;
            bucket.Touch();
        }
    }

    public void RegisterResponse(string userId, string sessionId, CompanionResponseMetadata metadata)
    {
        if (metadata == null || string.IsNullOrWhiteSpace(metadata.ResponseId))
            return;
        if (!TryMakeStorageKey(userId, sessionId, out var key))
            return;

        var bucket = _sessions.GetOrAdd(key, _ => new SessionBucket());
        lock (bucket.Gate)
        {
            ResetIfExpired(bucket);
            bucket.Responses.RemoveAll(x => string.Equals(
                x.ResponseId, metadata.ResponseId, StringComparison.Ordinal));
            bucket.Responses.Add(Clone(metadata));
            while (bucket.Responses.Count > MaxResponseMetadata)
                bucket.Responses.RemoveAt(0);
            bucket.Touch();
        }
    }

    public CompanionResponseMetadata? GetResponseMetadata(
        string userId,
        string sessionId,
        string responseId)
    {
        if (string.IsNullOrWhiteSpace(responseId))
            return null;
        if (!TryGetActiveBucket(userId, sessionId, out _, out var bucket))
            return null;

        lock (bucket.Gate)
        {
            bucket.Touch();
            var found = bucket.Responses.LastOrDefault(x =>
                string.Equals(x.ResponseId, responseId, StringComparison.Ordinal));
            return found == null ? null : Clone(found);
        }
    }

    public void AppendTurn(string userId, string sessionId, string userMessage, string assistantMessage)
    {
        if (!TryMakeStorageKey(userId, sessionId, out var key))
            return;

        var user = (userMessage ?? "").Trim();
        var assistant = (assistantMessage ?? "").Trim();
        if (user.Length == 0 && assistant.Length == 0)
            return;

        var bucket = _sessions.GetOrAdd(key, _ => new SessionBucket());
        lock (bucket.Gate)
        {
            ResetIfExpired(bucket);
            bucket.Touch();
            bucket.Exchanges.Add(new SessionExchange
            {
                UserMessage = user,
                AssistantMessage = assistant,
                Timestamp = DateTime.UtcNow
            });

            while (bucket.Exchanges.Count > MaxExchanges)
                bucket.Exchanges.RemoveAt(0);
        }
    }

    public void Clear(string userId, string sessionId)
    {
        if (TryMakeStorageKey(userId, sessionId, out var key))
            _sessions.TryRemove(key, out _);
    }

    public void ClearUser(string userId)
    {
        if (!UserIdSanitizer.TryNormalize(userId, out var safe))
            return;
        var prefix = safe + ":";
        foreach (var key in _sessions.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
                _sessions.TryRemove(key, out _);
        }
    }

    public static bool TryMakeStorageKey(string userId, string sessionId, out string key)
    {
        key = "";
        if (!UserIdSanitizer.TryNormalize(userId, out var safeUser))
            return false;
        if (!UserIdSanitizer.TryNormalize(sessionId, out var safeSession))
            return false;
        key = $"{safeUser}:{safeSession}";
        return true;
    }

    private bool TryGetActiveBucket(
        string userId,
        string sessionId,
        out string key,
        out SessionBucket bucket)
    {
        bucket = null!;
        if (!TryMakeStorageKey(userId, sessionId, out key))
            return false;

        if (!_sessions.TryGetValue(key, out bucket!))
            return false;

        lock (bucket.Gate)
        {
            if (IsExpired(bucket))
            {
                _sessions.TryRemove(new KeyValuePair<string, SessionBucket>(key, bucket));
                bucket = null!;
                return false;
            }
        }

        return true;
    }

    private static void ResetIfExpired(SessionBucket bucket)
    {
        if (!IsExpired(bucket))
            return;

        bucket.Exchanges.Clear();
        bucket.Responses.Clear();
        bucket.ThreadState = new CompanionThreadState();
        bucket.Touch();
    }

    private static bool IsExpired(SessionBucket bucket) =>
        DateTime.UtcNow - bucket.LastAccessUtc > SessionTtl;

    private static IReadOnlyList<CompanionConversationTurn> Flatten(List<SessionExchange> exchanges)
    {
        if (exchanges.Count == 0)
            return Array.Empty<CompanionConversationTurn>();

        var turns = new List<CompanionConversationTurn>();
        foreach (var ex in exchanges)
        {
            if (!string.IsNullOrWhiteSpace(ex.UserMessage))
            {
                turns.Add(new CompanionConversationTurn
                {
                    Role = "user",
                    Text = ex.UserMessage,
                    Timestamp = ex.Timestamp
                });
            }

            if (!string.IsNullOrWhiteSpace(ex.AssistantMessage))
            {
                turns.Add(new CompanionConversationTurn
                {
                    Role = "assistant",
                    Text = ex.AssistantMessage,
                    Timestamp = ex.Timestamp
                });
            }
        }

        return turns.Count <= 20 ? turns : turns.TakeLast(20).ToList();
    }

    private static CompanionThreadState Clone(CompanionThreadState state) => new()
    {
        Topic = state.Topic,
        PendingFollowUp = state.PendingFollowUp,
        LastIntent = state.LastIntent,
        LastMode = state.LastMode,
        TurnCount = state.TurnCount,
        UpdatedAtUtc = state.UpdatedAtUtc
    };

    private static CompanionResponseMetadata Clone(CompanionResponseMetadata metadata) => new()
    {
        ResponseId = metadata.ResponseId,
        ProviderId = metadata.ProviderId,
        Intent = metadata.Intent,
        Mode = metadata.Mode,
        Locale = metadata.Locale,
        QualityScore = metadata.QualityScore,
        CreatedAtUtc = metadata.CreatedAtUtc
    };

    private sealed class SessionBucket
    {
        public object Gate { get; } = new();
        public List<SessionExchange> Exchanges { get; } = new();
        public List<CompanionResponseMetadata> Responses { get; } = new();
        public CompanionThreadState ThreadState { get; set; } = new();
        public DateTime LastAccessUtc { get; private set; } = DateTime.UtcNow;

        public void Touch() => LastAccessUtc = DateTime.UtcNow;
    }

    private sealed class SessionExchange
    {
        public string UserMessage { get; set; } = "";
        public string AssistantMessage { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
