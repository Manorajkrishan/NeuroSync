namespace NeuroSync.Core;

/// <summary>
/// Safety classification for emotional conversations.
/// Estimates only — never a medical diagnosis.
/// </summary>
public enum SafetyLevel
{
    Normal = 0,
    ElevatedDistress = 1,
    PotentialCrisis = 2,
    ImmediateDanger = 3
}

public class SafetyAssessment
{
    public SafetyLevel Level { get; set; } = SafetyLevel.Normal;
    public string Reason { get; set; } = "No elevated safety signals detected.";
    public string Guidance { get; set; } =
        "NeuroSync is a wellbeing companion, not a therapist or crisis service.";
    public bool BlockNormalCompanionFlow { get; set; }
    public string? CrisisResourceHint { get; set; }
}
