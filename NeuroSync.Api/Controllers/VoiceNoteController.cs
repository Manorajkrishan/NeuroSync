using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;
using NeuroSync.Core;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VoiceNoteController : ControllerBase
{
    private readonly VoiceNoteService _voiceNoteService;
    private readonly PersonMemory _personMemory;
    private readonly ILogger<VoiceNoteController> _logger;

    public VoiceNoteController(
        VoiceNoteService voiceNoteService,
        PersonMemory personMemory,
        ILogger<VoiceNoteController> logger)
    {
        _voiceNoteService = voiceNoteService;
        _personMemory = personMemory;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadVoiceNote(
        [FromForm] string userId,
        [FromForm] string personName,
        [FromForm] IFormFile audioFile,
        [FromForm] string? description = null,
        [FromForm] string? transcript = null)
    {
        if (audioFile == null || audioFile.Length == 0)
        {
            return BadRequest(new { error = "Audio file is required" });
        }

        try
        {
            using (var stream = audioFile.OpenReadStream())
            {
                var voiceNote = await _voiceNoteService.StoreVoiceNoteAsync(
                    userId,
                    personName,
                    stream,
                    audioFile.FileName,
                    description,
                    transcript);

                // Associate with person
                _personMemory.AssociateVoiceNote(userId, personName, voiceNote.Id);

                return Ok(new
                {
                    success = true,
                    voiceNote = new
                    {
                        voiceNote.Id,
                        voiceNote.PersonName,
                        voiceNote.Description,
                        voiceNote.RecordedAt
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading voice note");
            return StatusCode(500, new { error = "Failed to upload voice note" });
        }
    }

    [HttpGet("list")]
    public IActionResult ListVoiceNotes([FromQuery] string userId, [FromQuery] string? personName = null)
    {
        var notes = _voiceNoteService.GetVoiceNotes(userId, personName);
        return Ok(new { voiceNotes = notes.Select(n => new { n.Id, n.PersonName, n.Description, n.RecordedAt }) });
    }

    [HttpGet("play/{voiceNoteId}")]
    public IActionResult PlayVoiceNote([FromQuery] string userId, string voiceNoteId)
    {
        var filePath = _voiceNoteService.GetVoiceNoteFilePath(userId, voiceNoteId);
        if (filePath == null)
        {
            return NotFound(new { error = "Voice note not found" });
        }

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        var contentType = "audio/mpeg"; // Adjust based on file format
        return File(fileBytes, contentType, Path.GetFileName(filePath));
    }

    [HttpDelete("{voiceNoteId}")]
    public IActionResult DeleteVoiceNote([FromQuery] string userId, string voiceNoteId)
    {
        var success = _voiceNoteService.DeleteVoiceNote(userId, voiceNoteId);
        if (!success)
        {
            return NotFound(new { error = "Voice note not found" });
        }

        return Ok(new { success = true });
    }
}

