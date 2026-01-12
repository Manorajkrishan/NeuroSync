using NeuroSync.Core;
using NeuroSync.IoT.Configuration;
using NeuroSync.IoT.Interfaces;
using NeuroSync.IoT.Services;

namespace NeuroSync.IoT;

/// <summary>
/// Real IoT controller that integrates with actual devices and services.
/// Falls back to simulation if no services are configured.
/// </summary>
public class RealIoTController
{
    private readonly MusicServiceManager? _musicServiceManager;
    private readonly SmartLightService? _smartLightService;
    private readonly IoTConfig? _config;
    private readonly Action<string>? _logger;

    public RealIoTController(
        MusicServiceManager? musicServiceManager = null,
        SmartLightService? smartLightService = null,
        IoTConfig? config = null,
        Action<string>? logger = null)
    {
        _musicServiceManager = musicServiceManager;
        _smartLightService = smartLightService;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Executes IoT actions on real devices.
    /// </summary>
    public async Task<bool> ExecuteActionAsync(IoTAction action)
    {
        try
        {
            switch (action.ActionType.ToLower())
            {
                case "playmusic":
                    return await ExecutePlayMusicAsync(action);
                
                case "setcolor":
                    return await ExecuteSetColorAsync(action);
                
                case "breathing":
                case "pulse":
                    return await ExecuteLightEffectAsync(action);
                
                case "showmessage":
                    return await ExecuteShowMessageAsync(action);
                
                default:
                    _logger?.Invoke($"RealIoTController: Unknown action type: {action.ActionType}");
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"RealIoTController: Error executing action - {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecutePlayMusicAsync(IoTAction action)
    {
        if (_musicServiceManager == null)
        {
            _logger?.Invoke("RealIoTController: Music service manager not configured - simulating");
            return false;
        }

        var genre = action.Parameters?.ContainsKey("genre") == true ? action.Parameters["genre"]?.ToString() ?? "" : "";
        var playlist = action.Parameters?.ContainsKey("playlist") == true ? action.Parameters["playlist"]?.ToString() ?? "" : "";
        var volume = action.Parameters?.ContainsKey("volume") == true && int.TryParse(action.Parameters["volume"]?.ToString(), out var vol) ? vol : 50;

        // Get the preferred service or best available
        IMusicService? service = null;
        if (_config?.PreferredMusicService != "Auto" && _config?.PreferredMusicService != null)
        {
            service = _musicServiceManager.GetService(_config.PreferredMusicService);
        }
        else
        {
            service = await _musicServiceManager.GetBestAvailableServiceAsync();
        }

        if (service == null)
        {
            _logger?.Invoke("RealIoTController: No music service available - simulating");
            return false;
        }

        // For YouTube Music, get the playlist URL
        if (service.ServiceName == "YouTube Music")
        {
            // Use reflection to call PlayMusicWithUrlAsync since it's not in the interface
            var method = service.GetType().GetMethod("PlayMusicWithUrlAsync");
            if (method != null)
            {
                var task = (Task<(bool Success, string? PlaylistUrl)>)method.Invoke(service, new object[] { action.DeviceId, genre, playlist, volume })!;
                var (success, playlistUrl) = await task;
                if (success && !string.IsNullOrEmpty(playlistUrl) && action.Parameters != null)
                {
                    // Add URL to action parameters so frontend can display it
                    action.Parameters["playlistUrl"] = playlistUrl;
                    action.Parameters["note"] = "Click the link below to play on YouTube Music (API limitation - cannot auto-play)";
                }
                return success;
            }
        }

        return await service.PlayMusicAsync(action.DeviceId, genre, playlist, volume);
    }

    private async Task<bool> ExecuteSetColorAsync(IoTAction action)
    {
        if (_smartLightService == null)
        {
            _logger?.Invoke("RealIoTController: Smart light service not configured - simulating");
            return false;
        }

        var color = action.Parameters?.ContainsKey("color") == true ? action.Parameters["color"]?.ToString() ?? "" : "";
        var brightness = action.Parameters?.ContainsKey("brightness") == true && int.TryParse(action.Parameters["brightness"]?.ToString(), out var bright) ? bright : 50;

        return await _smartLightService.ControlLightAsync(action.DeviceId, color, brightness);
    }

    private async Task<bool> ExecuteLightEffectAsync(IoTAction action)
    {
        // Light effects are handled similarly to setColor but with effects
        // For now, just log
        _logger?.Invoke($"RealIoTController: Light effect {action.ActionType} for {action.DeviceId} - requires device-specific implementation");
        return false;
    }

    private async Task<bool> ExecuteShowMessageAsync(IoTAction action)
    {
        var message = action.Parameters?.ContainsKey("message") == true ? action.Parameters["message"]?.ToString() ?? "" : "";
        
        // Notification implementation would go here
        // Could integrate with push notifications, email, SMS, etc.
        _logger?.Invoke($"RealIoTController: Notification: {message}");
        
        return await Task.FromResult(true);
    }
}
