using NeuroSync.Core;
using Microsoft.Extensions.Logging;

namespace NeuroSync.Api.Services;

/// <summary>
/// Adaptive Personality Service - Layer 3: AI changes personality automatically
/// If user is sad → soft, comforting tone
/// If user is motivated → high-energy coach mode
/// If user wants logical help → analytical advisor mode
/// </summary>
public class AdaptivePersonalityService
{
    private readonly ILogger<AdaptivePersonalityService> _logger;
    private readonly ConversationMemory? _conversationMemory;
    private readonly UserProfileService? _userProfileService;

    public AdaptivePersonalityService(
        ILogger<AdaptivePersonalityService> logger,
        ConversationMemory? conversationMemory = null,
        UserProfileService? userProfileService = null)
    {
        _logger = logger;
        _conversationMemory = conversationMemory;
        _userProfileService = userProfileService;
    }

    /// <summary>
    /// Determine the appropriate personality mode based on user's emotional state and context
    /// </summary>
    public PersonalityMode DeterminePersonalityMode(
        EmotionType emotion,
        float confidence,
        string? userMessage = null,
        string? userId = null,
        ConversationContext? context = null)
    {
        var modeType = SelectMode(emotion, confidence, userMessage, context, userId);
        
        var mode = new PersonalityMode
        {
            Mode = modeType,
            Tone = DetermineTone(emotion, modeType),
            EnergyLevel = DetermineEnergyLevel(emotion, modeType),
            CommunicationStyle = DetermineCommunicationStyle(modeType, userId)
        };

        _logger.LogInformation(
            "Personality mode determined: {Mode} for emotion {Emotion}",
            mode.Mode, emotion);

        return mode;
    }

    /// <summary>
    /// Select the appropriate personality mode
    /// </summary>
    private PersonalityModeType SelectMode(
        EmotionType emotion,
        float confidence,
        string? userMessage,
        ConversationContext? context,
        string? userId)
    {
        // High confidence emotional states → specific modes
        if (confidence > 0.7f)
        {
            return emotion switch
            {
                EmotionType.Sad => PersonalityModeType.ComfortingFriend,
                EmotionType.Anxious => PersonalityModeType.CalmCompanion,
                EmotionType.Angry => PersonalityModeType.SupportiveListener,
                EmotionType.Excited => PersonalityModeType.EnthusiasticCoach,
                EmotionType.Frustrated => PersonalityModeType.ProblemSolver,
                EmotionType.Happy => PersonalityModeType.CelebratoryFriend,
                EmotionType.Calm => PersonalityModeType.ThoughtfulAdvisor,
                _ => PersonalityModeType.WarmCompanion
            };
        }

        // Check for specific keywords that indicate desired mode
        if (!string.IsNullOrEmpty(userMessage))
        {
            var messageLower = userMessage.ToLower();

            // Problem-solving keywords → Problem Solver
            if (messageLower.Contains("help") || messageLower.Contains("solve") ||
                messageLower.Contains("fix") || messageLower.Contains("advice") ||
                messageLower.Contains("how") || messageLower.Contains("what should"))
            {
                return PersonalityModeType.ThoughtfulAdvisor;
            }

            // Goal/motivation keywords → Coach
            if (messageLower.Contains("goal") || messageLower.Contains("achieve") ||
                messageLower.Contains("motivate") || messageLower.Contains("improve") ||
                messageLower.Contains("better") || messageLower.Contains("progress"))
            {
                return PersonalityModeType.EnthusiasticCoach;
            }

            // Emotional support keywords → Friend
            if (messageLower.Contains("feel") || messageLower.Contains("sad") ||
                messageLower.Contains("lonely") || messageLower.Contains("miss") ||
                messageLower.Contains("hurt") || messageLower.Contains("heart"))
            {
                return PersonalityModeType.ComfortingFriend;
            }
        }

        // Pattern-based mode selection
        if (context != null && _conversationMemory != null && !string.IsNullOrEmpty(userId))
        {
            var recentEmotions = context.History
                .TakeLast(5)
                .Where(e => e.DetectedEmotion != null)
                .Select(e => e.DetectedEmotion!.Emotion)
                .ToList();

            // Pattern: Repeated negative emotions → Comforting Friend
            if (recentEmotions.Count(e => e == EmotionType.Sad || e == EmotionType.Anxious) >= 3)
            {
                return PersonalityModeType.ComfortingFriend;
            }

            // Pattern: Mixed emotions → Thoughtful Advisor
            if (recentEmotions.Distinct().Count() >= 4)
            {
                return PersonalityModeType.ThoughtfulAdvisor;
            }
        }

        // Default: Warm Companion
        return PersonalityModeType.WarmCompanion;
    }

    /// <summary>
    /// Determine tone based on emotion and mode
    /// </summary>
    private string DetermineTone(EmotionType emotion, PersonalityModeType mode)
    {
        return mode switch
        {
            PersonalityModeType.ComfortingFriend => "soft, gentle, warm",
            PersonalityModeType.CalmCompanion => "calm, soothing, peaceful",
            PersonalityModeType.EnthusiasticCoach => "energetic, motivating, positive",
            PersonalityModeType.ThoughtfulAdvisor => "analytical, clear, supportive",
            PersonalityModeType.ProblemSolver => "practical, direct, helpful",
            PersonalityModeType.CelebratoryFriend => "joyful, enthusiastic, warm",
            PersonalityModeType.SupportiveListener => "understanding, patient, validating",
            _ => "warm, friendly, supportive"
        };
    }

    /// <summary>
    /// Determine energy level (0.0 = low/calm, 1.0 = high/energetic)
    /// </summary>
    private float DetermineEnergyLevel(EmotionType emotion, PersonalityModeType mode)
    {
        return mode switch
        {
            PersonalityModeType.EnthusiasticCoach => 0.9f,
            PersonalityModeType.CelebratoryFriend => 0.8f,
            PersonalityModeType.ProblemSolver => 0.6f,
            PersonalityModeType.ThoughtfulAdvisor => 0.5f,
            PersonalityModeType.SupportiveListener => 0.4f,
            PersonalityModeType.WarmCompanion => 0.5f,
            PersonalityModeType.ComfortingFriend => 0.3f,
            PersonalityModeType.CalmCompanion => 0.2f,
            _ => 0.5f
        };
    }

    /// <summary>
    /// Determine communication style based on mode and user preferences
    /// </summary>
    private CommunicationStyle DetermineCommunicationStyle(PersonalityModeType mode, string? userId)
    {
        var style = new CommunicationStyle
        {
            Formality = mode switch
            {
                PersonalityModeType.ThoughtfulAdvisor => "professional",
                PersonalityModeType.ProblemSolver => "casual",
                _ => "friendly"
            },
            UseEmojis = mode switch
            {
                PersonalityModeType.CelebratoryFriend => true,
                PersonalityModeType.EnthusiasticCoach => true,
                PersonalityModeType.ComfortingFriend => true,
                _ => false
            },
            MessageLength = mode switch
            {
                PersonalityModeType.EnthusiasticCoach => "medium",
                PersonalityModeType.ThoughtfulAdvisor => "detailed",
                PersonalityModeType.ComfortingFriend => "medium",
                _ => "medium"
            }
        };

        // Adjust based on user profile if available
        if (!string.IsNullOrEmpty(userId) && _userProfileService != null)
        {
            var profile = _userProfileService.GetOrCreateProfile(userId);
            
            if (profile.PrefersShortMessages)
            {
                style.MessageLength = "short";
            }

            if (profile.PrefersEmojis)
            {
                style.UseEmojis = true;
            }

            if (!string.IsNullOrEmpty(profile.CommunicationStyle))
            {
                style.Formality = profile.CommunicationStyle;
            }
        }

        return style;
    }

    /// <summary>
    /// Get response templates for a specific personality mode
    /// </summary>
    public List<string> GetModeResponseTemplates(PersonalityModeType mode, EmotionType emotion)
    {
        var templates = new List<string>();

        switch (mode)
        {
            case PersonalityModeType.ComfortingFriend:
                templates.AddRange(GetComfortingFriendTemplates(emotion));
                break;

            case PersonalityModeType.EnthusiasticCoach:
                templates.AddRange(GetEnthusiasticCoachTemplates(emotion));
                break;

            case PersonalityModeType.ThoughtfulAdvisor:
                templates.AddRange(GetThoughtfulAdvisorTemplates(emotion));
                break;

            case PersonalityModeType.ProblemSolver:
                templates.AddRange(GetProblemSolverTemplates(emotion));
                break;

            case PersonalityModeType.CalmCompanion:
                templates.AddRange(GetCalmCompanionTemplates(emotion));
                break;

            default:
                templates.AddRange(GetWarmCompanionTemplates(emotion));
                break;
        }

        return templates;
    }

    private List<string> GetComfortingFriendTemplates(EmotionType emotion)
    {
        return emotion switch
        {
            EmotionType.Sad => new List<string>
            {
                "I'm here with you, and it's okay to feel this way.",
                "You're not alone in this. I'm listening, and I care.",
                "I can sense you're hurting. Would you like to talk about it?",
                "Your feelings are completely valid. I'm here for you."
            },
            _ => new List<string> { "I'm here with you. How can I help?" }
        };
    }

    private List<string> GetEnthusiasticCoachTemplates(EmotionType emotion)
    {
        return emotion switch
        {
            EmotionType.Excited => new List<string>
            {
                "That's amazing! Let's channel this energy into something great!",
                "I love your enthusiasm! What's your next move?",
                "This is fantastic! How can we make the most of this momentum?",
                "You're on fire! Let's keep this going!"
            },
            EmotionType.Frustrated => new List<string>
            {
                "I know it's frustrating, but you've got this! Let's break it down.",
                "Challenges are opportunities in disguise. What's the first step?",
                "You're stronger than this obstacle. Let's tackle it together!",
                "Every expert was once a beginner. Let's figure this out!"
            },
            _ => new List<string> { "Let's do this! How can I help you succeed?" }
        };
    }

    private List<string> GetThoughtfulAdvisorTemplates(EmotionType emotion)
    {
        return emotion switch
        {
            EmotionType.Anxious => new List<string>
            {
                "Let's think through this together. What's the core concern?",
                "Breaking this down logically might help. What are the facts?",
                "Consider the options: what's the worst case, best case, and most likely?",
                "Let's examine this from different angles. What have you tried?"
            },
            _ => new List<string> { "Let me think about this with you. What's the situation?" }
        };
    }

    private List<string> GetProblemSolverTemplates(EmotionType emotion)
    {
        return new List<string>
        {
            "Let's solve this step by step. What's the main issue?",
            "I can help you think through this. What have you tried so far?",
            "Let's break this down: what's working, and what's not?",
            "Here's a practical approach: [solution steps]"
        };
    }

    private List<string> GetCalmCompanionTemplates(EmotionType emotion)
    {
        return new List<string>
        {
            "Take a deep breath. We can work through this together.",
            "In this moment, what do you need most?",
            "Let's find some peace. What would help you feel calmer?",
            "Everything will be okay. Let's take it one step at a time."
        };
    }

    private List<string> GetWarmCompanionTemplates(EmotionType emotion)
    {
        return new List<string>
        {
            "I'm here with you. How are you feeling?",
            "You're doing great. What's on your mind?",
            "I care about you. What can I help with today?",
            "Let's work through this together."
        };
    }
}

/// <summary>
/// Personality mode type
/// </summary>
public enum PersonalityModeType
{
    WarmCompanion,          // Default: warm, friendly
    ComfortingFriend,       // Soft, gentle for sadness
    EnthusiasticCoach,      // High-energy for motivation
    ThoughtfulAdvisor,      // Analytical for problem-solving
    ProblemSolver,          // Practical, direct
    CalmCompanion,          // Soothing for anxiety
    CelebratoryFriend,      // Joyful for happiness
    SupportiveListener      // Patient, validating for anger
}

/// <summary>
/// Personality mode configuration
/// </summary>
public class PersonalityMode
{
    public PersonalityModeType Mode { get; set; }
    public string Tone { get; set; } = "warm, friendly";
    public float EnergyLevel { get; set; } = 0.5f;
    public CommunicationStyle CommunicationStyle { get; set; } = new();
}

/// <summary>
/// Communication style preferences
/// </summary>
public class CommunicationStyle
{
    public string Formality { get; set; } = "friendly";
    public bool UseEmojis { get; set; } = false;
    public string MessageLength { get; set; } = "medium";
}
