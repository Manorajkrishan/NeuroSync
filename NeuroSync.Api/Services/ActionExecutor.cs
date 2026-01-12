using NeuroSync.Core;
using System.Text.RegularExpressions;

namespace NeuroSync.Api.Services;

/// <summary>
/// Executes user action requests (like a personal assistant).
/// </summary>
public class ActionExecutor
{
    private readonly VoiceNoteService _voiceNoteService;
    private readonly PersonMemory _personMemory;
    private readonly ILogger<ActionExecutor> _logger;

    public ActionExecutor(
        VoiceNoteService voiceNoteService,
        PersonMemory personMemory,
        ILogger<ActionExecutor> logger)
    {
        _voiceNoteService = voiceNoteService;
        _personMemory = personMemory;
        _logger = logger;
    }

    /// <summary>
    /// Detects and executes actions from user text.
    /// </summary>
    public async Task<ActionResult?> ExecuteActionAsync(string userId, string userText)
    {
        var text = userText.ToLower().Trim();

        // Pattern: "need to hear", "want to hear", "hear voice", "hear [person]'s voice"
        var hearVoicePattern = new Regex(@"(?:need|want|wanna|would\s+like)\s+to\s+hear\s+(?:my\s+)?(?:best\s+friend|friend|person|someone|her|him|their)\s*(?:'s\s+voice|voice)?", RegexOptions.IgnoreCase);
        var hearMatch = hearVoicePattern.Match(text);
        if (hearMatch.Success)
        {
            // Try to find "best friend" or similar in person memory
            var bestFriend = _personMemory.GetAllPeople(userId)
                .FirstOrDefault(p => p.Relationship?.ToLower().Contains("best friend") == true || 
                                    p.Relationship?.ToLower().Contains("friend") == true);
            
            if (bestFriend != null)
            {
                var voiceNotes = _voiceNoteService.GetVoiceNotesForPerson(userId, bestFriend.Name);
                if (voiceNotes.Count > 0)
                {
                    // Automatically play the voice note
                    return await PlayVoiceNoteAsync(userId, bestFriend.Name, autoPlay: true);
                }
            }
            
            // If no best friend found, look for any person with voice notes
            var allPeople = _personMemory.GetAllPeople(userId);
            foreach (var person in allPeople)
            {
                var voiceNotes = _voiceNoteService.GetVoiceNotesForPerson(userId, person.Name);
                if (voiceNotes.Count > 0)
                {
                    return await PlayVoiceNoteAsync(userId, person.Name, autoPlay: true);
                }
            }
        }

        // Pattern: "play voice note of [person]" or "play [person]'s voice"
        var playVoicePattern = new Regex(@"play\s+(?:voice\s+note\s+of|voice\s+of|)\s*([a-z\s]+)", RegexOptions.IgnoreCase);
        var playMatch = playVoicePattern.Match(text);
        if (playMatch.Success)
        {
            var personName = playMatch.Groups[1].Value.Trim();
            return await PlayVoiceNoteAsync(userId, personName);
        }

        // Pattern: "hear [person]'s voice" or "hear [person]"
        var hearPersonPattern = new Regex(@"hear\s+(?:my\s+)?([a-z\s]+?)(?:'s\s+voice|voice)?", RegexOptions.IgnoreCase);
        var hearPersonMatch = hearPersonPattern.Match(text);
        if (hearPersonMatch.Success)
        {
            var personName = hearPersonMatch.Groups[1].Value.Trim();
            // Remove common words
            personName = personName.Replace("best friend", "").Replace("friend", "").Trim();
            if (!string.IsNullOrEmpty(personName))
            {
                return await PlayVoiceNoteAsync(userId, personName, autoPlay: true);
            }
        }

        // Pattern: "I miss [person]" or "missing [person]" or "I miss my [person]"
        var missPattern = new Regex(@"(?:i\s+miss|missing|miss)\s+(?:my\s+)?([a-z\s]+)", RegexOptions.IgnoreCase);
        var missMatch = missPattern.Match(text);
        if (missMatch.Success)
        {
            var personName = missMatch.Groups[1].Value.Trim();
            // Handle common terms like "love", "loved one", etc.
            if (personName.Equals("love", StringComparison.OrdinalIgnoreCase) || 
                personName.Equals("loved one", StringComparison.OrdinalIgnoreCase))
            {
                personName = "your love";
            }
            return await HandleMissingPersonAsync(userId, personName);
        }

        // Pattern: "remember [person] as [relationship]" or "remember [person]"
        var rememberPattern = new Regex(@"remember\s+([a-z\s]+?)(?:\s+as\s+([a-z\s]+))?", RegexOptions.IgnoreCase);
        var rememberMatch = rememberPattern.Match(text);
        if (rememberMatch.Success)
        {
            var personName = rememberMatch.Groups[1].Value.Trim();
            var relationship = rememberMatch.Groups[2].Success ? rememberMatch.Groups[2].Value.Trim() : null;
            return await RememberPersonAsync(userId, personName, relationship);
        }

        // Pattern: "tell me about [person]" or "who is [person]" or "talk about [person]" or just "[person]" when mentioned
        var tellAboutPattern = new Regex(@"(?:tell\s+me\s+about|who\s+is|talk\s+about|about)\s+([a-z\s]+)", RegexOptions.IgnoreCase);
        var tellMatch = tellAboutPattern.Match(text);
        if (tellMatch.Success)
        {
            var personName = tellMatch.Groups[1].Value.Trim();
            return await TellAboutPersonAsync(userId, personName);
        }
        
        // Pattern: When user mentions a person by name (e.g., "dilshika", "her", "my best friend")
        // Check if any known person is mentioned
        var knownPeople = _personMemory.GetAllPeople(userId);
        foreach (var knownPerson in knownPeople)
        {
            // Check if person name is mentioned in text
            if (text.Contains(knownPerson.Name.ToLower()) || 
                (knownPerson.Relationship != null && text.Contains(knownPerson.Relationship.ToLower())))
            {
                // If they have voice notes and user seems to want to hear them, play automatically
                if (text.Contains("hear") || text.Contains("voice") || text.Contains("miss"))
                {
                    var voiceNotes = _voiceNoteService.GetVoiceNotesForPerson(userId, knownPerson.Name);
                    if (voiceNotes.Count > 0)
                    {
                        return await PlayVoiceNoteAsync(userId, knownPerson.Name, autoPlay: true);
                    }
                }
                
                // Otherwise, be ready to talk about them
                return await TellAboutPersonAsync(userId, knownPerson.Name);
            }
        }

        // Pattern: "save voice note" or "record voice"
        if (text.Contains("save voice") || text.Contains("record voice") || text.Contains("store voice"))
        {
            return new ActionResult
            {
                ActionType = "request_voice_recording",
                Message = "I'm ready to record a voice note. Please use the voice recording feature in the interface.",
                Success = true
            };
        }

        return null; // No action detected
    }

    /// <summary>
    /// Plays voice notes for a person.
    /// </summary>
    private Task<ActionResult> PlayVoiceNoteAsync(string userId, string personName, bool autoPlay = false)
    {
        var voiceNotes = _voiceNoteService.GetVoiceNotesForPerson(userId, personName);
        
        if (voiceNotes.Count == 0)
        {
            // Try fuzzy match
            var allPeople = _personMemory.GetAllPeople(userId);
            var matchedPerson = allPeople.FirstOrDefault(p => 
                p.Name.Equals(personName, StringComparison.OrdinalIgnoreCase) ||
                p.Name.ToLower().Contains(personName.ToLower()) ||
                personName.ToLower().Contains(p.Name.ToLower()));
            
            if (matchedPerson != null)
            {
                voiceNotes = _voiceNoteService.GetVoiceNotesForPerson(userId, matchedPerson.Name);
                personName = matchedPerson.Name;
            }
        }
        
        if (voiceNotes.Count == 0)
        {
            return Task.FromResult(new ActionResult
            {
                ActionType = "play_voice",
                Message = $"I don't have any voice notes for {personName}. Would you like to record one?",
                Success = false
            });
        }

        var note = voiceNotes.First();
        var person = _personMemory.GetPerson(userId, personName);
        
        var message = autoPlay 
            ? $"I understand you want to hear {personName}'s voice. Playing it now... 💚"
            : $"Playing voice note from {personName}...";
        
        // Add context about the person if available
        if (person != null && !string.IsNullOrEmpty(person.Relationship))
        {
            message += $" Your {person.Relationship} is special to you.";
        }
        
        return Task.FromResult(new ActionResult
        {
            ActionType = "play_voice",
            Message = message,
            Success = true,
            Parameters = new Dictionary<string, object>
            {
                { "voiceNoteId", note.Id },
                { "personName", personName },
                { "filePath", note.FilePath },
                { "totalNotes", voiceNotes.Count },
                { "autoPlay", autoPlay },
                { "relationship", person?.Relationship ?? "" },
                { "allVoiceNotes", voiceNotes.Select(n => new { n.Id, n.Description, n.RecordedAt }).ToList() }
            }
        });
    }

    /// <summary>
    /// Handles when user misses someone - plays voice notes and provides comfort.
    /// </summary>
    private Task<ActionResult> HandleMissingPersonAsync(string userId, string personName)
    {
        // Try fuzzy match first
        var allPeople = _personMemory.GetAllPeople(userId);
        var matchedPerson = allPeople.FirstOrDefault(p => 
            p.Name.Equals(personName, StringComparison.OrdinalIgnoreCase) ||
            p.Name.ToLower().Contains(personName.ToLower()) ||
            personName.ToLower().Contains(p.Name.ToLower()));
        
        if (matchedPerson != null)
        {
            personName = matchedPerson.Name;
        }
        
        var person = _personMemory.GetPerson(userId, personName);
        var voiceNotes = _voiceNoteService.GetVoiceNotesForPerson(userId, personName);

        var message = $"I understand you miss {personName}.";
        
        if (person != null && !string.IsNullOrEmpty(person.Relationship))
        {
            message += $" Your {person.Relationship} is special to you.";
        }

        if (voiceNotes.Count > 0)
        {
            // Automatically play the voice note
            var note = voiceNotes.First();
            message += $" I have {voiceNotes.Count} voice note(s) from {personName}. Playing one now... 💚";
            
            return Task.FromResult(new ActionResult
            {
                ActionType = "missing_person",
                Message = message,
                Success = true,
                Parameters = new Dictionary<string, object>
                {
                    { "personName", personName },
                    { "hasVoiceNotes", true },
                    { "voiceNoteCount", voiceNotes.Count },
                    { "voiceNoteId", note.Id },
                    { "filePath", note.FilePath },
                    { "autoPlay", true },
                    { "relationship", person?.Relationship ?? "" },
                    { "voiceNotes", voiceNotes.Select(n => new { n.Id, n.Description, n.RecordedAt }).ToList() }
                }
            });
        }
        else
        {
            message += $" I don't have any voice notes saved, but I'm here to listen if you want to talk about {personName}.";
        }

        return Task.FromResult(new ActionResult
        {
            ActionType = "missing_person",
            Message = message,
            Success = true,
            Parameters = new Dictionary<string, object>
            {
                { "personName", personName },
                { "hasVoiceNotes", false },
                { "voiceNoteCount", 0 },
                { "relationship", person?.Relationship ?? "" }
            }
        });
    }

    /// <summary>
    /// Remembers a person.
    /// </summary>
    private Task<ActionResult> RememberPersonAsync(string userId, string personName, string? relationship)
    {
        var person = _personMemory.RememberPerson(userId, personName, relationship);
        
        var message = $"I'll remember {personName}";
        if (!string.IsNullOrEmpty(relationship))
        {
            message += $" as your {relationship}";
        }
        message += ".";

        return Task.FromResult(new ActionResult
        {
            ActionType = "remember_person",
            Message = message,
            Success = true,
            Parameters = new Dictionary<string, object>
            {
                { "personName", personName },
                { "personId", person.Id },
                { "relationship", relationship ?? "unknown" }
            }
        });
    }

    /// <summary>
    /// Tells about a person from memory.
    /// </summary>
    private Task<ActionResult> TellAboutPersonAsync(string userId, string personName)
    {
        // Try fuzzy match
        var allPeople = _personMemory.GetAllPeople(userId);
        var matchedPerson = allPeople.FirstOrDefault(p => 
            p.Name.Equals(personName, StringComparison.OrdinalIgnoreCase) ||
            p.Name.ToLower().Contains(personName.ToLower()) ||
            personName.ToLower().Contains(p.Name.ToLower()));
        
        if (matchedPerson != null)
        {
            personName = matchedPerson.Name;
        }
        
        var person = _personMemory.GetPerson(userId, personName);
        var voiceNotes = _voiceNoteService.GetVoiceNotesForPerson(userId, personName);
        
        if (person == null)
        {
            return Task.FromResult(new ActionResult
            {
                ActionType = "tell_about_person",
                Message = $"I don't have any information about {personName} yet. Would you like to tell me about them?",
                Success = false
            });
        }

        var message = $"I remember {person.Name}";
        if (!string.IsNullOrEmpty(person.Relationship))
        {
            message += $". They're your {person.Relationship}";
        }
        if (!string.IsNullOrEmpty(person.Notes))
        {
            message += $". {person.Notes}";
        }
        if (voiceNotes.Count > 0)
        {
            message += $" You have {voiceNotes.Count} voice note(s) from {person.Name}. Would you like to hear them?";
        }
        else
        {
            message += ". I'm here if you want to talk about them.";
        }

        return Task.FromResult(new ActionResult
        {
            ActionType = "tell_about_person",
            Message = message,
            Success = true,
            Parameters = new Dictionary<string, object>
            {
                { "personName", person.Name },
                { "relationship", person.Relationship ?? "unknown" },
                { "notes", person.Notes ?? "" },
                { "voiceNoteCount", voiceNotes.Count },
                { "hasVoiceNotes", voiceNotes.Count > 0 },
                { "voiceNotes", voiceNotes.Select(n => new { n.Id, n.Description, n.RecordedAt }).ToList() }
            }
        });
    }
}

/// <summary>
/// Result of an action execution.
/// </summary>
public class ActionResult
{
    public string ActionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
}

