using NeuroSync.Core;
using System.Collections.Concurrent;

namespace NeuroSync.Api.Services;

/// <summary>
/// Manages conversation memory and emotional patterns for personalized support.
/// </summary>
public class ConversationMemory
{
    private readonly ConcurrentDictionary<string, ConversationContext> _conversations = new();
    private readonly ILogger<ConversationMemory> _logger;
    private const int MaxHistoryEntries = 50; // Keep last 50 conversations

    public ConversationMemory(ILogger<ConversationMemory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates a conversation context for a user.
    /// </summary>
    public ConversationContext GetOrCreateContext(string userId = "default")
    {
        return _conversations.GetOrAdd(userId, _ => new ConversationContext { UserId = userId });
    }

    /// <summary>
    /// Adds a conversation entry to the history.
    /// </summary>
    public void AddEntry(string userId, string userMessage, EmotionResult emotion, AdaptiveResponse response, string? followUpQuestion = null)
    {
        var context = GetOrCreateContext(userId);
        
        var entry = new ConversationEntry
        {
            UserMessage = userMessage,
            DetectedEmotion = emotion,
            Response = response,
            FollowUpQuestion = followUpQuestion
        };

        context.History.Add(entry);
        context.LastEmotion = emotion.Emotion;
        context.LastInteraction = DateTime.UtcNow;
        context.ConversationCount++;

        // Update emotion patterns
        UpdateEmotionPattern(context, emotion);

        // Limit history size
        if (context.History.Count > MaxHistoryEntries)
        {
            context.History.RemoveAt(0);
        }

        _logger.LogInformation($"Added conversation entry for user {userId}. Total entries: {context.History.Count}");
    }

    /// <summary>
    /// Updates emotion patterns based on detected emotions.
    /// </summary>
    private void UpdateEmotionPattern(ConversationContext context, EmotionResult emotion)
    {
        var pattern = context.EmotionPatterns.FirstOrDefault(p => p.Emotion == emotion.Emotion);
        
        if (pattern == null)
        {
            pattern = new EmotionPattern
            {
                Emotion = emotion.Emotion,
                FirstDetected = DateTime.UtcNow,
                LastDetected = DateTime.UtcNow,
                AverageConfidence = emotion.Confidence,
                Frequency = 1
            };
            context.EmotionPatterns.Add(pattern);
        }
        else
        {
            pattern.Frequency++;
            pattern.LastDetected = DateTime.UtcNow;
            pattern.AverageConfidence = (pattern.AverageConfidence * (pattern.Frequency - 1) + emotion.Confidence) / pattern.Frequency;
        }

        // Track common triggers (simple keyword extraction)
        if (!string.IsNullOrEmpty(emotion.OriginalText))
        {
            var keywords = ExtractKeywords(emotion.OriginalText);
            foreach (var keyword in keywords)
            {
                if (!pattern.CommonTriggers.Contains(keyword, StringComparer.OrdinalIgnoreCase))
                {
                    pattern.CommonTriggers.Add(keyword);
                }
            }
        }
    }

    /// <summary>
    /// Extracts keywords from text (simple implementation).
    /// </summary>
    private List<string> ExtractKeywords(string text)
    {
        var stopWords = new HashSet<string> { "i", "am", "is", "are", "was", "were", "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "feel", "feeling" };
        var words = text.ToLower().Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Where(w => w.Length > 3 && !stopWords.Contains(w)).Take(5).ToList();
    }

    /// <summary>
    /// Gets the most common emotion pattern for a user.
    /// </summary>
    public EmotionPattern? GetMostCommonEmotion(string userId)
    {
        var context = GetOrCreateContext(userId);
        return context.EmotionPatterns.OrderByDescending(p => p.Frequency).FirstOrDefault();
    }

    /// <summary>
    /// Gets recent conversation history.
    /// </summary>
    public List<ConversationEntry> GetRecentHistory(string userId, int count = 5)
    {
        var context = GetOrCreateContext(userId);
        return context.History.TakeLast(count).ToList();
    }

    /// <summary>
    /// Detects if there's a recurring emotional pattern that needs attention.
    /// </summary>
    public bool HasConcerningPattern(string userId)
    {
        var context = GetOrCreateContext(userId);
        var negativeEmotions = new[] { EmotionType.Sad, EmotionType.Angry, EmotionType.Anxious, EmotionType.Frustrated };
        
        var negativePattern = context.EmotionPatterns
            .Where(p => negativeEmotions.Contains(p.Emotion))
            .OrderByDescending(p => p.Frequency)
            .FirstOrDefault();

        // If negative emotions appear frequently (more than 40% of interactions)
        if (negativePattern != null && context.ConversationCount > 5)
        {
            var negativeFrequency = context.EmotionPatterns
                .Where(p => negativeEmotions.Contains(p.Emotion))
                .Sum(p => p.Frequency);
            
            return (float)negativeFrequency / context.ConversationCount > 0.4f;
        }

        return false;
    }
}

