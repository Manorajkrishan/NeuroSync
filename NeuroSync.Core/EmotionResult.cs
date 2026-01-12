namespace NeuroSync.Core;

/// <summary>
/// Represents the result of emotion detection.
/// </summary>
public class EmotionResult
{
    public EmotionType Emotion { get; set; }
    public float Confidence { get; set; }
    public DateTime Timestamp { get; set; }
    public string? OriginalText { get; set; }

    public EmotionResult()
    {
        Timestamp = DateTime.UtcNow;
    }

    public EmotionResult(EmotionType emotion, float confidence, string? originalText = null)
        : this()
    {
        Emotion = emotion;
        Confidence = confidence;
        OriginalText = originalText;
    }
}


