using System.Collections.Concurrent;
using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// RAM-only conversation context for the current browser session.
/// No consent required; never persisted to disk or database.
/// </summary>
public sealed class EphemeralSessionContextService
{
    public const int MaxExchanges = 10;
    public static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(45);

    private readonly ConcurrentDictionary<string, SessionBucket> _sessions = new();

    public static string GenerateSessionId() => Guid.NewGuid().ToString("N");

    public IReadOnlyList<CompanionConversationTurn> GetRecentTurns(string userId, string sessionId)
    {
        if (!TryMakeKey(userId, sessionId, out var key))
            return Array.Empty<CompanionConversationTurn>();

        if (!_sessions.TryGetValue(key, out var bucket))
            return Array.Empty<CompanionConversationTurn>();

        if (IsExpired(bucket))
        {
            _sessions.TryRemove(key, out _);
            return Array.Empty<CompanionConversationTurn>();
        }

        bucket.Touch();
        return Flatten(bucket.Exchanges);
    }

    public void AppendTurn(string userId, string sessionId, string userMessage, string assistantMessage)
    {
        if (!TryMakeKey(userId, sessionId, out var key))
            return;

        var user = (userMessage ?? "").Trim();
        var assistant = (assistantMessage ?? "").Trim();
        if (user.Length == 0 && assistant.Length == 0)
            return;

        var bucket = _sessions.AddOrUpdate(
            key,
            _ => new SessionBucket(),
            (_, existing) =>
            {
                if (IsExpired(existing))
                    return new SessionBucket();
                existing.Touch();
                return existing;
            });

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

    public void Clear(string userId, string sessionId)
    {
        if (TryMakeKey(userId, sessionId, out var key))
            _sessions.TryRemove(key, out _);
    }

    private static bool TryMakeKey(string userId, string sessionId, out string key)
    {
        key = "";
        if (!UserIdSanitizer.TryNormalize(userId, out var safeUser))
            return false;
        if (!UserIdSanitizer.TryNormalize(sessionId, out var safeSession))
            return false;
        key = $"{safeUser}:{safeSession}";
        return true;
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

    private sealed class SessionBucket
    {
        public List<SessionExchange> Exchanges { get; } = new();
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
