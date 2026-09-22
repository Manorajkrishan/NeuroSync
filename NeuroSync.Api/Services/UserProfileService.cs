using NeuroSync.Core;
using System.Collections.Concurrent;
using System.Text.Json;

namespace NeuroSync.Api.Services;

/// <summary>
/// Service for managing user profiles - like a baby learning about its parent.
/// </summary>
public class UserProfileService
{
    private readonly ConcurrentDictionary<string, UserProfile> _profiles = new();
    private readonly ILogger<UserProfileService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _storagePath;

    public UserProfileService(ILogger<UserProfileService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
        _storagePath = Path.Combine(environment.ContentRootPath, "UserProfiles");
        
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
        
        LoadProfiles();
    }

    /// <summary>
    /// Gets or creates a user profile.
    /// </summary>
    public UserProfile GetOrCreateProfile(string userId)
    {
        if (!UserIdSanitizer.TryNormalize(userId, out var safe))
            safe = UserIdSanitizer.DefaultUserId;
        return _profiles.GetOrAdd(safe, id => new UserProfile { UserId = id });
    }

    /// <summary>
    /// Updates user profile with new information (like baby learning).
    /// </summary>
    public void LearnAboutUser(string userId, Dictionary<string, object> information)
    {
        var profile = GetOrCreateProfile(userId);
        
        foreach (var (key, value) in information)
        {
            switch (key.ToLower())
            {
                case "name":
                case "username":
                    profile.UserName = value.ToString();
                    if (string.IsNullOrEmpty(profile.PreferredName))
                    {
                        profile.PreferredName = value.ToString();
                    }
                    break;
                    
                case "preferredname":
                case "preferred_name":
                    profile.PreferredName = value.ToString();
                    break;
                    
                case "favoriteactivity":
                case "favorite_activity":
                    if (value is string activity && !profile.FavoriteActivities.Contains(activity))
                    {
                        profile.FavoriteActivities.Add(activity);
                    }
                    break;
                    
                case "musicpreference":
                case "music_preference":
                    if (value is string music && !profile.MusicPreferences.Contains(music))
                    {
                        profile.MusicPreferences.Add(music);
                    }
                    break;
                    
                case "whathelps":
                case "what_helps":
                    if (value is string help && !profile.ThingsThatHelp.Contains(help))
                    {
                        profile.ThingsThatHelp.Add(help);
                    }
                    break;
                    
                case "makeshappy":
                case "makes_happy":
                    if (value is string happy && !profile.ThingsThatMakeHappy.Contains(happy))
                    {
                        profile.ThingsThatMakeHappy.Add(happy);
                    }
                    break;
                    
                default:
                    profile.CustomAttributes[key] = value;
                    break;
            }
        }
        
        profile.LastLearningUpdate = DateTime.UtcNow;
        profile.InteractionCount++;
        
        // Update learning stage based on knowledge
        UpdateLearningStage(profile);
        
        SaveProfile(profile);
        _logger.LogInformation($"Learned about user {userId}: {string.Join(", ", information.Keys)}");
    }

    /// <summary>
    /// Extracts information from user message (like a best friend remembering what matters).
    /// </summary>
    public void LearnFromConversation(string userId, string userMessage, EmotionType? emotion = null)
    {
        var profile = GetOrCreateProfile(userId);
        var learned = false;
        var lower = userMessage.ToLowerInvariant();

        // Learn name
        if (string.IsNullOrEmpty(profile.PreferredName))
        {
            var namePatterns = new[] { "my name is", "call me", "i'm ", "i am " };
            foreach (var pattern in namePatterns)
            {
                var idx = lower.IndexOf(pattern, StringComparison.Ordinal);
                if (idx < 0) continue;
                var after = userMessage[(idx + pattern.Length)..].Trim();
                var name = after.Split(new[] { ' ', '.', ',', '!', '?', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrEmpty(name) && name.Length is >= 2 and < 30 &&
                    !new[] { "feeling", "sad", "happy", "tired", "not", "just", "really", "so", "a", "the" }.Contains(name.ToLowerInvariant()))
                {
                    profile.PreferredName = char.ToUpperInvariant(name[0]) + name[1..];
                    profile.UserName ??= profile.PreferredName;
                    learned = true;
                    break;
                }
            }
        }

        // Likes / loves → happiness + activities
        foreach (var cue in new[] { "i like ", "i love ", "i enjoy ", "i'm into " })
        {
            var idx = lower.IndexOf(cue, StringComparison.Ordinal);
            if (idx < 0) continue;
            var thing = ExtractPhrase(userMessage, idx + cue.Length);
            if (thing == null) continue;
            AddUnique(profile.ThingsThatMakeHappy, thing);
            AddUnique(profile.FavoriteActivities, thing);
            learned = true;
        }

        // Music
        if (lower.Contains("music") || lower.Contains("song") || lower.Contains("playlist"))
        {
            foreach (var cue in new[] { "listen to ", "i like ", "i love " })
            {
                var idx = lower.IndexOf(cue, StringComparison.Ordinal);
                if (idx < 0) continue;
                var thing = ExtractPhrase(userMessage, idx + cue.Length);
                if (thing != null) { AddUnique(profile.MusicPreferences, thing); learned = true; }
            }
        }

        // What helps when down
        foreach (var cue in new[] { "helps me", "makes me feel better", "makes me better", "calms me", "cheers me up" })
        {
            if (!lower.Contains(cue)) continue;
            // Prefer the part before "helps me" ("walking helps me")
            var before = lower.Split(new[] { cue }, StringSplitOptions.None)[0].Trim();
            var candidate = before.Split(' ').Reverse().Take(4).Reverse();
            var phrase = string.Join(' ', candidate).Trim();
            if (phrase.Length is >= 2 and < 60)
            {
                AddUnique(profile.ThingsThatHelp, phrase);
                if (emotion.HasValue)
                {
                    var eKey = emotion.Value.ToString().ToLowerInvariant();
                    if (!profile.WhatHelpsWhen.ContainsKey(eKey))
                        profile.WhatHelpsWhen[eKey] = new List<string>();
                    AddUnique(profile.WhatHelpsWhen[eKey], phrase);
                }
                learned = true;
            }
        }

        // Triggers / stress sources
        foreach (var cue in new[] { "stresses me", "triggers me", "makes me anxious", "makes me sad", "i hate ", "can't stand " })
        {
            var idx = lower.IndexOf(cue, StringComparison.Ordinal);
            if (idx < 0) continue;
            var thing = ExtractPhrase(userMessage, idx + cue.Length);
            if (thing != null) { AddUnique(profile.Triggers, thing); learned = true; }
        }

        // Habit / routine hints
        if (lower.Contains("every morning") || lower.Contains("i usually") || lower.Contains("my routine"))
        {
            var snippet = userMessage.Length > 80 ? userMessage[..80] : userMessage;
            profile.Routines["mentioned"] = snippet;
            learned = true;
        }

        // Communication style
        if (userMessage.Length < 40) profile.PrefersShortMessages = true;
        if (userMessage.Contains(':') || userMessage.Any(c => c > 127)) profile.PrefersEmojis = true;
        if (lower.Contains("lol") || lower.Contains("haha") || lower.Contains("bro") || lower.Contains("dude"))
            profile.CommunicationStyle = "casual";

        if (learned)
        {
            profile.LastLearningUpdate = DateTime.UtcNow;
            UpdateLearningStage(profile);
            SaveProfile(profile);
            _logger.LogInformation("Learned more about owner {UserId} (stage {Stage})", userId, profile.LearningStage);
        }
    }

    private static string? ExtractPhrase(string text, int start)
    {
        if (start >= text.Length) return null;
        var rest = text[start..].Trim();
        var cut = rest.Split(new[] { '.', '!', '?', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(cut) || cut.Length > 60) return null;
        return cut;
    }

    private static void AddUnique(List<string> list, string item)
    {
        if (list.Any(x => x.Equals(item, StringComparison.OrdinalIgnoreCase))) return;
        list.Add(item);
        if (list.Count > 20) list.RemoveAt(0);
    }

    /// <summary>Public save for companion turn updates.</summary>
    public void SaveProfilePublic(UserProfile profile) => SaveProfile(profile);

    /// <summary>
    /// Gets a friendly greeting based on what the AI knows about the user.
    /// </summary>
    public string GetGreeting(string userId)
    {
        var profile = GetOrCreateProfile(userId);
        
        if (profile.InteractionCount == 0)
        {
            return "Hi! I'm your new friend. What's your name?";
        }
        
        if (string.IsNullOrEmpty(profile.PreferredName))
        {
            return "Hey there! I'd love to know your name. What should I call you?";
        }
        
        var greeting = new List<string> { "Hi" };
        
        // Time-aware greeting
        var hour = DateTime.Now.Hour;
        if (hour < 12)
        {
            greeting.Add("good morning");
        }
        else if (hour < 18)
        {
            greeting.Add("good afternoon");
        }
        else
        {
            greeting.Add("good evening");
        }
        
        greeting.Add(profile.PreferredName);
        greeting.Add("!");
        
        return string.Join(" ", greeting);
    }

    /// <summary>
    /// Gets a question the AI should ask to learn more (like a curious baby).
    /// </summary>
    public string? GetLearningQuestion(string userId)
    {
        var profile = GetOrCreateProfile(userId);
        var topic = profile.GetNextLearningTopic();
        
        if (topic == null)
        {
            return null;
        }
        
        return topic switch
        {
            "name" => "What's your name?",
            "activities" => "What do you like to do for fun?",
            "what_helps" => "When you're feeling down, what helps you feel better?",
            _ => null
        };
    }

    /// <summary>
    /// Updates learning stage based on knowledge.
    /// </summary>
    private void UpdateLearningStage(UserProfile profile)
    {
        if (profile.LearningStage == 0 && !string.IsNullOrEmpty(profile.PreferredName))
        {
            profile.LearningStage = 1;
        }
        
        if (profile.LearningStage == 1 && profile.FavoriteActivities.Count > 0)
        {
            profile.LearningStage = 2;
        }
        
        if (profile.LearningStage == 2 && profile.ThingsThatHelp.Count > 0)
        {
            profile.LearningStage = 3;
        }
        
        if (profile.LearningStage == 3 && profile.InteractionCount > 20)
        {
            profile.LearningStage = 4;
        }
    }

    /// <summary>
    /// Saves profile to disk.
    /// </summary>
    private void SaveProfile(UserProfile profile)
    {
        try
        {
            if (!UserIdSanitizer.TryNormalize(profile.UserId, out var safe))
            {
                _logger.LogWarning("Refusing to save profile for unsafe userId");
                return;
            }

            var filePath = Path.Combine(_storagePath, $"{safe}.json");
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving profile for user {profile.UserId}");
        }
    }

    /// <summary>Privacy control: remove stored profile for a user.</summary>
    public bool DeleteProfile(string userId)
    {
        if (!UserIdSanitizer.TryNormalize(userId, out var safe))
        {
            _logger.LogWarning("Refusing to delete profile for unsafe userId");
            return false;
        }

        _profiles.TryRemove(safe, out _);
        try
        {
            var filePath = Path.Combine(_storagePath, $"{safe}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted profile for {UserId}", safe);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile for {UserId}", safe);
            return false;
        }
    }

    /// <summary>
    /// Loads profiles from disk.
    /// </summary>
    private void LoadProfiles()
    {
        try
        {
            var files = Directory.GetFiles(_storagePath, "*.json");
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var profile = JsonSerializer.Deserialize<UserProfile>(json);
                    if (profile != null)
                    {
                        _profiles[profile.UserId] = profile;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error loading profile from {file}");
                }
            }
            
            _logger.LogInformation($"Loaded {_profiles.Count} user profiles");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading profiles");
        }
    }
}
