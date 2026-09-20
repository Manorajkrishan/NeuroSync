using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NeuroSync.Api.Data;
using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Manages conversation memory and emotional patterns for personalized support.
/// Persists sessions, entries, and emotion patterns to the database.
/// </summary>
public class ConversationMemory
{
    private readonly ConcurrentDictionary<string, ConversationContext> _conversations = new();
    private readonly ILogger<ConversationMemory> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private const int MaxHistoryEntries = 50; // Keep last 50 conversations
    private static readonly TimeSpan EmotionPatternHalfLife = TimeSpan.FromDays(7); // Time decay: patterns older than this weigh less

    public ConversationMemory(ILogger<ConversationMemory> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Gets or creates a conversation context for a user (loads from DB if not in memory).
    /// </summary>
    public ConversationContext GetOrCreateContext(string userId = "default")
    {
        return _conversations.GetOrAdd(userId, _ => LoadContextFromDb(userId));
    }

    private ConversationContext LoadContextFromDb(string userId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NeuroSyncDbContext>();

            var session = db.ConversationSessions.AsNoTracking().FirstOrDefault(s => s.UserId == userId);
            var entries = db.ConversationEntries.AsNoTracking()
                .Where(e => e.UserId == userId)
                .OrderBy(e => e.Timestamp)
                .ToList();
            var patterns = db.EmotionPatterns.AsNoTracking().Where(p => p.UserId == userId).ToList();

            var context = new ConversationContext
            {
                UserId = userId,
                LastEmotion = session?.LastEmotion != null ? (EmotionType)session.LastEmotion : null,
                LastInteraction = session?.LastInteractionUtc,
                ConversationCount = session?.ConversationCount ?? 0
            };

            foreach (var e in entries)
            {
                context.History.Add(new ConversationEntry
                {
                    UserMessage = e.UserMessage,
                    DetectedEmotion = new EmotionResult((EmotionType)e.Emotion, (float)e.Confidence) { OriginalText = e.UserMessage },
                    Response = new AdaptiveResponse { Message = e.ResponseMessage },
                    Timestamp = e.Timestamp
                });
            }

            foreach (var p in patterns)
            {
                var triggers = string.IsNullOrEmpty(p.CommonTriggersJson)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(p.CommonTriggersJson) ?? new List<string>();
                context.EmotionPatterns.Add(new EmotionPattern
                {
                    Emotion = (EmotionType)p.Emotion,
                    Frequency = p.Frequency,
                    FirstDetected = p.FirstDetected,
                    LastDetected = p.LastDetected,
                    AverageConfidence = (float)p.AverageConfidence,
                    CommonTriggers = triggers
                });
            }

            _logger.LogDebug("Loaded conversation context for user {UserId} from DB: {Count} entries", userId, context.History.Count);
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load conversation context for {UserId}, using empty context", userId);
            return new ConversationContext { UserId = userId };
        }
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

        // Limit history size (in-memory)
        if (context.History.Count > MaxHistoryEntries)
        {
            context.History.RemoveAt(0);
        }

        // Persist to database
        try
        {
            SaveEntryToDb(userId, context, entry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist conversation entry for user {UserId}", userId);
        }

        _logger.LogInformation("Added conversation entry for user {UserId}. Total entries: {Count}", userId, context.History.Count);
    }

    private void SaveEntryToDb(string userId, ConversationContext context, ConversationEntry entry)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeuroSyncDbContext>();

        // Upsert session
        var session = db.ConversationSessions.FirstOrDefault(s => s.UserId == userId);
        if (session == null)
        {
            session = new ConversationSessionEntity { UserId = userId };
            db.ConversationSessions.Add(session);
        }
        session.LastEmotion = entry.DetectedEmotion != null ? (int)entry.DetectedEmotion.Emotion : null;
        session.LastInteractionUtc = context.LastInteraction;
        session.ConversationCount = context.ConversationCount;
        session.UpdatedAt = DateTime.UtcNow;

        // Insert new entry
        db.ConversationEntries.Add(new ConversationEntryEntity
        {
            UserId = userId,
            UserMessage = entry.UserMessage,
            Emotion = entry.DetectedEmotion != null ? (int)entry.DetectedEmotion.Emotion : (int)EmotionType.Neutral,
            Confidence = entry.DetectedEmotion?.Confidence ?? 0f,
            ResponseMessage = entry.Response?.Message,
            Timestamp = entry.Timestamp
        });

        // Upsert emotion pattern (the one we just updated)
        var emotion = entry.DetectedEmotion?.Emotion ?? EmotionType.Neutral;
        var pattern = context.EmotionPatterns.FirstOrDefault(p => p.Emotion == emotion);
        if (pattern != null)
        {
            var patternEntity = db.EmotionPatterns.FirstOrDefault(p => p.UserId == userId && p.Emotion == (int)pattern.Emotion);
            if (patternEntity == null)
            {
                patternEntity = new EmotionPatternEntity
                {
                    UserId = userId,
                    Emotion = (int)pattern.Emotion,
                    Frequency = pattern.Frequency,
                    FirstDetected = pattern.FirstDetected,
                    LastDetected = pattern.LastDetected,
                    AverageConfidence = pattern.AverageConfidence,
                    CommonTriggersJson = pattern.CommonTriggers.Count > 0 ? JsonSerializer.Serialize(pattern.CommonTriggers) : null
                };
                db.EmotionPatterns.Add(patternEntity);
            }
            else
            {
                patternEntity.Frequency = pattern.Frequency;
                patternEntity.LastDetected = pattern.LastDetected;
                patternEntity.AverageConfidence = pattern.AverageConfidence;
                patternEntity.CommonTriggersJson = pattern.CommonTriggers.Count > 0 ? JsonSerializer.Serialize(pattern.CommonTriggers) : null;
            }
        }

        db.SaveChanges();

        // Trim old entries in DB: keep only last MaxHistoryEntries
        var toRemove = db.ConversationEntries
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Timestamp)
            .Skip(MaxHistoryEntries)
            .Select(e => e.Id)
            .ToList();
        if (toRemove.Count > 0)
        {
            foreach (var id in toRemove)
            {
                var e = db.ConversationEntries.Find(id);
                if (e != null) db.ConversationEntries.Remove(e);
            }
            db.SaveChanges();
        }
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
    /// Decay multiplier for emotion patterns: recent patterns weigh more (1.0), older ones less (exponential decay).
    /// </summary>
    private static double TimeDecayMultiplier(DateTime lastDetected)
    {
        var age = DateTime.UtcNow - lastDetected;
        if (age <= TimeSpan.Zero) return 1.0;
        // Half-life decay: multiplier = 0.5^(age / halfLife)
        var halfLives = age.TotalSeconds / EmotionPatternHalfLife.TotalSeconds;
        return Math.Pow(0.5, halfLives);
    }

    /// <summary>
    /// Gets the most common emotion pattern for a user (with time decay: recent patterns matter more).
    /// </summary>
    public EmotionPattern? GetMostCommonEmotion(string userId)
    {
        var context = GetOrCreateContext(userId);
        return context.EmotionPatterns
            .OrderByDescending(p => p.Frequency * TimeDecayMultiplier(p.LastDetected))
            .FirstOrDefault();
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
    /// Detects if there's a recurring emotional pattern that needs attention (uses time-decayed effective frequency).
    /// </summary>
    public bool HasConcerningPattern(string userId)
    {
        var context = GetOrCreateContext(userId);
        var negativeEmotions = new[] { EmotionType.Sad, EmotionType.Angry, EmotionType.Anxious, EmotionType.Frustrated };

        // Use time-decayed effective frequency so recent negative patterns matter more
        var negativeEffective = context.EmotionPatterns
            .Where(p => negativeEmotions.Contains(p.Emotion))
            .Sum(p => p.Frequency * TimeDecayMultiplier(p.LastDetected));
        var totalEffective = context.EmotionPatterns.Sum(p => p.Frequency * TimeDecayMultiplier(p.LastDetected));

        if (context.ConversationCount > 5 && totalEffective > 0)
            return (float)(negativeEffective / totalEffective) > 0.4f;

        return false;
    }

    /// <summary>
    /// Privacy control: clear in-memory + persisted conversation data for a user.
    /// </summary>
    public void ClearUserData(string userId)
    {
        _conversations.TryRemove(userId, out _);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NeuroSyncDbContext>();
            db.ConversationEntries.RemoveRange(db.ConversationEntries.Where(e => e.UserId == userId));
            db.EmotionPatterns.RemoveRange(db.EmotionPatterns.Where(p => p.UserId == userId));
            db.ConversationSessions.RemoveRange(db.ConversationSessions.Where(s => s.UserId == userId));
            db.SaveChanges();
            _logger.LogInformation("Cleared conversation memory for {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear DB conversation memory for {UserId}", userId);
        }
    }

    /// <summary>
    /// Privacy-controlled emotional timeline (daily aggregates from history).
    /// </summary>
    public List<object> GetEmotionalTimeline(string userId, int days = 14)
    {
        var context = GetOrCreateContext(userId);
        var cutoff = DateTime.UtcNow.Date.AddDays(-(Math.Clamp(days, 1, 90) - 1));
        return context.History
            .Where(h => h.DetectedEmotion != null && h.Timestamp.Date >= cutoff)
            .GroupBy(h => h.Timestamp.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var moods = g.Select(x => MoodOrdinal(x.DetectedEmotion!.Emotion)).ToList();
                var avg = moods.Average();
                return (object)new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    averageMood = Math.Round(avg, 1),
                    dominantEmotion = g.GroupBy(x => x.DetectedEmotion!.Emotion)
                        .OrderByDescending(x => x.Count())
                        .First().Key.ToString(),
                    entries = g.Count(),
                    disclaimer = "Daily wellbeing estimates only — not a clinical chart."
                };
            })
            .ToList();
    }

    private static double MoodOrdinal(EmotionType e) => e switch
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

