namespace NeuroSync.Api.Data;

/// <summary>Persisted conversation session per user (for ConversationMemory).</summary>
public class ConversationSessionEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? LastEmotion { get; set; }
    public DateTime? LastInteractionUtc { get; set; }
    public int ConversationCount { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
