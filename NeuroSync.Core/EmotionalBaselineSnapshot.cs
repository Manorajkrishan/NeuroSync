namespace NeuroSync.Core;

/// <summary>
/// Personal emotional baseline vs recent behaviour.
/// All values are uncertain estimates for support — not diagnoses.
/// </summary>
public class EmotionalBaselineSnapshot
{
    public string UserId { get; set; } = string.Empty;
    public string? DominantBaselineEmotion { get; set; }
    public double BaselineMoodScore { get; set; }
    public double RecentMoodScore { get; set; }
    public double Deviation { get; set; }
    public bool IsSignificantlyDifferent { get; set; }
    public int SampleCount { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Disclaimer { get; set; } =
        "These are behavioural estimates from your conversations with NeuroSync. They are not medical facts or diagnoses.";
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
