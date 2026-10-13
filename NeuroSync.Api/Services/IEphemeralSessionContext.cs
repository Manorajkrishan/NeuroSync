using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Short-lived companion turns keyed by authenticated user and session.
/// Not personal memory. Default lifetime is 45 minutes.
/// </summary>
public interface IEphemeralSessionContext
{
    IReadOnlyList<CompanionConversationTurn> GetRecentTurns(string userId, string sessionId);
    void AppendTurn(string userId, string sessionId, string userMessage, string assistantMessage);
    void Clear(string userId, string sessionId);
    void ClearUser(string userId);
}
