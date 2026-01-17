namespace NeuroSync.Core.Models;

public class LifeEvent
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public LifeEventType EventType { get; set; }
    
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Emotional significance
    public double EmotionalSignificance { get; set; } // 0-100
    public LifeImpactLevel LifeImpact { get; set; } = LifeImpactLevel.Medium;
    
    // Related events (JSON array of event IDs)
    public string? RelatedEventIds { get; set; } // JSON: [1, 5, 12]
    
    // Tags (JSON array)
    public string? Tags { get; set; } // JSON: ["breakup", "healing", "growth"]
    
    // Life domain affected
    public LifeDomainType? AffectedDomain { get; set; }
    
    // Narrative context (for storytelling)
    public string? NarrativeContext { get; set; } // Free text for context
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum LifeEventType
{
    Trauma = 0,
    Growth = 1,
    Milestone = 2,
    Relationship = 3,
    Decision = 4,
    Crisis = 5
}

public enum LifeImpactLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Transformative = 3
}
