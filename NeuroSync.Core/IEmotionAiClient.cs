namespace NeuroSync.Core;

/// <summary>
/// Emotion AI client abstraction — DecisionEngine never depends on Hugging Face or ML.NET directly.
/// </summary>
public interface IEmotionAiClient
{
    string ProviderId { get; }
    Task<EmotionAnalysisResult> AnalyseAsync(string text, CancellationToken cancellationToken = default);
}

public class EmotionAnalysisResult
{
    public string Model { get; set; } = "mlnet-baseline";
    public string ModelVersion { get; set; } = "1.0";
    public string? PrimarySignal { get; set; }
    public Dictionary<string, float> Signals { get; set; } = new();
    public UncertaintyLevel Uncertainty { get; set; } = UncertaintyLevel.Uncertain;
    public string? UncertaintyNote { get; set; }
    public EmotionType MappedEmotion { get; set; } = EmotionType.Neutral;
    public float ModelScore { get; set; }
    public long ProcessingMs { get; set; }
    public string Disclaimer { get; set; } =
        "Signal strengths are model scores over linguistic labels — not clinical probabilities.";
}
