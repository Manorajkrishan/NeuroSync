namespace NeuroSync.Core.Models;

public class DailyEmotionalSummary
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    
    // Current emotional state
    public string CurrentEmotion { get; set; } = "Neutral";
    public double CurrentEmotionConfidence { get; set; }
    
    // Trend data (7-day emotional trend)
    public string EmotionalTrend { get; set; } = "Stable"; // Improving/Declining/Stable
    public double AverageEmotionScore { get; set; }
    
    // Stress & Mental Load
    public double StressLevel { get; set; } // 0-100
    public double MentalLoad { get; set; } // 0-100
    public double EnergyLevel { get; set; } // 0-100
    
    // Risk scores
    public double BurnoutRisk { get; set; } // 0-100
    public string BurnoutRiskLevel { get; set; } = "Low"; // Low/Medium/High/Critical
    
    // Growth metrics
    public double EmotionalGrowthScore { get; set; } // 0-100
    
    // Insights (JSON for flexibility)
    public string? KeyInsights { get; set; } // JSON array of insights
    
    // Domain states (JSON for 5 domains)
    public string? DomainStates { get; set; } // JSON: {Mental: 75, Relationships: 60, ...}
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
