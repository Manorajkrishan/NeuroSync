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

    /// <summary>
    /// Multi-layer emotion detection endpoint
    /// Combines Visual, Audio, Biometric, and Contextual layers
    /// </summary>
    [HttpPost("multilayer")]
    public async Task<IActionResult> DetectMultiLayerEmotion([FromBody] MultiLayerEmotionRequest request)
    {
        try
        {
            var userId = request.UserId ?? "default";

            // Check ethical consent
            var ethicalFramework = HttpContext.RequestServices.GetService<EthicalAIFrameworkService>();
            if (ethicalFramework != null && !ethicalFramework.HasConsent(userId, ConsentType.EmotionSensing))
            {
                return BadRequest(new { error = "Emotion sensing consent required. Please provide consent first." });
            }

            // Layer 1: Visual
            VisualEmotionData? visualData = null;
            if (!string.IsNullOrEmpty(request.VisualEmotion) && request.VisualConfidence.HasValue)
            {
                if (ethicalFramework != null && !ethicalFramework.HasConsent(userId, ConsentType.VisualLayer))
                {
                    return BadRequest(new { error = "Visual layer consent required." });
                }

                if (Enum.TryParse<EmotionType>(request.VisualEmotion, true, out var visualEmotion))
                {
                    visualData = new VisualEmotionData
                    {
                        Emotion = visualEmotion,
                        Confidence = request.VisualConfidence.Value
                    };
                }
            }

            // Layer 2: Audio
            AudioEmotionData? audioData = null;
            var audioService = HttpContext.RequestServices.GetService<AdvancedAudioAnalysisService>();
            if (audioService != null && (!string.IsNullOrEmpty(request.AudioTranscript) || request.AudioPitch.HasValue))
            {
                if (ethicalFramework != null && !ethicalFramework.HasConsent(userId, ConsentType.AudioLayer))
                {
                    return BadRequest(new { error = "Audio layer consent required." });
                }

                audioData = audioService.AnalyzeAudio(
                    audioData: null,
                    textTranscript: request.AudioTranscript,
                    pitch: request.AudioPitch,
                    volume: request.AudioVolume,
                    speechRate: request.AudioSpeechRate);
            }

            // Layer 3: Biometric
            BiometricEmotionData? biometricData = null;
            var biometricService = HttpContext.RequestServices.GetService<BiometricIntegrationService>();
            if (biometricService != null && (request.HeartRate.HasValue || request.SkinConductivity.HasValue))
            {
                if (ethicalFramework != null && !ethicalFramework.HasConsent(userId, ConsentType.BiometricLayer))
                {
                    return BadRequest(new { error = "Biometric layer consent required." });
                }

                biometricData = biometricService.AnalyzeBiometrics(
                    heartRate: request.HeartRate,
                    hrv: request.HRV,
                    skinConductivity: request.SkinConductivity,
                    temperature: request.Temperature);
            }

            // Layer 4: Contextual
            ContextualEmotionData? contextualData = null;
            var contextualService = HttpContext.RequestServices.GetService<ContextualAwarenessService>();
            if (contextualService != null && (!string.IsNullOrEmpty(request.ActivityType) || request.TaskIntensity.HasValue))
            {
                contextualData = contextualService.AnalyzeContext(
                    userId: userId,
                    activityType: request.ActivityType,
                    activityIntensity: request.ActivityIntensity,
                    taskIntensity: request.TaskIntensity,
                    taskComplexity: request.TaskComplexity);
            }

            // If text provided, use existing emotion detection as additional layer
            EmotionResult? textEmotionResult = null;
            if (!string.IsNullOrEmpty(request.Text))
            {
                textEmotionResult = _emotionDetectionService.DetectEmotion(request.Text);
                // Add to contextual layer if available
                if (contextualData == null && contextualService != null)
                {
                    contextualData = contextualService.AnalyzeContext(userId: userId);
                }
            }

            // Fuse all layers
            var fusionService = HttpContext.RequestServices.GetService<MultiLayerEmotionFusionService>();
            if (fusionService == null)
            {
                return StatusCode(500, new { error = "Fusion service not available" });
            }

            var fusedResult = fusionService.FuseEmotions(
                visual: visualData,
                audio: audioData,
                biometric: biometricData,
                contextual: contextualData,
                userId: userId);

            // Generate adaptive response
            var emotionResultForResponse = textEmotionResult ?? new EmotionResult
            {
                Emotion = fusedResult.PrimaryEmotion,
                Confidence = fusedResult.OverallConfidence,
                OriginalText = request.Text ?? "Multi-layer emotion detection"
            };

            var adaptiveResponse = _decisionEngine.GenerateResponse(emotionResultForResponse, userId, request.Text);

            // Get advanced actions
            var actionOrchestrator = HttpContext.RequestServices.GetService<AdvancedActionOrchestrator>();
            var iotActions = actionOrchestrator != null
                ? await actionOrchestrator.OrchestrateActions(fusedResult, userId)
                : await _decisionEngine.GetIoTActionsAsync(fusedResult.PrimaryEmotion);

            // Send real-time updates via SignalR
            await _hubContext.Clients.All.SendAsync("EmotionDetected", emotionResultForResponse);
            await _hubContext.Clients.All.SendAsync("AdaptiveResponse", adaptiveResponse);
            await _hubContext.Clients.All.SendAsync("MultiLayerEmotion", fusedResult);
            
            foreach (var action in iotActions)
            {
                await _hubContext.Clients.All.SendAsync("IoTAction", action);
            }

            return Ok(new
            {
                fusedEmotion = fusedResult,
                emotion = emotionResultForResponse,
                adaptiveResponse = adaptiveResponse,
                iotActions = iotActions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing multi-layer emotion detection: {Message}", ex.Message);
            
            var errorMessage = _environment.IsDevelopment() 
                ? $"An error occurred: {ex.Message}" 
                : "An error occurred while processing the request";
            
            return StatusCode(500, new { error = errorMessage, details = _environment.IsDevelopment() ? ex.ToString() : null });
        }
    }
}

