using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;
using NeuroSync.Core;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticController : ControllerBase
{
    private readonly EmotionDetectionService _emotionDetectionService;
    private readonly ILogger<DiagnosticController> _logger;

    public DiagnosticController(
        EmotionDetectionService emotionDetectionService,
        ILogger<DiagnosticController> logger)
    {
        _emotionDetectionService = emotionDetectionService;
        _logger = logger;
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        try
        {
            // Test basic functionality
            var testText = "I'm feeling happy!";
            var result = _emotionDetectionService.DetectEmotion(testText);
            
            return Ok(new
            {
                status = "OK",
                message = "Emotion detection service is working",
                testResult = result,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostic test failed");
            return StatusCode(500, new
            {
                status = "ERROR",
                message = "Emotion detection service failed",
                error = ex.Message,
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "NeuroSync API"
        });
    }
}

