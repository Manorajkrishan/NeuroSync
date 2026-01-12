namespace NeuroSync.Core;

/// <summary>
/// Represents a stored voice note.
/// </summary>
public class VoiceNote
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string PersonName { get; set; } = string.Empty; // Who the voice note is from/about
    public string FilePath { get; set; } = string.Empty;
    public string? Transcript { get; set; } // Optional text transcript
    public DateTime RecordedAt { get; set; }
    public string? Description { get; set; } // User's description of the note
    public Dictionary<string, string> Metadata { get; set; } = new(); // Duration, format, etc.
    
    public VoiceNote()
    {
        RecordedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a person in the user's memory.
/// </summary>
public class Person
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Relationship { get; set; } // Friend, Family, Partner, etc.
    public string? Notes { get; set; } // Things to remember about this person
    public List<string> VoiceNoteIds { get; set; } = new(); // Associated voice notes
    public DateTime CreatedAt { get; set; }
    public DateTime LastMentioned { get; set; }
    public Dictionary<string, object> Attributes { get; set; } = new(); // Custom attributes
    
    public Person()
    {
        CreatedAt = DateTime.UtcNow;
        LastMentioned = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a user action request.
/// </summary>
public class ActionRequest
{
    public string ActionType { get; set; } = string.Empty; // play_voice, remember_person, execute_command, etc.
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? PersonName { get; set; } // If action involves a person
    public string? VoiceNoteId { get; set; } // If action involves a voice note
}

