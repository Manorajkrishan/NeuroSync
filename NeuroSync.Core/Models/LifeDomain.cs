namespace NeuroSync.Core.Models;

public class LifeDomain
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public LifeDomainType Domain { get; set; }
    
    // Emotional state
    public double EmotionalScore { get; set; } // 0-100
    public string RiskLevel { get; set; } = "Healthy"; // Healthy/AtRisk/Unhealthy/Crisis
    
    // Current status
    public string CurrentState { get; set; } = string.Empty;
    public string? KeyInsights { get; set; } // JSON array
    
    // Stress analysis
    public string? StressTriggers { get; set; } // JSON array
    public double StressLevel { get; set; } // 0-100
    
    // Strengths & Challenges
    public string? Strengths { get; set; } // JSON array
    public string? Challenges { get; set; } // JSON array
    
    // Recommendations
    public string? Recommendations { get; set; } // JSON array
    
    // Timeline
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum LifeDomainType
{
    MentalHealth = 0,    // Inner world, mental state
    Relationships = 1,   // People, connections, love
    CareerWork = 2,      // Work, purpose, achievement
    MoneySurvival = 3,   // Finances, security, stability
    SelfGrowth = 4,      // Identity, purpose, becoming
    All = 99             // All domains (for relationships)
}
