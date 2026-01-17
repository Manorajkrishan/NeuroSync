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

            // Get conversation memory to check context
            var conversationMemory = HttpContext.RequestServices.GetService<ConversationMemory>();
            var context = conversationMemory?.GetOrCreateContext(userId);
            
            // Smart response logic: Only respond to significant emotion changes or strong emotions
            // Don't spam responses for neutral states or minor fluctuations
            bool shouldRespond = false;
            string? reason = null;
            
            // Check if this is a significant emotion change
            if (context != null && context.LastEmotion.HasValue)
            {
                var lastEmotion = context.LastEmotion.Value;
                var timeSinceLastResponse = context.LastInteraction.HasValue 
                    ? (DateTime.UtcNow - context.LastInteraction.Value).TotalSeconds 
                    : 999;
                
                // Respond if:
                // 1. Emotion changed significantly (not neutral -> neutral)
                if (lastEmotion != emotionType && emotionType != EmotionType.Neutral)
                {
                    shouldRespond = true;
                    reason = "emotion_change";
                }
                // 2. Strong negative emotion detected (always respond)
                else if ((emotionType == EmotionType.Sad || emotionType == EmotionType.Anxious || 
                          emotionType == EmotionType.Angry) && request.Confidence > 0.7f)
                {
                    shouldRespond = true;
                    reason = "strong_negative_emotion";
                }
                // 3. Strong positive emotion (happy/excited) with high confidence
                else if ((emotionType == EmotionType.Happy || emotionType == EmotionType.Excited) && 
                         request.Confidence > 0.8f && timeSinceLastResponse > 10)
                {
                    shouldRespond = true;
                    reason = "strong_positive_emotion";
                }
                // 4. No response in last 30 seconds and user seems engaged (not just neutral)
                else if (timeSinceLastResponse > 30 && emotionType != EmotionType.Neutral && request.Confidence > 0.75f)
                {
                    shouldRespond = true;
                    reason = "periodic_check";
                }
                // 5. Don't respond to neutral unless it's been a long time (60+ seconds)
                else if (emotionType == EmotionType.Neutral && timeSinceLastResponse > 60)
                {
                    shouldRespond = true;
                    reason = "periodic_neutral_check";
                }
            }
            else
            {
                // First interaction - only respond to strong emotions, not neutral
                if (emotionType != EmotionType.Neutral && request.Confidence > 0.7f)
                {
                    shouldRespond = true;
                    reason = "first_interaction";
                }
            }

            // Create emotion result from facial expression
            var emotionResult = new EmotionResult
            {
                Emotion = emotionType,
                Confidence = request.Confidence,
                OriginalText = $"Facial expression detected: {request.Emotion}"
            };

            // Only generate and send response if we should respond
            AdaptiveResponse? adaptiveResponse = null;
            List<IoTAction>? iotActions = null;
            
            if (shouldRespond)
            {
                _logger.LogInformation($"Responding to facial emotion: {emotionType} (confidence: {request.Confidence:P2}, reason: {reason})");
                
                // Generate adaptive response with emotional intelligence
                adaptiveResponse = _decisionEngine.GenerateResponse(emotionResult, userId, $"I'm feeling {request.Emotion}");

                // Get IoT actions (async to ensure real device parameters are populated)
                iotActions = await _decisionEngine.GetIoTActionsAsync(emotionResult.Emotion);

                // Send real-time updates via SignalR
                await _hubContext.Clients.All.SendAsync("EmotionDetected", emotionResult);
                await _hubContext.Clients.All.SendAsync("AdaptiveResponse", adaptiveResponse);
                
                foreach (var action in iotActions)
                {
                    await _hubContext.Clients.All.SendAsync("IoTAction", action);
                }
                
                // Update conversation memory
                if (conversationMemory != null && adaptiveResponse != null)
                {
                    conversationMemory.AddEntry(userId, $"Facial: {request.Emotion}", emotionResult, adaptiveResponse);
                }
            }
            else
            {
                // Still update emotion detection for tracking, but don't send response
                _logger.LogDebug($"Facial emotion detected but not responding: {emotionType} (confidence: {request.Confidence:P2}) - too soon or neutral");
                
                // Update last emotion in context without sending response
                if (context != null)
                {
                    context.LastEmotion = emotionType;
                }
            }

            // Collect real-world data for continuous learning (always, even if not responding)
            if (emotionResult.Confidence >= 0.7f)
            {
                var dataCollector = HttpContext.RequestServices.GetService<RealWorldDataCollector>();
                dataCollector?.CollectData($"I'm feeling {request.Emotion}", emotionResult.Emotion, emotionResult.Confidence);
            }

            return Ok(new
            {
                emotion = emotionResult,
                adaptiveResponse = adaptiveResponse,
                iotActions = iotActions,
                responded = shouldRespond,
                reason = reason
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

