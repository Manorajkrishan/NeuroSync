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
        return _profiles.GetOrAdd(userId, _ => new UserProfile { UserId = userId });
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
    /// Extracts information from user message (like baby learning from conversation).
    /// </summary>
    public void LearnFromConversation(string userId, string userMessage, EmotionType? emotion = null)
    {
        var profile = GetOrCreateProfile(userId);
        var learned = false;
        
        // Learn name if mentioned
        if (string.IsNullOrEmpty(profile.PreferredName))
        {
            // Simple name extraction (can be improved)
            var namePatterns = new[] { "my name is", "i'm", "call me", "i am" };
            foreach (var pattern in namePatterns)
            {
                if (userMessage.ToLower().Contains(pattern))
                {
                    var parts = userMessage.Split(new[] { pattern }, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        var name = parts[1].Trim().Split(' ', '.', ',', '!', '?')[0];
                        if (name.Length > 0 && name.Length < 30)
                        {
                            profile.PreferredName = name;
                            learned = true;
                            break;
                        }
                    }
                }
            }
        }
        
        // Learn preferences from conversation
        if (userMessage.ToLower().Contains("i like") || userMessage.ToLower().Contains("i love"))
        {
            // Extract what they like
            var parts = userMessage.ToLower().Split(new[] { "i like", "i love" }, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                var thing = parts[1].Trim().Split('.', ',', '!', '?')[0].Trim();
                if (thing.Length > 0 && thing.Length < 50)
                {
                    profile.ThingsThatMakeHappy.Add(thing);
                    learned = true;
                }
            }
        }
        
        // Learn what helps
        if (userMessage.ToLower().Contains("helps me") || userMessage.ToLower().Contains("makes me feel better"))
        {
            var parts = userMessage.ToLower().Split(new[] { "helps me", "makes me feel better" }, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                var thing = parts[1].Trim().Split('.', ',', '!', '?')[0].Trim();
                if (thing.Length > 0 && thing.Length < 50)
                {
                    profile.ThingsThatHelp.Add(thing);
                    learned = true;
                }
            }
        }
        
        if (learned)
        {
            profile.LastLearningUpdate = DateTime.UtcNow;
            UpdateLearningStage(profile);
            SaveProfile(profile);
        }
    }

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
            var filePath = Path.Combine(_storagePath, $"{profile.UserId}.json");
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving profile for user {profile.UserId}");
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
