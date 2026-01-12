using NeuroSync.Core;
using System.Collections.Concurrent;

namespace NeuroSync.Api.Services;

/// <summary>
/// Service for remembering people, relationships, and associated memories.
/// </summary>
public class PersonMemory
{
    private readonly ConcurrentDictionary<string, List<Person>> _people = new();
    private readonly ILogger<PersonMemory> _logger;

    public PersonMemory(ILogger<PersonMemory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds or updates a person in memory.
    /// </summary>
    public Person RememberPerson(string userId, string name, string? relationship = null, string? notes = null)
    {
        var people = _people.GetOrAdd(userId, _ => new List<Person>());
        
        var existing = people.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        
        if (existing != null)
        {
            // Update existing person
            if (!string.IsNullOrEmpty(relationship))
            {
                existing.Relationship = relationship;
            }
            if (!string.IsNullOrEmpty(notes))
            {
                existing.Notes = notes;
            }
            existing.LastMentioned = DateTime.UtcNow;
            _logger.LogInformation($"Updated person {name} in memory (user: {userId})");
            return existing;
        }

        // Create new person
        var person = new Person
        {
            UserId = userId,
            Name = name,
            Relationship = relationship,
            Notes = notes
        };

        people.Add(person);
        _logger.LogInformation($"Remembered new person {name} (user: {userId})");
        return person;
    }

    /// <summary>
    /// Gets a person by name.
    /// </summary>
    public Person? GetPerson(string userId, string name)
    {
        if (!_people.TryGetValue(userId, out var people))
        {
            return null;
        }

        var person = people.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (person != null)
        {
            person.LastMentioned = DateTime.UtcNow;
        }
        return person;
    }

    /// <summary>
    /// Gets all people for a user.
    /// </summary>
    public List<Person> GetAllPeople(string userId)
    {
        if (!_people.TryGetValue(userId, out var people))
        {
            return new List<Person>();
        }

        return people.OrderByDescending(p => p.LastMentioned).ToList();
    }

    /// <summary>
    /// Associates a voice note with a person.
    /// </summary>
    public void AssociateVoiceNote(string userId, string personName, string voiceNoteId)
    {
        var person = GetPerson(userId, personName);
        if (person != null && !person.VoiceNoteIds.Contains(voiceNoteId))
        {
            person.VoiceNoteIds.Add(voiceNoteId);
            _logger.LogInformation($"Associated voice note {voiceNoteId} with person {personName}");
        }
    }

    /// <summary>
    /// Gets information about a person for conversation context.
    /// </summary>
    public string? GetPersonContext(string userId, string personName)
    {
        var person = GetPerson(userId, personName);
        if (person == null)
        {
            return null;
        }

        var context = $"Person: {person.Name}";
        if (!string.IsNullOrEmpty(person.Relationship))
        {
            context += $", Relationship: {person.Relationship}";
        }
        if (!string.IsNullOrEmpty(person.Notes))
        {
            context += $", Notes: {person.Notes}";
        }
        if (person.VoiceNoteIds.Count > 0)
        {
            context += $", Has {person.VoiceNoteIds.Count} voice note(s)";
        }

        return context;
    }

    /// <summary>
    /// Searches for people by name (fuzzy match).
    /// </summary>
    public List<Person> SearchPeople(string userId, string searchTerm)
    {
        if (!_people.TryGetValue(userId, out var people))
        {
            return new List<Person>();
        }

        var term = searchTerm.ToLower();
        return people
            .Where(p => p.Name.ToLower().Contains(term) || 
                       (p.Relationship != null && p.Relationship.ToLower().Contains(term)))
            .OrderByDescending(p => p.LastMentioned)
            .ToList();
    }
}

