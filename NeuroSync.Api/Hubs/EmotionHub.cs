using Microsoft.AspNetCore.SignalR;
using NeuroSync.Core;

namespace NeuroSync.Api.Hubs;

/// <summary>
/// SignalR hub for real-time companion updates — scoped per user group, never broadcast.
/// </summary>
public class EmotionHub : Hub
{
    /// <summary>
    /// Client must call this after connect so EmotionDetected / AdaptiveResponse / IoTAction
    /// only reach that user's connections.
    /// </summary>
    public async Task JoinUserGroup(string userId)
    {
        if (!UserIdSanitizer.TryNormalize(userId, out var safe))
            throw new HubException("Invalid userId");

        await Groups.AddToGroupAsync(Context.ConnectionId, EmotionHubUserScope.GroupName(safe));
        await Clients.Caller.SendAsync("JoinedUserGroup", EmotionHubUserScope.GroupName(safe));
    }

    public async Task LeaveUserGroup(string userId)
    {
        if (!UserIdSanitizer.TryNormalize(userId, out var safe))
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, EmotionHubUserScope.GroupName(safe));
    }

    // Deprecated client-callable broadcast APIs removed — use server-side EmotionHubUserScope.

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
