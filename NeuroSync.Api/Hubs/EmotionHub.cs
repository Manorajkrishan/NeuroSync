using Microsoft.AspNetCore.SignalR;
using NeuroSync.Core;

namespace NeuroSync.Api.Hubs;

/// <summary>
/// SignalR hub for real-time companion updates — scoped per user group, never broadcast.
/// </summary>
public class EmotionHub : Hub
{
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;

    public EmotionHub(IConfiguration config, IHostEnvironment env)
    {
        _config = config;
        _env = env;
    }

    /// <summary>
    /// Client must call this after connect so EmotionDetected / AdaptiveResponse / IoTAction
    /// only reach that user's connections.
    /// </summary>
    public async Task JoinUserGroup(string userId)
    {
        var safe = ResolveUser(userId);
        await Groups.AddToGroupAsync(Context.ConnectionId, EmotionHubUserScope.GroupName(safe));
        await Clients.Caller.SendAsync("JoinedUserGroup", EmotionHubUserScope.GroupName(safe));
    }

    private string ResolveUser(string requested)
    {
        var require = NeuroSync.Api.Auth.AuthPolicy.RequireAuthentication(_config, _env);
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!require)
        {
            if (!UserIdSanitizer.TryNormalize(requested, out var safe))
                throw new HubException("Invalid userId");
            return safe;
        }

        if (string.IsNullOrEmpty(claim))
            throw new HubException("Sign in required.");
        if (!string.IsNullOrWhiteSpace(requested) && !string.Equals(requested, claim, StringComparison.Ordinal))
            throw new HubException("userId does not match the signed-in account.");
        return claim;
    }

    public async Task LeaveUserGroup(string userId)
    {
        string safe;
        try
        {
            safe = ResolveUser(userId);
        }
        catch (HubException)
        {
            return;
        }

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
