namespace NeuroSync.Core;

/// <summary>
/// Comprehensive emotion result combining all 4 layers: Visual, Audio, Biometric, and Contextual
/// </summary>
public class MultiLayerEmotionResult
{
    /// <summary>
    /// Final fused emotion with confidence score
    /// </summary>
    public EmotionType PrimaryEmotion { get; set; }
    
    /// <summary>
    /// Overall confidence score (0.0 to 1.0)
    /// </summary>
    public float OverallConfidence { get; set; }
    
    /// <summary>
    /// Layer 1: Visual emotion recognition
    /// </summary>
    public VisualEmotionData? VisualLayer { get; set; }
    
    /// <summary>
    /// Layer 2: Audio emotion intelligence
    /// </summary>
    public AudioEmotionData? AudioLayer { get; set; }
    
    /// <summary>
    /// Layer 3: Biometric emotional analysis
    /// </summary>
    public BiometricEmotionData? BiometricLayer { get; set; }
    
    /// <summary>
    /// Layer 4: Contextual emotional awareness
    /// </summary>
    public ContextualEmotionData? ContextualLayer { get; set; }
    
    /// <summary>
    /// Weighted scores for each layer (used in fusion)
    /// </summary>
    public LayerWeights Weights { get; set; } = new();
    
    /// <summary>
    /// Timestamp of detection
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// User ID
    /// </summary>
    public string? UserId { get; set; }
}

/// <summary>
/// Layer 1: Visual emotion recognition data
/// </summary>
public class VisualEmotionData
{
    /// <summary>
    /// Detected emotion from facial expression
    /// </summary>
    public EmotionType? Emotion { get; set; }
    
    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// </summary>
    public float Confidence { get; set; }
    
    /// <summary>
    /// Micro-expressions detected
    /// </summary>
    public List<MicroExpression>? MicroExpressions { get; set; }
    
    /// <summary>
    /// Eye behavior tracking data
    /// </summary>
    public EyeBehaviorData? EyeBehavior { get; set; }
    
    /// <summary>
    /// Facial muscle movement analysis
    /// </summary>
    public MuscleMovementData? MuscleMovement { get; set; }
    
    /// <summary>
    /// Non-verbal behavioral cues
    /// </summary>
    public NonVerbalCues? NonVerbalCues { get; set; }
}

/// <summary>
/// Layer 2: Audio emotion intelligence data
/// </summary>
public class AudioEmotionData
{
    /// <summary>
    /// Detected emotion from audio
    /// </summary>
    public EmotionType? Emotion { get; set; }
    
    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// </summary>
    public float Confidence { get; set; }
    
    /// <summary>
    /// Tone stress analysis score (0.0 = calm, 1.0 = high stress)
    /// </summary>
    public float ToneStressScore { get; set; }
    
    /// <summary>
    /// Speech pattern emotion mapping
    /// </summary>
    public SpeechPatternData? SpeechPattern { get; set; }
    
    /// <summary>
    /// Breathing emotional fluctuation (detected from audio)
    /// </summary>
    public BreathingData? Breathing { get; set; }
    
    /// <summary>
    /// Voice tremor detection (anxiety indicator)
    /// </summary>
    public float VoiceTremorScore { get; set; }
}

/// <summary>
/// Layer 3: Biometric emotional analysis data
/// </summary>
public class BiometricEmotionData
{
    /// <summary>
    /// Heart rate variability (HRV) emotional mapping
    /// </summary>
    public HeartRateData? HeartRate { get; set; }
    
    /// <summary>
    /// Skin conductivity for stress measurement (GSR)
    /// </summary>
    public float? SkinConductivity { get; set; }
    
    /// <summary>
    /// Body temperature emotional fluctuation tracking
    /// </summary>
    public float? Temperature { get; set; }
    
    /// <summary>
    /// Derived emotion from biometric data
    /// </summary>
    public EmotionType? Emotion { get; set; }
    
    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// </summary>
    public float Confidence { get; set; }
}

/// <summary>
/// Layer 4: Contextual emotional awareness data
/// </summary>
public class ContextualEmotionData
{
    /// <summary>
    /// Detected emotion from context
    /// </summary>
    public EmotionType? Emotion { get; set; }
    
    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// </summary>
    public float Confidence { get; set; }
    
    /// <summary>
    /// Time-based emotional trend
    /// </summary>
    public TimeBasedTrend? TimeTrend { get; set; }
    
    /// <summary>
    /// Activity-based emotional influence
    /// </summary>
    public ActivityData? Activity { get; set; }
    
    /// <summary>
    /// Task intensity emotional mapping
    /// </summary>
    public TaskIntensityData? TaskIntensity { get; set; }
    
    /// <summary>
    /// Historic emotional memory patterns
    /// </summary>
    public EmotionalPattern? Pattern { get; set; }
}

/// <summary>
/// Weight configuration for each layer in fusion
/// </summary>
public class LayerWeights
{
    public float VisualWeight { get; set; } = 0.3f;
    public float AudioWeight { get; set; } = 0.3f;
    public float BiometricWeight { get; set; } = 0.2f;
    public float ContextualWeight { get; set; } = 0.2f;
}

// Supporting data structures

public class MicroExpression
{
    public string Type { get; set; } = string.Empty;
    public float Intensity { get; set; }
    public EmotionType? AssociatedEmotion { get; set; }
}

public class EyeBehaviorData
{
    public float BlinkRate { get; set; }
    public float? GazeDirection { get; set; }
    public float? PupilDilation { get; set; }
    public EmotionType? InferredEmotion { get; set; }
}

public class MuscleMovementData
{
    public Dictionary<string, float> MuscleTension { get; set; } = new();
    public EmotionType? InferredEmotion { get; set; }
}

public class NonVerbalCues
{
    public float? HeadPosition { get; set; }
    public float? BodyPosition { get; set; }
    public EmotionType? InferredEmotion { get; set; }
}

public class SpeechPatternData
{
    public float SpeechRate { get; set; }
    public float PitchVariation { get; set; }
    public float PauseFrequency { get; set; }
    public EmotionType? InferredEmotion { get; set; }
}

public class BreathingData
{
    public float BreathingRate { get; set; }
    public float Irregularity { get; set; }
    public EmotionType? InferredEmotion { get; set; }
}

public class HeartRateData
{
    public float? HeartRate { get; set; }
    public float? HRV { get; set; }
    public EmotionType? InferredEmotion { get; set; }
}

public class TimeBasedTrend
{
    public EmotionType? TrendEmotion { get; set; }
    public List<EmotionType> RecentEmotions { get; set; } = new();
    public TimeSpan TimeWindow { get; set; }
}

public class ActivityData
{
    public string ActivityType { get; set; } = string.Empty;
    public float Intensity { get; set; }
    public EmotionType? InferredEmotion { get; set; }
}

public class TaskIntensityData
{
    public float Intensity { get; set; }
    public float Complexity { get; set; }
    public EmotionType? InferredEmotion { get; set; }
}

public class EmotionalPattern
{
    public EmotionType PatternEmotion { get; set; }
    public float Frequency { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> Triggers { get; set; } = new();
}
