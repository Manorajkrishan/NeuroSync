using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Builds a personal emotional baseline from conversation history.
/// Compares recent turns to the person's own norms — not population "normal".
/// </summary>
public class EmotionalBaselineService
{
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
        var history = context.History.Where(h => h.DetectedEmotion != null).ToList();

        if (history.Count < 3)
        {
            return new EmotionalBaselineSnapshot
            {
                UserId = userId,
                SampleCount = history.Count,
                Summary = "Not enough conversation history yet to estimate your personal baseline.",
                IsSignificantlyDifferent = false
            };
        }

        var allScores = history.Select(h => MoodScore(h.DetectedEmotion!.Emotion)).ToList();
        var baseline = allScores.Average();
        var recent = history.TakeLast(Math.Min(5, history.Count))
            .Select(h => MoodScore(h.DetectedEmotion!.Emotion))
            .Average();
        var deviation = recent - baseline;
        var dominant = context.EmotionPatterns
            .OrderByDescending(p => p.Frequency)
            .FirstOrDefault()?.Emotion.ToString();

        var significant = Math.Abs(deviation) >= 1.2 && history.Count >= 6;

        var snapshot = new EmotionalBaselineSnapshot
        {
            UserId = userId,
            DominantBaselineEmotion = dominant,
            BaselineMoodScore = Math.Round(baseline, 2),
            RecentMoodScore = Math.Round(recent, 2),
            Deviation = Math.Round(deviation, 2),
            IsSignificantlyDifferent = significant,
            SampleCount = history.Count,
            Summary = significant
                ? $"Your recent pattern looks different from your usual baseline (Δ {deviation:+0.0;-0.0}). This is an estimate for gentle check-ins — not a diagnosis."
                : "Your recent pattern looks close to your usual baseline."
        };

        _logger.LogInformation(
            "Baseline for {UserId}: baseline={Baseline}, recent={Recent}, significant={Significant}",
            userId, snapshot.BaselineMoodScore, snapshot.RecentMoodScore, snapshot.IsSignificantlyDifferent);

        return snapshot;
    }

    /// <summary>Rough ordinal mood score for trend comparison only.</summary>
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
