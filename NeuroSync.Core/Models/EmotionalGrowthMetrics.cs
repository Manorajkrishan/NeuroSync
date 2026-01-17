namespace NeuroSync.Core.Models;

public class EmotionalGrowthMetrics
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    
    // Maturity score
    public double MaturityScore { get; set; } // 0-100
    public double EmotionalIntelligence { get; set; } // 0-100
    public double SelfAwareness { get; set; } // 0-100
    public double RegulationAbility { get; set; } // 0-100
    public double EmpathyScore { get; set; } // 0-100
    public double SocialSkills { get; set; } // 0-100
    
    // Resilience score
    public double ResilienceScore { get; set; } // 0-100
    public double RecoverySpeed { get; set; } // 0-100
    public double BounceBackAbility { get; set; } // 0-100
    public double StressTolerance { get; set; } // 0-100
    public double AdaptationCapacity { get; set; } // 0-100
    public double SupportUtilization { get; set; } // 0-100
    
    // Healing progress (JSON array for multiple traumas)
    public string? HealingProgress { get; set; } // JSON: [{TraumaId: 1, Progress: 75, ...}]
    
    // Growth milestones (JSON array)
    public string? GrowthMilestones { get; set; } // JSON array
    
    // Timeline
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
