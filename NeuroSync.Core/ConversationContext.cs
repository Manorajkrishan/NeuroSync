namespace NeuroSync.Core;

/// <summary>
/// Represents the context of a conversation for emotional support.
/// </summary>
public class ConversationContext
{
    public string UserId { get; set; } = "default";
    public List<ConversationEntry> History { get; set; } = new();
    public EmotionType? LastEmotion { get; set; }
    public DateTime? LastInteraction { get; set; }
    public Dictionary<string, object> UserPreferences { get; set; } = new();
    public List<EmotionPattern> EmotionPatterns { get; set; } = new();
    public string? CurrentTopic { get; set; }
    public int ConversationCount { get; set; }
    
    public ConversationContext()
    {
        ConversationCount = 0;
    }
}

/// <summary>
/// A single entry in the conversation history.
/// </summary>
public class ConversationEntry
{
    public string UserMessage { get; set; } = string.Empty;
    public EmotionResult? DetectedEmotion { get; set; }
    public AdaptiveResponse? Response { get; set; }
    public DateTime Timestamp { get; set; }
    public string? FollowUpQuestion { get; set; }
    
    public ConversationEntry()
    {
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a pattern in emotional states over time.
/// </summary>
public class EmotionPattern
{
    public EmotionType Emotion { get; set; }
    public int Frequency { get; set; }
    public DateTime FirstDetected { get; set; }
    public DateTime LastDetected { get; set; }
    public float AverageConfidence { get; set; }
    public List<string> CommonTriggers { get; set; } = new();
}

