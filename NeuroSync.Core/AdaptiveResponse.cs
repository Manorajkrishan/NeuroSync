namespace NeuroSync.Core;

/// <summary>
/// Represents an adaptive response based on detected emotion.
/// </summary>
public class AdaptiveResponse
{
    public EmotionType Emotion { get; set; }
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }

    public AdaptiveResponse()
    {
        Timestamp = DateTime.UtcNow;
    }
}


