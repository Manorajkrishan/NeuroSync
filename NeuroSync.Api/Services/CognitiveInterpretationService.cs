using NeuroSync.Core;
using Microsoft.Extensions.Logging;

namespace NeuroSync.Api.Services;

/// <summary>
/// Cognitive Interpretation Service - Layer 2: Understand WHY the user feels that way
/// Thinks like a psychologist + friend + mentor
/// Analyzes: life situation, stress triggers, mindset patterns
/// Creates: a mental model of the user
/// </summary>
public class CognitiveInterpretationService
{
    private readonly ILogger<CognitiveInterpretationService> _logger;
    private readonly ConversationMemory? _conversationMemory;
    private readonly UserProfileService? _userProfileService;

    public CognitiveInterpretationService(
        ILogger<CognitiveInterpretationService> logger,
        ConversationMemory? conversationMemory = null,
        UserProfileService? userProfileService = null)
    {
        _logger = logger;
        _conversationMemory = conversationMemory;
        _userProfileService = userProfileService;
    }

    /// <summary>
    /// Analyze the root cause of user's emotional state
    /// </summary>
    public CognitiveAnalysisResult AnalyzeEmotion(
        EmotionType emotion,
        float confidence,
        string? userMessage = null,
        string? userId = null,
        ConversationContext? context = null)
    {
        var result = new CognitiveAnalysisResult
        {
            DetectedEmotion = emotion,
            Confidence = confidence,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };

        // Analyze stress triggers
        result.StressTriggers = IdentifyStressTriggers(emotion, userMessage, context, userId);

        // Analyze mindset patterns
        result.MindsetPatterns = AnalyzeMindsetPatterns(emotion, context, userId);

        // Analyze life situation context
        result.LifeSituation = AnalyzeLifeSituation(emotion, userMessage, context, userId);

        // Create mental model
        result.MentalModel = CreateMentalModel(result, context, userId);

        // Generate insights
        result.Insights = GenerateInsights(result, context, userId);

        _logger.LogInformation(
            "Cognitive analysis completed for emotion {Emotion}. Triggers: {Triggers}, Insights: {Insights}",
            emotion, result.StressTriggers.Count, result.Insights.Count);

        return result;
    }

    /// <summary>
    /// Identify potential stress triggers based on emotion and context
    /// </summary>
    private List<string> IdentifyStressTriggers(
        EmotionType emotion,
        string? userMessage,
        ConversationContext? context,
        string? userId)
    {
        var triggers = new List<string>();

        // Analyze message content for triggers
        if (!string.IsNullOrEmpty(userMessage))
        {
            var messageLower = userMessage.ToLower();

            // Work-related triggers
            if (messageLower.Contains("work") || messageLower.Contains("job") || messageLower.Contains("boss") ||
                messageLower.Contains("deadline") || messageLower.Contains("project") || messageLower.Contains("meeting"))
            {
                triggers.Add("Work pressure");
            }

            // Relationship triggers
            if (messageLower.Contains("friend") || messageLower.Contains("family") || messageLower.Contains("relationship") ||
                messageLower.Contains("partner") || messageLower.Contains("breakup") || messageLower.Contains("argue"))
            {
                triggers.Add("Relationship stress");
            }

            // Financial triggers
            if (messageLower.Contains("money") || messageLower.Contains("bill") || messageLower.Contains("debt") ||
                messageLower.Contains("rent") || messageLower.Contains("expensive") || messageLower.Contains("afford"))
            {
                triggers.Add("Financial concerns");
            }

            // Health triggers
            if (messageLower.Contains("sick") || messageLower.Contains("pain") || messageLower.Contains("tired") ||
                messageLower.Contains("sleep") || messageLower.Contains("health") || messageLower.Contains("doctor"))
            {
                triggers.Add("Health concerns");
            }

            // Academic/learning triggers
            if (messageLower.Contains("exam") || messageLower.Contains("test") || messageLower.Contains("study") ||
                messageLower.Contains("homework") || messageLower.Contains("grade") || messageLower.Contains("school"))
            {
                triggers.Add("Academic pressure");
            }

            // Social triggers
            if (messageLower.Contains("alone") || messageLower.Contains("lonely") || messageLower.Contains("isolated") ||
                messageLower.Contains("rejected") || messageLower.Contains("excluded"))
            {
                triggers.Add("Social isolation");
            }
        }

        // Analyze historical patterns for triggers
        if (context != null && _conversationMemory != null && !string.IsNullOrEmpty(userId))
        {
            var recentEmotions = context.History
                .TakeLast(10)
                .Where(e => e.DetectedEmotion != null)
                .Select(e => e.DetectedEmotion!.Emotion)
                .ToList();

            // Pattern: Repeated negative emotions at similar times
            if (recentEmotions.Count(e => e == EmotionType.Sad || e == EmotionType.Anxious || e == EmotionType.Frustrated) >= 5)
            {
                triggers.Add("Chronic stress pattern");
            }

            // Pattern: Specific time-based triggers
            var currentHour = DateTime.UtcNow.Hour;
            var timeBasedEmotions = context.History
                .Where(e => e.DetectedEmotion != null && 
                           e.Timestamp.Hour >= currentHour - 2 && 
                           e.Timestamp.Hour <= currentHour + 2)
                .Select(e => e.DetectedEmotion!.Emotion)
                .ToList();

            if (timeBasedEmotions.Count(e => e == EmotionType.Sad || e == EmotionType.Anxious) >= 3)
            {
                triggers.Add("Time-based trigger (time of day pattern)");
            }
        }

        // If no specific triggers found, add general ones based on emotion
        if (triggers.Count == 0)
        {
            triggers.Add(emotion switch
            {
                EmotionType.Sad => "Emotional distress",
                EmotionType.Anxious => "Uncertainty or worry",
                EmotionType.Angry => "Frustration or conflict",
                EmotionType.Frustrated => "Obstacles or challenges",
                _ => "General stress"
            });
        }

        return triggers;
    }

    /// <summary>
    /// Analyze mindset patterns (optimistic, pessimistic, resilient, etc.)
    /// </summary>
    private MindsetPattern AnalyzeMindsetPatterns(
        EmotionType emotion,
        ConversationContext? context,
        string? userId)
    {
        var pattern = new MindsetPattern
        {
            Timestamp = DateTime.UtcNow
        };

        if (context == null || _conversationMemory == null || string.IsNullOrEmpty(userId))
        {
            // Default patterns
            pattern.OverallMindset = "Balanced";
            pattern.OptimismLevel = 0.5f;
            pattern.ResilienceLevel = 0.5f;
            return pattern;
        }

        // Analyze emotional history
        var recentEmotions = context.History
            .TakeLast(20)
            .Where(e => e.DetectedEmotion != null)
            .Select(e => e.DetectedEmotion!.Emotion)
            .ToList();

        var positiveEmotions = recentEmotions.Count(e => 
            e == EmotionType.Happy || e == EmotionType.Excited || e == EmotionType.Calm);
        var negativeEmotions = recentEmotions.Count(e => 
            e == EmotionType.Sad || e == EmotionType.Anxious || e == EmotionType.Angry || e == EmotionType.Frustrated);
        var totalEmotions = recentEmotions.Count;

        if (totalEmotions > 0)
        {
            // Calculate optimism level (ratio of positive to negative)
            pattern.OptimismLevel = positiveEmotions / (float)totalEmotions;

            // Analyze resilience (ability to bounce back from negative emotions)
            var negativeStreaks = CalculateNegativeStreaks(recentEmotions);
            var recoveryTime = CalculateAverageRecoveryTime(context.History);
            pattern.ResilienceLevel = Math.Max(0, 1.0f - (negativeStreaks * 0.1f) - (recoveryTime / 10.0f));
            pattern.ResilienceLevel = Math.Min(1.0f, pattern.ResilienceLevel);

            // Determine overall mindset
            pattern.OverallMindset = pattern.OptimismLevel > 0.6f ? "Optimistic" :
                                   pattern.OptimismLevel < 0.4f ? "Pessimistic" :
                                   pattern.ResilienceLevel > 0.6f ? "Resilient" :
                                   "Balanced";
        }
        else
        {
            pattern.OverallMindset = "Balanced";
            pattern.OptimismLevel = 0.5f;
            pattern.ResilienceLevel = 0.5f;
        }

        return pattern;
    }

    /// <summary>
    /// Analyze life situation context
    /// </summary>
    private LifeSituation AnalyzeLifeSituation(
        EmotionType emotion,
        string? userMessage,
        ConversationContext? context,
        string? userId)
    {
        var situation = new LifeSituation
        {
            Timestamp = DateTime.UtcNow
        };

        // Analyze activity patterns
        if (context != null && _conversationMemory != null && !string.IsNullOrEmpty(userId))
        {
            var recentInteractions = context.History.TakeLast(20).ToList();
            
            // Activity frequency
            var interactionFrequency = recentInteractions.Count / 7.0f; // Per day estimate
            situation.ActivityLevel = Math.Min(1.0f, interactionFrequency / 5.0f);

            // Social engagement (estimated from conversation patterns)
            var socialKeywords = new[] { "friend", "family", "people", "together", "alone", "social" };
            var socialMentions = recentInteractions.Count(e => 
                !string.IsNullOrEmpty(e.UserMessage) && 
                socialKeywords.Any(keyword => e.UserMessage.ToLower().Contains(keyword)));
            situation.SocialEngagement = Math.Min(1.0f, socialMentions / (float)recentInteractions.Count);
        }

        // Analyze time-based patterns
        var currentTime = DateTime.UtcNow;
        situation.TimeOfDay = currentTime.Hour switch
        {
            >= 5 and < 12 => "Morning",
            >= 12 and < 17 => "Afternoon",
            >= 17 and < 21 => "Evening",
            _ => "Night"
        };

        // Determine situation context
        situation.Context = emotion switch
        {
            EmotionType.Happy => "Positive life situation",
            EmotionType.Sad => "Challenging life situation",
            EmotionType.Anxious => "Uncertain life situation",
            EmotionType.Angry => "Conflict in life situation",
            EmotionType.Frustrated => "Obstacle in life situation",
            _ => "Neutral life situation"
        };

        return situation;
    }

    /// <summary>
    /// Create a mental model of the user
    /// </summary>
    private MentalModel CreateMentalModel(
        CognitiveAnalysisResult analysis,
        ConversationContext? context,
        string? userId)
    {
        var model = new MentalModel
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId
        };

        // Personality traits (simplified)
        model.PersonalityTraits = new List<string>();

        if (analysis.MindsetPatterns.OptimismLevel > 0.6f)
        {
            model.PersonalityTraits.Add("Optimistic");
        }
        if (analysis.MindsetPatterns.OptimismLevel < 0.4f)
        {
            model.PersonalityTraits.Add("Realistic");
        }
        if (analysis.MindsetPatterns.ResilienceLevel > 0.6f)
        {
            model.PersonalityTraits.Add("Resilient");
        }

        // Emotional patterns
        model.EmotionalPatterns = new Dictionary<string, float>();
        if (context != null && _conversationMemory != null && !string.IsNullOrEmpty(userId))
        {
            var emotionFrequency = context.History
                .Where(e => e.DetectedEmotion != null)
                .GroupBy(e => e.DetectedEmotion!.Emotion)
                .ToDictionary(g => g.Key.ToString(), g => (float)g.Count() / context.History.Count);

            foreach (var kvp in emotionFrequency)
            {
                model.EmotionalPatterns[kvp.Key] = kvp.Value;
            }
        }

        // Coping mechanisms (inferred from patterns)
        model.InferredCopingMechanisms = new List<string>();
        if (analysis.MindsetPatterns.ResilienceLevel > 0.7f)
        {
            model.InferredCopingMechanisms.Add("Strong emotional regulation");
        }
        if (analysis.StressTriggers.Count > 0)
        {
            model.InferredCopingMechanisms.Add("Aware of stress sources");
        }

        return model;
    }

    /// <summary>
    /// Generate psychological insights
    /// </summary>
    private List<string> GenerateInsights(
        CognitiveAnalysisResult analysis,
        ConversationContext? context,
        string? userId)
    {
        var insights = new List<string>();

        // Insight: Stress trigger patterns
        if (analysis.StressTriggers.Count > 0)
        {
            insights.Add($"Primary stress triggers identified: {string.Join(", ", analysis.StressTriggers.Take(3))}");
        }

        // Insight: Mindset patterns
        if (analysis.MindsetPatterns.OptimismLevel < 0.4f)
        {
            insights.Add("Recent patterns suggest a more challenging emotional state. Consider self-care and support.");
        }
        else if (analysis.MindsetPatterns.OptimismLevel > 0.7f)
        {
            insights.Add("You're maintaining a positive outlook! This is great for overall well-being.");
        }

        // Insight: Resilience
        if (analysis.MindsetPatterns.ResilienceLevel < 0.5f)
        {
            insights.Add("You might benefit from strategies to build emotional resilience and recovery.");
        }

        // Insight: Emotional patterns
        if (analysis.MentalModel.EmotionalPatterns.Count > 0)
        {
            var dominantEmotion = analysis.MentalModel.EmotionalPatterns
                .OrderByDescending(kvp => kvp.Value)
                .First();
            insights.Add($"Most frequent emotional state: {dominantEmotion.Key} ({dominantEmotion.Value:P0})");
        }

        return insights;
    }

    // Helper methods
    private int CalculateNegativeStreaks(List<EmotionType> emotions)
    {
        int maxStreak = 0;
        int currentStreak = 0;
        var negativeEmotions = new[] { EmotionType.Sad, EmotionType.Anxious, EmotionType.Angry, EmotionType.Frustrated };

        foreach (var emotion in emotions)
        {
            if (negativeEmotions.Contains(emotion))
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
            else
            {
                currentStreak = 0;
            }
        }

        return maxStreak;
    }

    private float CalculateAverageRecoveryTime(List<ConversationEntry> history)
    {
        if (history.Count < 2) return 0;

        var negativeEmotions = new[] { EmotionType.Sad, EmotionType.Anxious, EmotionType.Angry, EmotionType.Frustrated };
        var recoveryTimes = new List<float>();

        for (int i = 0; i < history.Count - 1; i++)
        {
            var current = history[i];
            var next = history[i + 1];

            if (current.DetectedEmotion != null && next.DetectedEmotion != null &&
                negativeEmotions.Contains(current.DetectedEmotion.Emotion) &&
                !negativeEmotions.Contains(next.DetectedEmotion.Emotion))
            {
                var timeDiff = (next.Timestamp - current.Timestamp).TotalHours;
                if (timeDiff < 24) // Only count recoveries within 24 hours
                {
                    recoveryTimes.Add((float)timeDiff);
                }
            }
        }

        return recoveryTimes.Count > 0 ? recoveryTimes.Average() : 0;
    }
}

/// <summary>
/// Cognitive analysis result
/// </summary>
public class CognitiveAnalysisResult
{
    public EmotionType DetectedEmotion { get; set; }
    public float Confidence { get; set; }
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public List<string> StressTriggers { get; set; } = new();
    public MindsetPattern MindsetPatterns { get; set; } = new();
    public LifeSituation LifeSituation { get; set; } = new();
    public MentalModel MentalModel { get; set; } = new();
    public List<string> Insights { get; set; } = new();
}

/// <summary>
/// Mindset pattern analysis
/// </summary>
public class MindsetPattern
{
    public DateTime Timestamp { get; set; }
    public string OverallMindset { get; set; } = "Balanced";
    public float OptimismLevel { get; set; } = 0.5f;
    public float ResilienceLevel { get; set; } = 0.5f;
}

/// <summary>
/// Life situation analysis
/// </summary>
public class LifeSituation
{
    public DateTime Timestamp { get; set; }
    public string Context { get; set; } = "Neutral";
    public string TimeOfDay { get; set; } = "Unknown";
    public float ActivityLevel { get; set; } = 0.5f;
    public float SocialEngagement { get; set; } = 0.5f;
}

/// <summary>
/// Mental model of the user
/// </summary>
public class MentalModel
{
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    public List<string> PersonalityTraits { get; set; } = new();
    public Dictionary<string, float> EmotionalPatterns { get; set; } = new();
    public List<string> InferredCopingMechanisms { get; set; } = new();
}
