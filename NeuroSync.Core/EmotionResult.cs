namespace NeuroSync.Core;

/// <summary>
/// Represents the result of emotion detection + deeper understanding.
/// </summary>
public class EmotionResult
{
    public EmotionType Emotion { get; set; }
    public float Confidence { get; set; }
    public DateTime Timestamp { get; set; }
    public string? OriginalText { get; set; }

    /// <summary>mild | moderate | intense</summary>
    public string Intensity { get; set; } = "moderate";

    /// <summary>Short human explanation of what we understood.</summary>
    public string? UnderstoodAs { get; set; }

    /// <summary>Likely situational cause (exam, work, loneliness, etc.).</summary>
    public string? LikelyCause { get; set; }

    /// <summary>Secondary emotion if mixed feelings are present.</summary>
    public EmotionType? SecondaryEmotion { get; set; }

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
