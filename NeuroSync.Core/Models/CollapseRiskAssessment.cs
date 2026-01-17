namespace NeuroSync.Core.Models;

public class CollapseRiskAssessment
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    
    // Overall risk
    public double OverallRiskScore { get; set; } // 0-100
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    
    // Burnout risk
    public double BurnoutRiskScore { get; set; } // 0-100
    public string? BurnoutContributingFactors { get; set; } // JSON array
    public string? BurnoutWarningSigns { get; set; } // JSON array
    public string? BurnoutTimelineEstimate { get; set; }
    
    // Depression risk
    public double DepressionRiskScore { get; set; } // 0-100
    public string? DepressionSymptoms { get; set; } // JSON array
    public string? DepressionTriggers { get; set; } // JSON array
    public string DepressionSeverity { get; set; } = "None";
    
    // Anxiety risk
    public double AnxietyRiskScore { get; set; } // 0-100
    public string? AnxietyEscalationPattern { get; set; } // JSON array
    public string? AnxietyTriggers { get; set; } // JSON array
    public string AnxietyImpact { get; set; } = "Low";
    
    // Intervention
    public string InterventionUrgency { get; set; } = "Low"; // Low/Medium/High/Critical
    public string? ImmediateActions { get; set; } // JSON array
    public string? SupportResources { get; set; } // JSON array
    public string? ProfessionalReferrals { get; set; } // JSON array
    public string? SafetyMeasures { get; set; } // JSON array
    
    // Timeline
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NextCheckDate { get; set; }
}

public enum RiskLevel
{
    Low = 0,
    Moderate = 1,
    High = 2,
    Critical = 3
}
