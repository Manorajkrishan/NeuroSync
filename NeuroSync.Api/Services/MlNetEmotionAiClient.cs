using System.Diagnostics;
using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Baseline emotion provider — wraps existing ML.NET + understanding layer.
/// Kept for research comparison against Hugging Face transformers.
/// </summary>
public class MlNetEmotionAiClient : IEmotionAiClient
{
    private readonly EmotionDetectionService _detector;
    private readonly ILogger<MlNetEmotionAiClient> _logger;

    public string ProviderId => "mlnet-baseline";

    public MlNetEmotionAiClient(EmotionDetectionService detector, ILogger<MlNetEmotionAiClient> logger)
    {
        _detector = detector;
        _logger = logger;
    }

    public Task<EmotionAnalysisResult> AnalyseAsync(string text, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var result = _detector.DetectEmotion(text);
        sw.Stop();

        var signals = result.SignalEstimates.Count > 0
            ? new Dictionary<string, float>(result.SignalEstimates, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                [result.Emotion.ToString().ToLowerInvariant()] = result.Confidence
            };

        var analysis = new EmotionAnalysisResult
        {
            Model = ProviderId,
            ModelVersion = "mlnet-v1",
            PrimarySignal = result.Emotion.ToString().ToLowerInvariant(),
            Signals = signals,
            Uncertainty = result.Uncertainty,
            UncertaintyNote = result.UncertaintyNote,
            MappedEmotion = result.Emotion,
            ModelScore = result.Confidence,
            ProcessingMs = sw.ElapsedMilliseconds
        };

        _logger.LogDebug("ML.NET baseline analyse len={Len} ms={Ms} uncertainty={U}",
            text?.Length ?? 0, analysis.ProcessingMs, analysis.Uncertainty);

        return Task.FromResult(analysis);
    }
}
