namespace NeuroSync.Core;

/// <summary>
/// Represents a user profile - what the AI "knows" about the user (like a baby learning about its parent).
/// </summary>
public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    
    // Basic Information (Day 1)
    public string? UserName { get; set; }
    public string? PreferredName { get; set; } // How they like to be called
    public DateTime? FirstInteraction { get; set; }
    public int InteractionCount { get; set; }
    
    // Preferences (Week 1-2)
    public List<string> FavoriteActivities { get; set; } = new();
    public List<string> MusicPreferences { get; set; } = new();
    public List<string> ThingsThatHelp { get; set; } = new(); // What helps when sad/anxious
    public List<string> ThingsThatMakeHappy { get; set; } = new();
    public List<string> Triggers { get; set; } = new(); // What causes stress/sadness
    
    // Routines (Month 1)
    public Dictionary<string, string> Routines { get; set; } = new(); // "morning", "evening", etc.
    public string? ActiveHours { get; set; } // "morning", "evening", "night"
    public string? WorkSchedule { get; set; }
    
    // Emotional Patterns (Month 1-2)
    public Dictionary<string, int> EmotionalPatterns { get; set; } = new(); // Emotion -> frequency
    public Dictionary<string, List<string>> WhatHelpsWhen { get; set; } = new(); // "sad" -> ["music", "walk"]
    public Dictionary<string, int> RecoveryTime { get; set; } = new(); // How long it takes to recover
    
    // Communication Style (Ongoing)
    public string? CommunicationStyle { get; set; } // "formal", "casual", "friendly"
    public string? FormalityLevel { get; set; } // "casual", "friendly", "professional"
    public bool PrefersShortMessages { get; set; }
    public bool PrefersEmojis { get; set; }
    
    // Learning Progress (Like baby development stages)
    public int LearningStage { get; set; } = 0; // 0=newborn, 1=basic, 2=preferences, 3=routines, 4=advanced
    public DateTime LastLearningUpdate { get; set; }
    public Dictionary<string, DateTime> LearnedTopics { get; set; } = new(); // What was learned and when
    
    // Personal Information
    public Dictionary<string, object> CustomAttributes { get; set; } = new(); // Any other info
    
    public UserProfile()
    {
        FirstInteraction = DateTime.UtcNow;
        LastLearningUpdate = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Gets what the AI "knows" about the user in a friendly way.
    /// </summary>
    public string GetWhatIKnow()
    {
        var facts = new List<string>();
        
        if (!string.IsNullOrEmpty(PreferredName))
        {
            facts.Add($"Your name is {PreferredName}");
        }
        
        if (FavoriteActivities.Count > 0)
        {
            facts.Add($"You like: {string.Join(", ", FavoriteActivities.Take(3))}");
        }
        
        if (ThingsThatHelp.Count > 0)
        {
            facts.Add($"When you're sad, {ThingsThatHelp.First()} helps");
        }
        
        return string.Join(". ", facts);
    }
    
    /// <summary>
    /// Determines what the AI should learn next based on current stage.
    /// </summary>
    public string? GetNextLearningTopic()
    {
        if (LearningStage == 0 && string.IsNullOrEmpty(UserName))
        {
            return "name";
        }
        
        if (LearningStage == 1 && FavoriteActivities.Count == 0)
        {
            return "activities";
        }
        
        if (LearningStage == 2 && ThingsThatHelp.Count == 0)
        {
            return "what_helps";
        }
        
        return null; // Continue normal conversation
    }
}
