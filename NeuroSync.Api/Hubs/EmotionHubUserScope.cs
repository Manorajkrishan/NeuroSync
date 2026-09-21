using Microsoft.AspNetCore.SignalR;
using NeuroSync.Core;

namespace NeuroSync.Api.Hubs;

/// <summary>
/// Per-user SignalR isolation. NEVER use Clients.All for emotional / adaptive / IoT payloads.
/// Clients join group via EmotionHub.JoinUserGroup(userId).
/// </summary>
public static class EmotionHubUserScope
{
    public static string GroupName(string userId)
    {
        var safe = UserIdSanitizer.NormalizeOrDefault(userId);
        return $"user:{safe}";
    }

    /// <summary>
    /// Send to the authenticated/session user's group only — proves scoping for tests/callers.
    /// </summary>
    public static Task SendToUserAsync(
        IHubContext<EmotionHub> hub,
        string userId,
        string method,
        object? arg,
        CancellationToken cancellationToken = default)
    {
        // Explicit: Group(user:…) — not Clients.All
        return hub.Clients.Group(GroupName(userId)).SendAsync(method, arg, cancellationToken);
    }

    public static async Task SendToUserAsync(
        IHubContext<EmotionHub> hub,
        string userId,
        string method,
        object? arg1,
        object? arg2,
        CancellationToken cancellationToken = default)
    {
        await hub.Clients.Group(GroupName(userId)).SendAsync(method, arg1, arg2, cancellationToken);
    }
}
