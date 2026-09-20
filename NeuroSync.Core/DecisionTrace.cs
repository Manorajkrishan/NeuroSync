namespace NeuroSync.Core;

/// <summary>
/// Internal explainability for DecisionEngine (not a user-facing medical chart).
/// </summary>
public class DecisionTrace
{
    public SafetyLevel Safety { get; set; }
    public CompanionInteractionMode Mode { get; set; }
    public UncertaintyLevel Uncertainty { get; set; }
    public Dictionary<string, float> EmotionSignals { get; set; } = new();
    public double? BaselineDeviation { get; set; }
    public double? BaselineConfidence { get; set; }
    public string Action { get; set; } = "None";
    public bool MemoryWriteAllowed { get; set; }
    public string Summary { get; set; } = string.Empty;

    public override string ToString() =>
        $"Safety={Safety} → Mode={Mode} → Uncertainty={Uncertainty} → " +
        $"EmotionSignals={{{string.Join(", ", EmotionSignals.Select(kv => $"{kv.Key}:{kv.Value:0.00}"))}}} → " +
        $"BaselineDeviation={BaselineDeviation?.ToString("0.00") ?? "n/a"} → Action={Action}";
}
