namespace NeuroSync.Api.Data;

/// <summary>Single conversation entry (user message + detected emotion + response summary).</summary>
public class ConversationEntryEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public int Emotion { get; set; }
    public double Confidence { get; set; }
    public string? ResponseMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
