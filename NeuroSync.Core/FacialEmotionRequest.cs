namespace NeuroSync.Core;

/// <summary>
/// Request model for facial expression + eye/motion cues.
/// </summary>
public class FacialEmotionRequest
{
    public string Emotion { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public string? UserId { get; set; }
    public string Source { get; set; } = "facial_expression";

    /// <summary>0–1 estimated eye contact / attention toward camera.</summary>
    public float? EyeContactScore { get; set; }

    /// <summary>0–1 face/head motion energy.</summary>
    public float? FaceMotionScore { get; set; }

    /// <summary>looking_at_you | looking_left | looking_right | eyes_closed_or_down | partial_attention</summary>
    public string? GazeState { get; set; }

    /// <summary>calm_focused | steady | restless | disengaged</summary>
    public string? Engagement { get; set; }

    public string? CueNotes { get; set; }
    public bool RealTime { get; set; }
}
