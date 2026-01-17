namespace NeuroSync.Core.Models;

public class IdentityProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    
    // Core values (JSON array - top 5-7 values)
    public string? CoreValues { get; set; } // JSON: ["Growth", "Connection", "Authenticity", ...]
    
    // Identity traits (JSON array)
    public string? IdentityTraits { get; set; } // JSON array
    
    // Self-perception
    public SelfPerceptionType SelfPerception { get; set; } = SelfPerceptionType.Neutral;
    public double IdentityClarityScore { get; set; } // 0-100
    public double ConfidenceInSelf { get; set; } // 0-100
    
    // Purpose
    public string? LifePurpose { get; set; }
    public double PurposeClarityScore { get; set; } // 0-100
    public double PurposeAlignment { get; set; } // 0-100
    public double DirectionConfidence { get; set; } // 0-100
    
    // Evolution timeline (JSON for tracking changes over time)
    public string? EvolutionTimeline { get; set; } // JSON array of changes
    
    // Timeline
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum SelfPerceptionType
{
    Positive = 0,
    Neutral = 1,
    Negative = 2
}
