namespace NeuroSync.Api.Data;

/// <summary>Persisted emotion pattern per user (for ConversationMemory).</summary>
public class EmotionPatternEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Emotion { get; set; }
    public int Frequency { get; set; }
    public DateTime FirstDetected { get; set; }
    public DateTime LastDetected { get; set; }
    public double AverageConfidence { get; set; }
    public string? CommonTriggersJson { get; set; }
}
