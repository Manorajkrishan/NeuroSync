using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;
using NeuroSync.Core;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VoiceController : ControllerBase
{
    private readonly VoiceService _voiceService;
    private readonly PersonMemory _personMemory;
    private readonly ILogger<VoiceController> _logger;

    public VoiceController(
        VoiceService voiceService,
        PersonMemory personMemory,
        ILogger<VoiceController> logger)
    {
        _voiceService = voiceService;
        _personMemory = personMemory;
        _logger = logger;
    }

    /// <summary>
    /// Generates speech from text. Can use cloned voice if voiceId is provided.
    /// </summary>
    [HttpPost("speak")]
    public async Task<IActionResult> Speak([FromBody] SpeakRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { error = "Text is required" });
        }

        try
        {
            var userId = request.UserId ?? "default";
            string? voiceId = null;

            // If personName is provided, try to find their cloned voice
            if (!string.IsNullOrEmpty(request.PersonName))
            {
                var person = _personMemory.GetPerson(userId, request.PersonName);
                if (person != null && person.Attributes.ContainsKey("VoiceCloneId"))
                {
                    voiceId = person.Attributes["VoiceCloneId"]?.ToString();
                }
            }

            var response = await _voiceService.SpeakAsync(request.Text, voiceId, userId);

            // If audio file was generated, return it
            if (!string.IsNullOrEmpty(response.AudioPath) && System.IO.File.Exists(response.AudioPath))
            {
                var audioBytes = await System.IO.File.ReadAllBytesAsync(response.AudioPath);
                return File(audioBytes, "audio/mpeg", "speech.mp3");
            }

            // Otherwise, return text for frontend TTS
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating speech");
            return StatusCode(500, new { error = "Failed to generate speech" });
        }
    }

    /// <summary>
    /// Clones a voice from an audio sample.
    /// </summary>
    [HttpPost("clone")]
    public async Task<IActionResult> CloneVoice(
        [FromForm] string userId,
        [FromForm] string personName,
        [FromForm] IFormFile audioFile)
    {
        if (audioFile == null || audioFile.Length == 0)
        {
            return BadRequest(new { error = "Audio file is required" });
        }

        if (string.IsNullOrWhiteSpace(personName))
        {
            return BadRequest(new { error = "Person name is required" });
        }

        try
        {
            using (var stream = audioFile.OpenReadStream())
            {
                var result = await _voiceService.CloneVoiceAsync(userId, personName, stream, audioFile.FileName);

                if (result.Success && !string.IsNullOrEmpty(result.VoiceId))
                {
                    // Associate voice clone with person
                    var person = _personMemory.RememberPerson(userId, personName);
                    person.Attributes["VoiceCloneId"] = result.VoiceId;
                    person.Attributes["VoiceClonedAt"] = DateTime.UtcNow;

                    return Ok(new
                    {
                        success = true,
                        voiceId = result.VoiceId,
                        personName = result.PersonName,
                        message = result.Message
                    });
                }

                // Return detailed error message
                _logger.LogWarning($"Voice cloning failed for {personName}: {result.Error}");
                return BadRequest(new { 
                    error = result.Error ?? "Failed to clone voice",
                    details = result.Error
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning voice");
            return StatusCode(500, new { error = "Failed to clone voice" });
        }
    }

    /// <summary>
    /// Gets all cloned voices for a user.
    /// </summary>
    [HttpGet("clones")]
    public IActionResult GetClonedVoices([FromQuery] string userId)
    {
        try
        {
            var voices = _voiceService.GetClonedVoices(userId);
            return Ok(new { voices });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cloned voices");
            return StatusCode(500, new { error = "Failed to get cloned voices" });
        }
    }
}

/// <summary>
/// Request to generate speech.
/// </summary>
public class SpeakRequest
{
    public string Text { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? PersonName { get; set; }
    public string? VoiceId { get; set; }
}
