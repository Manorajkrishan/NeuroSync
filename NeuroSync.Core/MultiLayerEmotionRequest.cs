namespace NeuroSync.Core;

/// <summary>
/// Request for multi-layer emotion detection
/// </summary>
public class MultiLayerEmotionRequest
{
    /// <summary>
    /// User ID
    /// </summary>
    public string? UserId { get; set; }

    // Layer 1: Visual
    public string? VisualEmotion { get; set; }
    public float? VisualConfidence { get; set; }

    // Layer 2: Audio
    public string? AudioTranscript { get; set; }
    public float? AudioPitch { get; set; }
    public float? AudioVolume { get; set; }
    public float? AudioSpeechRate { get; set; }

    // Layer 3: Biometric
    public float? HeartRate { get; set; }
    public float? HRV { get; set; }
    public float? SkinConductivity { get; set; }
    public float? Temperature { get; set; }

    // Layer 4: Contextual
    public string? ActivityType { get; set; }
    public float? ActivityIntensity { get; set; }
    public float? TaskIntensity { get; set; }
    public float? TaskComplexity { get; set; }

    // Text input (fallback to existing emotion detection)
    public string? Text { get; set; }
}
