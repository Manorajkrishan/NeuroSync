namespace NeuroSync.Core;

/// <summary>
/// Request model for facial expression emotion detection.
/// </summary>
public class FacialEmotionRequest
{
    public string Emotion { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public string? UserId { get; set; }
    public string Source { get; set; } = "facial_expression";
}

