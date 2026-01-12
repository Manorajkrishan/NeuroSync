using NeuroSync.Core;
using System.Collections.Concurrent;

namespace NeuroSync.Api.Services;

/// <summary>
/// Service for managing voice notes - storing, retrieving, and playing voice recordings.
/// </summary>
public class VoiceNoteService
{
    private readonly ConcurrentDictionary<string, List<VoiceNote>> _voiceNotes = new();
    private readonly string _storagePath;
    private readonly ILogger<VoiceNoteService> _logger;

    public VoiceNoteService(ILogger<VoiceNoteService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _storagePath = Path.Combine(environment.ContentRootPath, "VoiceNotes");
        
        // Ensure storage directory exists
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            _logger.LogInformation($"Created voice notes directory: {_storagePath}");
        }
    }

    /// <summary>
    /// Stores a voice note file and creates a VoiceNote record.
    /// </summary>
    public async Task<VoiceNote> StoreVoiceNoteAsync(
        string userId, 
        string personName, 
        Stream audioStream, 
        string fileName,
        string? description = null,
        string? transcript = null)
    {
        var voiceNote = new VoiceNote
        {
            UserId = userId,
            PersonName = personName,
            Description = description,
            Transcript = transcript,
            Metadata = new Dictionary<string, string>
            {
                { "fileName", fileName },
                { "format", Path.GetExtension(fileName).TrimStart('.') }
            }
        };

        // Save audio file
        var filePath = Path.Combine(_storagePath, $"{voiceNote.Id}{Path.GetExtension(fileName)}");
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await audioStream.CopyToAsync(fileStream);
        }

        voiceNote.FilePath = filePath;

        // Store in memory
        var userNotes = _voiceNotes.GetOrAdd(userId, _ => new List<VoiceNote>());
        userNotes.Add(voiceNote);

        _logger.LogInformation($"Stored voice note {voiceNote.Id} for person {personName} (user: {userId})");
        return voiceNote;
    }

    /// <summary>
    /// Gets all voice notes for a user, optionally filtered by person name.
    /// </summary>
    public List<VoiceNote> GetVoiceNotes(string userId, string? personName = null)
    {
        if (!_voiceNotes.TryGetValue(userId, out var notes))
        {
            return new List<VoiceNote>();
        }

        if (string.IsNullOrEmpty(personName))
        {
            return notes.ToList();
        }

        return notes.Where(n => n.PersonName.Equals(personName, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Gets a specific voice note by ID.
    /// </summary>
    public VoiceNote? GetVoiceNote(string userId, string voiceNoteId)
    {
        if (!_voiceNotes.TryGetValue(userId, out var notes))
        {
            return null;
        }

        return notes.FirstOrDefault(n => n.Id == voiceNoteId);
    }

    /// <summary>
    /// Gets the file path for a voice note (for playback).
    /// </summary>
    public string? GetVoiceNoteFilePath(string userId, string voiceNoteId)
    {
        var note = GetVoiceNote(userId, voiceNoteId);
        return note != null && File.Exists(note.FilePath) ? note.FilePath : null;
    }

    /// <summary>
    /// Deletes a voice note.
    /// </summary>
    public bool DeleteVoiceNote(string userId, string voiceNoteId)
    {
        if (!_voiceNotes.TryGetValue(userId, out var notes))
        {
            return false;
        }

        var note = notes.FirstOrDefault(n => n.Id == voiceNoteId);
        if (note == null)
        {
            return false;
        }

        // Delete file
        if (File.Exists(note.FilePath))
        {
            try
            {
                File.Delete(note.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to delete voice note file: {note.FilePath}");
            }
        }

        // Remove from memory
        notes.Remove(note);
        _logger.LogInformation($"Deleted voice note {voiceNoteId} (user: {userId})");
        return true;
    }

    /// <summary>
    /// Gets voice notes for a person (when missing them).
    /// </summary>
    public List<VoiceNote> GetVoiceNotesForPerson(string userId, string personName)
    {
        return GetVoiceNotes(userId, personName)
            .OrderByDescending(n => n.RecordedAt)
            .ToList();
    }
}

