namespace NeuroSync.Core;

/// <summary>
/// Request model for emotion detection.
/// </summary>
public class EmotionRequest
{
    public string Text { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
}


