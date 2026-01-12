using NeuroSync.Core;
using Microsoft.Extensions.Logging;

namespace NeuroSync.Api.Services;

/// <summary>
/// Contextual awareness service for Layer 4: Contextual Emotional Awareness
/// Tracks time-based trends, activity-based influence, task intensity, and emotional patterns
/// </summary>
public class ContextualAwarenessService
{
    private readonly ILogger<ContextualAwarenessService> _logger;
    private readonly ConversationMemory? _conversationMemory;
    private readonly UserProfileService? _userProfileService;

    public ContextualAwarenessService(
        ILogger<ContextualAwarenessService> logger,
        ConversationMemory? conversationMemory = null,
        UserProfileService? userProfileService = null)
    {
        _logger = logger;
        _conversationMemory = conversationMemory;
        _userProfileService = userProfileService;
    }

    /// <summary>
    /// Analyze contextual data for emotion detection
    /// </summary>
    public ContextualEmotionData AnalyzeContext(
        string? userId = null,
        string? activityType = null,
        float? activityIntensity = null,
        float? taskIntensity = null,
        float? taskComplexity = null,
        TimeSpan? timeWindow = null)
    {
        var result = new ContextualEmotionData
        {
            Confidence = 0.0f
        };

        // Time-based trend analysis
        if (!string.IsNullOrEmpty(userId) && _conversationMemory != null)
        {
            result.TimeTrend = AnalyzeTimeBasedTrend(userId, timeWindow ?? TimeSpan.FromHours(1));
        }

        // Activity-based influence
        if (!string.IsNullOrEmpty(activityType) && activityIntensity.HasValue)
        {
            result.Activity = new ActivityData
            {
                ActivityType = activityType,
                Intensity = activityIntensity.Value
            };
            result.Activity.InferredEmotion = InferEmotionFromActivity(activityType, activityIntensity.Value);
        }

        // Task intensity mapping
        if (taskIntensity.HasValue || taskComplexity.HasValue)
        {
            result.TaskIntensity = new TaskIntensityData
            {
                Intensity = taskIntensity ?? 0.5f,
                Complexity = taskComplexity ?? 0.5f
            };
            result.TaskIntensity.InferredEmotion = InferEmotionFromTaskIntensity(result.TaskIntensity);
        }

        // Historic emotional memory patterns
        if (!string.IsNullOrEmpty(userId) && _conversationMemory != null)
        {
            result.Pattern = AnalyzeEmotionalPattern(userId);
        }

        // Determine emotion from all contextual indicators
        result.Emotion = DetermineEmotionFromContext(result);
        result.Confidence = CalculateContextualConfidence(result);

        _logger.LogDebug("Contextual analysis - Emotion: {Emotion}, Confidence: {Confidence:P2}, Activity: {Activity}, TaskIntensity: {TaskIntensity}",
            result.Emotion, result.Confidence, activityType, taskIntensity);

        return result;
    }

    /// <summary>
    /// Analyze time-based emotional trend
    /// </summary>
    private TimeBasedTrend AnalyzeTimeBasedTrend(string userId, TimeSpan timeWindow)
    {
        var trend = new TimeBasedTrend
        {
            TimeWindow = timeWindow
        };

        if (_conversationMemory == null)
            return trend;

        // Get recent emotions from conversation memory
        var context = _conversationMemory.GetOrCreateContext(userId);
        // Get recent history entries
        var recentEntries = context.History
            .Where(e => DateTime.UtcNow - e.Timestamp <= timeWindow)
            .OrderByDescending(e => e.Timestamp)
            .Take(20)
            .ToList();
        
        var recentEmotions = recentEntries
            .Where(e => e.DetectedEmotion != null)
            .Select(e => e.DetectedEmotion!.Emotion)
            .ToList();

        trend.RecentEmotions = recentEmotions;

        // Determine trend emotion (most common in time window)
        if (recentEmotions.Count > 0)
        {
            trend.TrendEmotion = recentEmotions
                .GroupBy(e => e)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;
        }

        return trend;
    }

    /// <summary>
    /// Infer emotion from activity type and intensity
    /// </summary>
    private EmotionType? InferEmotionFromActivity(string activityType, float intensity)
    {
        // High intensity activities = excitement
        if (intensity > 0.7f)
        {
            return EmotionType.Excited;
        }

        // Low intensity activities = calm
        if (intensity < 0.3f)
        {
            return EmotionType.Calm;
        }

        // Activity-specific emotions
        var lowerActivity = activityType.ToLower();
        if (lowerActivity.Contains("work") || lowerActivity.Contains("task"))
        {
            return EmotionType.Neutral;
        }
        if (lowerActivity.Contains("exercise") || lowerActivity.Contains("sport"))
        {
            return EmotionType.Excited;
        }
        if (lowerActivity.Contains("rest") || lowerActivity.Contains("relax"))
        {
            return EmotionType.Calm;
        }

        return EmotionType.Neutral;
    }

    /// <summary>
    /// Infer emotion from task intensity and complexity
    /// </summary>
    private EmotionType? InferEmotionFromTaskIntensity(TaskIntensityData taskIntensity)
    {
        var combined = (taskIntensity.Intensity + taskIntensity.Complexity) / 2.0f;

        // Very high intensity/complexity = stress/anxiety
        if (combined > 0.8f)
        {
            return EmotionType.Anxious;
        }

        // High intensity/complexity = frustration
        if (combined > 0.6f)
        {
            return EmotionType.Frustrated;
        }

        // Low intensity/complexity = calm
        if (combined < 0.4f)
        {
            return EmotionType.Calm;
        }

        return EmotionType.Neutral;
    }

    /// <summary>
    /// Analyze emotional patterns from history
    /// </summary>
    private EmotionalPattern? AnalyzeEmotionalPattern(string userId)
    {
        if (_conversationMemory == null)
            return null;

        var context = _conversationMemory.GetOrCreateContext(userId);
        var patternEmotion = _conversationMemory.GetMostCommonEmotion(userId);

        if (patternEmotion == null)
            return null;

        // Count occurrences of this emotion in history
        var emotionCount = context.History
            .Count(e => e.DetectedEmotion?.Emotion == patternEmotion.Emotion);

        if (emotionCount == 0)
            return null;

        var pattern = new EmotionalPattern
        {
            PatternEmotion = patternEmotion.Emotion,
            Frequency = emotionCount / (float)Math.Max(1, context.History.Count),
            Duration = patternEmotion.LastDetected - patternEmotion.FirstDetected
        };

        // Use triggers from the pattern emotion
        pattern.Triggers = patternEmotion.CommonTriggers;

        return pattern;
    }

    /// <summary>
    /// Determine emotion from all contextual indicators
    /// </summary>
    private EmotionType? DetermineEmotionFromContext(ContextualEmotionData data)
    {
        var emotions = new List<EmotionType?>();

        if (data.TimeTrend?.TrendEmotion != null)
            emotions.Add(data.TimeTrend.TrendEmotion);

        if (data.Activity?.InferredEmotion != null)
            emotions.Add(data.Activity.InferredEmotion);

        if (data.TaskIntensity?.InferredEmotion != null)
            emotions.Add(data.TaskIntensity.InferredEmotion);

        if (data.Pattern != null)
            emotions.Add(data.Pattern.PatternEmotion);

        // Return most common emotion
        if (emotions.Count == 0)
            return EmotionType.Neutral;

        return emotions.GroupBy(e => e)
                      .OrderByDescending(g => g.Count())
                      .FirstOrDefault()?.Key ?? EmotionType.Neutral;
    }

    /// <summary>
    /// Calculate confidence score for contextual analysis
    /// </summary>
    private float CalculateContextualConfidence(ContextualEmotionData data)
    {
        var confidence = 0.0f;
        var factors = 0;

        if (data.TimeTrend != null && data.TimeTrend.RecentEmotions.Count > 0)
        {
            confidence += 0.4f;
            factors++;
        }

        if (data.Activity != null)
        {
            confidence += 0.3f;
            factors++;
        }

        if (data.TaskIntensity != null)
        {
            confidence += 0.2f;
            factors++;
        }

        if (data.Pattern != null)
        {
            confidence += 0.1f;
            factors++;
        }

        return factors > 0 ? Math.Min(1.0f, confidence) : 0.0f;
    }
}
