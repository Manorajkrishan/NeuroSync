using Microsoft.AspNetCore.SignalR;
using NeuroSync.Core;

namespace NeuroSync.Api.Hubs;

/// <summary>
/// SignalR hub for real-time emotion data transmission.
/// </summary>
public class EmotionHub : Hub
{
    public async Task SendEmotionResult(EmotionResult result)
    {
        await Clients.All.SendAsync("EmotionDetected", result);
    }

    public async Task SendAdaptiveResponse(AdaptiveResponse response)
    {
        await Clients.All.SendAsync("AdaptiveResponse", response);
    }

    public async Task SendIoTAction(IoTAction action)
    {
        await Clients.All.SendAsync("IoTAction", action);
    }

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


