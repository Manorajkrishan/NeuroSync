namespace NeuroSync.Core.Models;

public class Decision
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DecisionText { get; set; } = string.Empty;
    public DecisionType DecisionType { get; set; }
    public DecisionStatus Status { get; set; } = DecisionStatus.Active;
    public StakesLevel Stakes { get; set; } = StakesLevel.Medium;
    
    // Analysis
    public string? Analysis { get; set; } // JSON: DecisionAnalysis
    public string? Recommendations { get; set; } // JSON array
    public string? Warnings { get; set; } // JSON array
    
    // Timeline
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Options (one-to-many)
    public List<DecisionOption> Options { get; set; } = new();
}

public class DecisionOption
{
    public int Id { get; set; }
    public int DecisionId { get; set; }
    public Decision Decision { get; set; } = null!;
    
    public string OptionText { get; set; } = string.Empty;
    
    // Analysis
    public string? EmotionalPros { get; set; } // JSON array
    public string? EmotionalCons { get; set; } // JSON array
    public string? PredictedOutcomes { get; set; } // JSON: Short/Medium/Long term
    
    // Metrics
    public double RegretProbability { get; set; } // 0-100
    public string RiskLevel { get; set; } = "Medium"; // Low/Medium/High
    public double ValueAlignment { get; set; } // 0-100
    
    // Recommendation
    public bool IsRecommended { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum DecisionType
{
    Career = 0,
    Relationship = 1,
    Financial = 2,
    Life = 3,
    Crisis = 4
}

public enum DecisionStatus
{
    Active = 0,
    Resolved = 1,
    Abandoned = 2
}

public enum StakesLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
