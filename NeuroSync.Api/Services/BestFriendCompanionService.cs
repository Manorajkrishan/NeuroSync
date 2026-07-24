using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Best-friend companion layer: learns the owner, thinks ahead, and supports mentally.
/// </summary>
public class BestFriendCompanionService
{
    private readonly UserProfileService _profiles;
    private readonly ConversationMemory _memory;
    private readonly EmotionalIntelligence _ei;
    private readonly ILogger<BestFriendCompanionService> _logger;

    public BestFriendCompanionService(
        UserProfileService profiles,
        ConversationMemory memory,
        EmotionalIntelligence ei,
        ILogger<BestFriendCompanionService> logger)
    {
        _profiles = profiles;
        _memory = memory;
        _ei = ei;
        _logger = logger;
    }

    /// <summary>
    /// Learn from this turn and build a personalized companion reply package.
    /// </summary>
    public CompanionTurn BuildTurn(string userId, string userMessage, EmotionResult emotion)
    {
        _profiles.LearnFromConversation(userId, userMessage, emotion.Emotion);
        var profile = _profiles.GetOrCreateProfile(userId);
        profile.InteractionCount++;
        profile.LastLearningUpdate = DateTime.UtcNow;

        // Track emotional habit
        var key = emotion.Emotion.ToString();
        profile.EmotionalPatterns[key] = profile.EmotionalPatterns.GetValueOrDefault(key) + 1;

        // Persist interaction bump
        _profiles.SaveProfilePublic(profile);

        var context = _memory.GetOrCreateContext(userId);
        var crisis = _ei.NeedsImmediateSupport(userMessage);
        var concerning = _memory.HasConcerningPattern(userId);
        var thoughts = ThinkAboutOwner(profile, context, emotion, userMessage, concerning, crisis);

        return new CompanionTurn
        {
            Profile = profile,
            DisplayName = profile.PreferredName ?? profile.UserName,
            IsCrisis = crisis,
            HasConcerningPattern = concerning,
            SelfThoughts = thoughts,
            LearningQuestion = ShouldAskLearningQuestion(profile, userMessage)
                ? _profiles.GetLearningQuestion(userId)
                : null,
            PersonalizedHelp = GetPersonalizedHelp(profile, emotion.Emotion)
        };
    }

    /// <summary>
    /// Self-thinking: what a best friend would notice and care about.
    /// </summary>
    private List<string> ThinkAboutOwner(
        UserProfile profile,
        ConversationContext context,
        EmotionResult emotion,
        string userMessage,
        bool concerning,
        bool crisis)
    {
        var thoughts = new List<string>();

        if (crisis)
        {
            thoughts.Add("Owner may be in real distress — prioritize safety and gentle presence, not advice spam.");
            return thoughts;
        }

        if (concerning)
            thoughts.Add("Recent mood pattern looks heavy — check in like a close friend, not a therapist lecture.");

        if (!string.IsNullOrEmpty(profile.PreferredName) && EmotionalIntelligence.IsGreetingOrSmallTalk(userMessage))
            thoughts.Add($"Greet {profile.PreferredName} warmly — they've been here {profile.InteractionCount} times.");

        if (emotion.Emotion is EmotionType.Sad or EmotionType.Anxious or EmotionType.Frustrated)
        {
            if (profile.ThingsThatHelp.Count > 0)
                thoughts.Add($"Offer what already helps them: {profile.ThingsThatHelp.First()}.");
            else
                thoughts.Add("Ask gently what usually helps — remember it for next time.");
        }

        if (!string.IsNullOrEmpty(emotion.UnderstoodAs))
            thoughts.Add($"Read: {emotion.UnderstoodAs}");
        if (!string.IsNullOrEmpty(emotion.LikelyCause))
            thoughts.Add($"Likely about {emotion.LikelyCause} — ask about that gently.");
        if (emotion.Intensity == "intense")
            thoughts.Add("Intensity is high — keep the reply short, warm, and present.");

        if (emotion.Emotion == EmotionType.Happy && profile.ThingsThatMakeHappy.Count > 0)
            thoughts.Add($"Celebrate with them — they love {profile.ThingsThatMakeHappy.First()}.");

        if (profile.Triggers.Count > 0 &&
            profile.Triggers.Any(t => userMessage.Contains(t, StringComparison.OrdinalIgnoreCase)))
            thoughts.Add("A known trigger showed up — stay calm, validate, don't minimize.");

        var hour = DateTime.Now.Hour;
        if (hour >= 22 || hour < 5)
            thoughts.Add("It's late — keep replies soft and short; encourage rest if they're struggling.");

        if (context.ConversationCount >= 3 && context.History.TakeLast(3).All(h =>
                h.DetectedEmotion?.Emotion is EmotionType.Sad or EmotionType.Anxious or EmotionType.Angry or EmotionType.Frustrated))
            thoughts.Add("Three hard turns in a row — be extra present; remind them they're not alone.");

        if (thoughts.Count == 0)
            thoughts.Add("Be their best friend: listen fully, zero judgment, leave them feeling less alone and a bit happier.");

        // Loneliness mission
        var lonelyHints = new[] { "alone", "lonely", "no one", "nobody", "no friends", "can't talk" };
        if (lonelyHints.Any(h => userMessage.Contains(h, StringComparison.OrdinalIgnoreCase)))
            thoughts.Insert(0, "Owner feels alone — prioritize presence, warmth, and invitation to share everything.");

        return thoughts;
    }

    private static bool ShouldAskLearningQuestion(UserProfile profile, string userMessage)
    {
        if (EmotionalIntelligence.IsGreetingOrSmallTalk(userMessage)) return false;
        if (profile.InteractionCount % 4 != 0) return false; // don't nag every turn
        return profile.GetNextLearningTopic() != null;
    }

    private static string? GetPersonalizedHelp(UserProfile profile, EmotionType emotion)
    {
        if (emotion is EmotionType.Sad or EmotionType.Anxious or EmotionType.Angry or EmotionType.Frustrated)
        {
            if (profile.WhatHelpsWhen.TryGetValue(emotion.ToString().ToLowerInvariant(), out var list) && list.Count > 0)
                return list[0];
            if (profile.ThingsThatHelp.Count > 0)
                return profile.ThingsThatHelp[0];
        }
        return null;
    }
}

public class CompanionTurn
{
    public UserProfile Profile { get; set; } = new();
    public string? DisplayName { get; set; }
    public bool IsCrisis { get; set; }
    public bool HasConcerningPattern { get; set; }
    public List<string> SelfThoughts { get; set; } = new();
    public string? LearningQuestion { get; set; }
    public string? PersonalizedHelp { get; set; }
}
