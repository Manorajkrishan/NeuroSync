using NeuroSync.IoT.Configuration;
using System.Text.Json;

namespace NeuroSync.IoT.Services;

/// <summary>
/// Service for controlling smart lights (Philips Hue, LIFX, etc.).
/// </summary>
public class SmartLightService
{
    private readonly HttpClient _httpClient;
    private readonly IoTConfig? _config;
    private readonly Action<string>? _logger;

    public SmartLightService(HttpClient httpClient, IoTConfig? config = null, Action<string>? logger = null)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Controls a smart light (tries Philips Hue first, then LIFX).
    /// </summary>
    public async Task<bool> ControlLightAsync(string deviceId, string color, int brightness)
    {
        // Try Philips Hue first
        if (_config?.PhilipsHue != null && 
            !string.IsNullOrEmpty(_config.PhilipsHue.BridgeIp) &&
            !string.IsNullOrEmpty(_config.PhilipsHue.Username))
        {
            var result = await ControlPhilipsHueLightAsync(deviceId, color, brightness);
            if (result)
                return true;
        }

        // Try LIFX
        if (_config?.LIFX != null && !string.IsNullOrEmpty(_config.LIFX.ApiKey))
        {
            var result = await ControlLIFXLightAsync(deviceId, color, brightness);
            if (result)
                return true;
        }

        // No configured services
        _logger?.Invoke($"SmartLight: No configured light services. Simulating action for {deviceId}");
        return false;
    }

    private async Task<bool> ControlPhilipsHueLightAsync(string deviceId, string color, int brightness)
    {
        try
        {
            var (hue, sat, bri) = ConvertColorToHue(color, brightness);
            var bridgeIp = _config!.PhilipsHue!.BridgeIp!;
            var username = _config.PhilipsHue.Username!;

            var body = new
            {
                on = true,
                bri = bri,
                hue = hue,
                sat = sat
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var url = $"http://{bridgeIp}/api/{username}/lights/{deviceId}/state";
            var response = await _httpClient.PutAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                _logger?.Invoke($"Philips Hue: Light {deviceId} set to {color} at {brightness}%");
                return true;
            }

            _logger?.Invoke($"Philips Hue: Failed to control light {deviceId}");
            return false;
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"Philips Hue: Error - {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ControlLIFXLightAsync(string deviceId, string color, int brightness)
    {
        try
        {
            var (r, g, b) = ConvertColorToRGB(color);
            var apiKey = _config!.LIFX!.ApiKey!;

            var body = new
            {
                power = "on",
                color = $"rgb:{r},{g},{b}",
                brightness = brightness / 100.0
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var url = $"https://api.lifx.com/v1/lights/{deviceId}/state";
            var response = await _httpClient.PutAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                _logger?.Invoke($"LIFX: Light {deviceId} set to {color} at {brightness}%");
                return true;
            }

            _logger?.Invoke($"LIFX: Failed to control light {deviceId}");
            return false;
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"LIFX: Error - {ex.Message}");
            return false;
        }
    }

    private (int hue, int sat, int bri) ConvertColorToHue(string color, int brightness)
    {
        // Convert color name to Hue values (simplified)
        var colorMap = new Dictionary<string, (int hue, int sat)>
        {
            { "warm_yellow", (12750, 254) },
            { "soft_blue", (46920, 254) },
            { "cool_white", (0, 0) },
            { "soft_purple", (56100, 254) },
            { "warm_white", (0, 0) },
            { "bright_white", (0, 0) },
            { "orange", (6500, 254) },
            { "neutral_white", (0, 0) }
        };

        if (colorMap.TryGetValue(color, out var values))
        {
            return (values.hue, values.sat, (int)(brightness * 2.54));
        }

        return (0, 0, (int)(brightness * 2.54));
    }

    private (int r, int g, int b) ConvertColorToRGB(string color)
    {
        // Convert color name to RGB values
        var colorMap = new Dictionary<string, (int r, int g, int b)>
        {
            { "warm_yellow", (255, 200, 100) },
            { "soft_blue", (100, 150, 255) },
            { "cool_white", (255, 255, 255) },
            { "soft_purple", (200, 150, 255) },
            { "warm_white", (255, 250, 240) },
            { "bright_white", (255, 255, 255) },
            { "orange", (255, 165, 0) },
            { "neutral_white", (255, 255, 255) }
        };

        return colorMap.TryGetValue(color, out var rgb) ? rgb : (255, 255, 255);
    }
}

