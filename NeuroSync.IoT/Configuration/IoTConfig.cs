namespace NeuroSync.IoT.Configuration;

/// <summary>
/// Configuration for IoT services.
/// </summary>
public class IoTConfig
{
    /// <summary>
    /// Spotify API configuration.
    /// </summary>
    public SpotifyConfig? Spotify { get; set; }

    /// <summary>
    /// YouTube Music API configuration.
    /// </summary>
    public YouTubeMusicConfig? YouTubeMusic { get; set; }

    /// <summary>
    /// Philips Hue configuration.
    /// </summary>
    public PhilipsHueConfig? PhilipsHue { get; set; }

    /// <summary>
    /// LIFX configuration.
    /// </summary>
    public LIFXConfig? LIFX { get; set; }

    /// <summary>
    /// Preferred music service ("Spotify", "YouTubeMusic", or "Auto").
    /// </summary>
    public string PreferredMusicService { get; set; } = "Auto";
}

public class SpotifyConfig
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}

public class YouTubeMusicConfig
{
    public string? ApiKey { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}

public class PhilipsHueConfig
{
    public string? BridgeIp { get; set; }
    public string? Username { get; set; }
}

public class LIFXConfig
{
    public string? ApiKey { get; set; }
}
