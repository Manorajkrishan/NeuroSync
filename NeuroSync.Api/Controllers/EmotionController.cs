using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NeuroSync.Api.Hubs;
using NeuroSync.Api.Services;
using NeuroSync.Core;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmotionController : ControllerBase
{
    private readonly EmotionDetectionService _emotionDetectionService;
    private readonly DecisionEngine _decisionEngine;
    private readonly IHubContext<EmotionHub> _hubContext;
    private readonly ILogger<EmotionController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly UserProfileService? _userProfileService;

    public EmotionController(
        EmotionDetectionService emotionDetectionService,
        DecisionEngine decisionEngine,
        IHubContext<EmotionHub> hubContext,
        ILogger<EmotionController> logger,
        IWebHostEnvironment environment,
        UserProfileService? userProfileService = null)
    {
        _emotionDetectionService = emotionDetectionService;
        _decisionEngine = decisionEngine;
        _hubContext = hubContext;
        _logger = logger;
        _environment = environment;
        _userProfileService = userProfileService;
    }

    [HttpPost("detect")]
    public async Task<IActionResult> DetectEmotion([FromBody] EmotionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { error = "Text is required" });
        }

        try
        {
            // Get user ID from request or use default
            var userId = request.UserId ?? "default";

            // Check for action requests first (like "play voice note", "remember person", etc.)
            var actionExecutor = HttpContext.RequestServices.GetService<ActionExecutor>();
            Services.ActionResult? actionResult = null;
            if (actionExecutor != null)
            {
                actionResult = await actionExecutor.ExecuteActionAsync(userId, request.Text);
            }

            // Detect emotion
            var emotionResult = _emotionDetectionService.DetectEmotion(request.Text);
            
            // Collect real-world data for continuous learning (only high-confidence predictions)
            if (emotionResult.Confidence >= 0.7f)
            {
                var dataCollector = HttpContext.RequestServices.GetService<RealWorldDataCollector>();
                dataCollector?.CollectData(request.Text, emotionResult.Emotion, emotionResult.Confidence);
            }

            // Generate adaptive response with emotional intelligence
            var adaptiveResponse = _decisionEngine.GenerateResponse(emotionResult, userId, request.Text);

            // Get IoT actions (async to ensure real device parameters are populated)
            var iotActions = await _decisionEngine.GetIoTActionsAsync(emotionResult.Emotion);

            // Send real-time updates via SignalR
            await _hubContext.Clients.All.SendAsync("EmotionDetected", emotionResult);
            await _hubContext.Clients.All.SendAsync("AdaptiveResponse", adaptiveResponse);
            
            if (actionResult != null)
            {
                await _hubContext.Clients.All.SendAsync("ActionExecuted", actionResult);
            }
            
            foreach (var action in iotActions)
            {
                await _hubContext.Clients.All.SendAsync("IoTAction", action);
            }

            return Ok(new
            {
                emotion = emotionResult,
                adaptiveResponse = adaptiveResponse,
                iotActions = iotActions,
                actionResult = actionResult
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing emotion detection: {Message}", ex.Message);
            _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            
            // Return more detailed error in development
            var errorMessage = _environment.IsDevelopment() 
                ? $"An error occurred: {ex.Message}" 
                : "An error occurred while processing the request";
            
            return StatusCode(500, new { error = errorMessage, details = _environment.IsDevelopment() ? ex.ToString() : null });
        }
    }

    [HttpGet("types")]
    public IActionResult GetEmotionTypes()
    {
        var types = Enum.GetValues(typeof(EmotionType))
            .Cast<EmotionType>()
            .Select(e => new { name = e.ToString(), value = (int)e })
            .ToList();

        return Ok(types);
    }

    [HttpPost("facial")]
    public async Task<IActionResult> DetectFacialEmotion([FromBody] FacialEmotionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Emotion))
        {
            return BadRequest(new { error = "Emotion is required" });
        }

        try
        {
            var userId = request.UserId ?? "default";

            // Convert string emotion to EmotionType enum
            if (!Enum.TryParse<EmotionType>(request.Emotion, true, out var emotionType))
            {
                return BadRequest(new { error = $"Invalid emotion type: {request.Emotion}" });
            }

            // Create emotion result from facial expression
            var emotionResult = new EmotionResult
            {
                Emotion = emotionType,
                Confidence = request.Confidence,
                OriginalText = $"Facial expression detected: {request.Emotion}"
            };

            // Generate adaptive response with emotional intelligence
            var adaptiveResponse = _decisionEngine.GenerateResponse(emotionResult, userId, $"I'm feeling {request.Emotion}");

            // Get IoT actions (async to ensure real device parameters are populated)
            var iotActions = await _decisionEngine.GetIoTActionsAsync(emotionResult.Emotion);

            // Send real-time updates via SignalR
            await _hubContext.Clients.All.SendAsync("EmotionDetected", emotionResult);
            await _hubContext.Clients.All.SendAsync("AdaptiveResponse", adaptiveResponse);
            
            foreach (var action in iotActions)
            {
                await _hubContext.Clients.All.SendAsync("IoTAction", action);
            }

            // Collect real-world data for continuous learning
            if (emotionResult.Confidence >= 0.7f)
            {
                var dataCollector = HttpContext.RequestServices.GetService<RealWorldDataCollector>();
                dataCollector?.CollectData($"I'm feeling {request.Emotion}", emotionResult.Emotion, emotionResult.Confidence);
            }

            return Ok(new
            {
                emotion = emotionResult,
                adaptiveResponse = adaptiveResponse,
                iotActions = iotActions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing facial emotion detection: {Message}", ex.Message);
            
            var errorMessage = _environment.IsDevelopment() 
                ? $"An error occurred: {ex.Message}" 
                : "An error occurred while processing the request";
            
            return StatusCode(500, new { error = errorMessage, details = _environment.IsDevelopment() ? ex.ToString() : null });
        }
    }
}

