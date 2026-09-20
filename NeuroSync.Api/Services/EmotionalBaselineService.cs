using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Personal baseline with minimum samples, rolling windows, decay, and confidence.
/// No "significant deviation" claims when baselineConfidence is low.
/// </summary>
public class EmotionalBaselineService
{
    public const int AbsoluteMinimumSamples = 15;
    public const int HighConfidenceSamples = 40;
    public const int BaselineWindowDays = 30;
    public const int RecentWindowSize = 7;

    private readonly ConversationMemory _memory;
    private readonly ILogger<EmotionalBaselineService> _logger;

    public EmotionalBaselineService(ConversationMemory memory, ILogger<EmotionalBaselineService> logger)
    {
        _memory = memory;
        _logger = logger;
    }

    public EmotionalBaselineSnapshot GetSnapshot(string userId)
    {
        var context = _memory.GetOrCreateContext(userId);
        var cutoff = DateTime.UtcNow.AddDays(-BaselineWindowDays);
        var history = context.History
            .Where(h => h.DetectedEmotion != null && h.Timestamp >= cutoff)
            .ToList();

        var sampleCount = history.Count;
        var confidence = ComputeConfidence(sampleCount);

        if (sampleCount < AbsoluteMinimumSamples)
        {
            return new EmotionalBaselineSnapshot
            {
                UserId = userId,
                SampleCount = sampleCount,
                BaselineConfidence = confidence,
                HasSufficientData = false,
                IsSignificantlyDifferent = false,
                Summary = $"Baseline not ready ({sampleCount}/{AbsoluteMinimumSamples} samples in {BaselineWindowDays}d window).",
                WindowDays = BaselineWindowDays
            };
        }

        // Time-decayed baseline (older turns weigh less)
        var weighted = history.Select(h =>
        {
            var ageDays = (DateTime.UtcNow - h.Timestamp).TotalDays;
            var w = Math.Exp(-ageDays / 14.0); // ~2 week half-ish decay
            return (score: MoodScore(h.DetectedEmotion!.Emotion), w);
        }).ToList();

        var baseline = weighted.Sum(x => x.score * x.w) / weighted.Sum(x => x.w);
        var recentSlice = history.TakeLast(Math.Min(RecentWindowSize, history.Count)).ToList();
        var recent = recentSlice.Average(h => MoodScore(h.DetectedEmotion!.Emotion));
        var deviation = recent - baseline;

        var dominant = history
            .GroupBy(h => h.DetectedEmotion!.Emotion)
            .OrderByDescending(g => g.Count())
            .First().Key.ToString();

        // Require confidence + meaningful deviation
        var significant = confidence >= 0.55 && Math.Abs(deviation) >= 1.5;

        var snapshot = new EmotionalBaselineSnapshot
        {
            UserId = userId,
            DominantBaselineEmotion = dominant,
            BaselineMoodScore = Math.Round(baseline, 2),
            RecentMoodScore = Math.Round(recent, 2),
            Deviation = Math.Round(deviation, 2),
            IsSignificantlyDifferent = significant,
            SampleCount = sampleCount,
            BaselineConfidence = Math.Round(confidence, 2),
            HasSufficientData = true,
            WindowDays = BaselineWindowDays,
            Summary = significant
                ? $"Recent pattern differs from your {BaselineWindowDays}d baseline (Δ {deviation:+0.0;-0.0}, confidence {confidence:0.00}). Estimate only — not a diagnosis."
                : $"Baseline available (confidence {confidence:0.00}). Recent pattern looks close to your usual."
        };

        _logger.LogInformation(
            "Baseline userLen={Len} samples={Samples} confidence={Confidence} significant={Significant}",
            userId.Length, sampleCount, snapshot.BaselineConfidence, snapshot.IsSignificantlyDifferent);

        return snapshot;
    }

    public void ResetBaselineHistory(string userId)
    {
        _memory.ClearUserData(userId);
        _logger.LogInformation("Baseline history reset requested (userId length={Len})", userId.Length);
    }

    private static double ComputeConfidence(int samples)
    {
        if (samples <= 0) return 0;
        if (samples < AbsoluteMinimumSamples)
            return Math.Clamp(samples / (double)AbsoluteMinimumSamples * 0.49, 0, 0.49);
        if (samples >= HighConfidenceSamples) return 0.95;
        var t = (samples - AbsoluteMinimumSamples) / (double)(HighConfidenceSamples - AbsoluteMinimumSamples);
        return Math.Clamp(0.55 + t * 0.40, 0.55, 0.95);
    }

    private static double MoodScore(EmotionType emotion) => emotion switch
    {
        EmotionType.Happy => 8,
        EmotionType.Excited => 8.5,
        EmotionType.Calm => 7,
        EmotionType.Neutral => 5.5,
        EmotionType.Frustrated => 3.5,
        EmotionType.Anxious => 3,
        EmotionType.Angry => 2.5,
        EmotionType.Sad => 2,
        _ => 5
    };
}
