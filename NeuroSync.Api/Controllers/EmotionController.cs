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

        if (!UserIdSanitizer.TryNormalize(request.UserId, out var userId))
            return BadRequest(new { error = "Invalid userId (use letters, digits, _ or - only, max 64)" });

        try
        {
            var actionExecutor = HttpContext.RequestServices.GetService<ActionExecutor>();
            Services.ActionResult? actionResult = null;
            if (actionExecutor != null)
            {
                actionResult = await actionExecutor.ExecuteActionAsync(userId, request.Text);
            }

            var emotionResult = _emotionDetectionService.DetectEmotion(request.Text);

            // Self-learning: CollectData ONLY with explicit DataSharingConsent (default OFF)
            TryCollectLearningData(userId, request.Text, emotionResult);

            var adaptiveResponse = await _decisionEngine.GenerateResponseAsync(emotionResult, userId, request.Text);

            // IoT consent enforced here (not only in DecisionEngine trace)
            var iotActions = await ResolveIoTActionsAsync(userId, request.Text, emotionResult, adaptiveResponse);

            // Per-user SignalR — never Clients.All
            await EmotionHubUserScope.SendToUserAsync(_hubContext, userId, "EmotionDetected", emotionResult);
            await EmotionHubUserScope.SendToUserAsync(_hubContext, userId, "AdaptiveResponse", adaptiveResponse);

            if (actionResult != null)
            {
                await EmotionHubUserScope.SendToUserAsync(_hubContext, userId, "ActionExecuted", actionResult);
            }

            foreach (var action in iotActions)
            {
                await EmotionHubUserScope.SendToUserAsync(_hubContext, userId, "IoTAction", action);
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

            var errorMessage = _environment.IsDevelopment()
                ? $"An error occurred: {ex.Message}"
                : "An error occurred while processing the request";

            return StatusCode(500, new { error = errorMessage, details = _environment.IsDevelopment() ? ex.ToString() : null });
        }
    }

    /// <summary>
    /// Trigger model retrain (uses Data/emotions.csv + Data/realworld_emotions.csv). Retrain runs within ~5 min.
    /// </summary>
    [HttpPost("retrain")]
    public IActionResult RequestRetrain()
    {
        var dataDir = Path.Combine(_environment.ContentRootPath, "Data");
        if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
        try
        {
            System.IO.File.WriteAllText(Path.Combine(dataDir, "please_retrain"), "");
            return Ok(new { ok = true, message = "Retrain requested. Model will retrain within ~5 minutes. Restart the app to load the new model." });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write please_retrain file");
            return StatusCode(500, new { error = "Could not request retrain" });
        }
    }

    /// <summary>
    /// User corrects a wrong emotion. Helps improve accuracy (self-learning).
    /// Body: { "text": "user's message", "correctEmotion": "anxious" }
    /// </summary>
    [HttpPost("correct")]
    public IActionResult CorrectEmotion([FromBody] EmotionCorrectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Text) || string.IsNullOrWhiteSpace(request?.CorrectEmotion))
            return BadRequest(new { error = "Text and correctEmotion are required" });

        var collector = HttpContext.RequestServices.GetService<RealWorldDataCollector>();
        if (collector == null) return StatusCode(500, new { error = "Correction service not available" });

        // Explicit user correction is consent for that sample
        collector.CollectCorrection(request.Text, request.CorrectEmotion);
        return Ok(new { ok = true, message = "Thanks! This helps improve accuracy." });
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

        if (!UserIdSanitizer.TryNormalize(request.UserId, out var userId))
            return BadRequest(new { error = "Invalid userId (use letters, digits, _ or - only, max 64)" });

        try
        {
            var ethical = HttpContext.RequestServices.GetService<EthicalAIFrameworkService>();
            if (ethical != null && !ethical.HasConsent(userId, ConsentType.FaceAnalysis))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    error = "FaceAnalysisConsent is OFF. Enable it in privacy settings. Facial analysis is experimental and never drives safety decisions.",
                    experimental = true
                });
            }

            if (!Enum.TryParse<EmotionType>(request.Emotion, true, out var emotionType))
            {
                return BadRequest(new { error = $"Invalid emotion type: {request.Emotion}" });
            }

            var conversationMemory = HttpContext.RequestServices.GetService<ConversationMemory>();
            var context = conversationMemory?.GetOrCreateContext(userId);

            bool shouldRespond = false;
            string? reason = null;

            if (context != null && context.LastEmotion.HasValue)
            {
                var lastEmotion = context.LastEmotion.Value;
                var timeSinceLastResponse = context.LastInteraction.HasValue
                    ? (DateTime.UtcNow - context.LastInteraction.Value).TotalSeconds
                    : 999;

                if (lastEmotion != emotionType && emotionType != EmotionType.Neutral)
                {
                    shouldRespond = true;
                    reason = "emotion_change";
                }
                else if ((emotionType == EmotionType.Sad || emotionType == EmotionType.Anxious ||
                          emotionType == EmotionType.Angry) && request.Confidence > 0.7f)
                {
                    shouldRespond = true;
                    reason = "strong_negative_emotion";
                }
                else if ((emotionType == EmotionType.Happy || emotionType == EmotionType.Excited) &&
                         request.Confidence > 0.8f && timeSinceLastResponse > 10)
                {
                    shouldRespond = true;
                    reason = "strong_positive_emotion";
                }
                else if (timeSinceLastResponse > 30 && emotionType != EmotionType.Neutral && request.Confidence > 0.75f)
                {
                    shouldRespond = true;
                    reason = "periodic_check";
                }
                else if (emotionType == EmotionType.Neutral && timeSinceLastResponse > 60)
                {
                    shouldRespond = true;
                    reason = "periodic_neutral_check";
                }
            }
            else
            {
                if (emotionType != EmotionType.Neutral && request.Confidence > 0.7f)
                {
                    shouldRespond = true;
                    reason = "first_interaction";
                }
            }

            var emotionResult = new EmotionResult
            {
                Emotion = emotionType,
                Confidence = request.Confidence,
                OriginalText = $"Facial: {request.Emotion}; gaze={request.GazeState}; eye={request.EyeContactScore:F2}; motion={request.FaceMotionScore:F2}",
                Intensity = request.FaceMotionScore > 0.5f || request.Confidence > 0.85f ? "intense" : "moderate",
                UnderstoodAs = $"Camera read: {emotionType}" +
                    (request.EyeContactScore.HasValue ? $", eye contact {request.EyeContactScore:P0}" : "") +
                    (!string.IsNullOrEmpty(request.GazeState) ? $", {request.GazeState.Replace('_', ' ')}" : ""),
                LikelyCause = "body language / facial cues"
            };

            if (!shouldRespond && (
                (request.EyeContactScore.HasValue && request.EyeContactScore < 0.3f) ||
                (request.FaceMotionScore.HasValue && request.FaceMotionScore > 0.55f) ||
                request.GazeState is "eyes_closed_or_down"))
            {
                var gap = context?.LastInteraction.HasValue == true
                    ? (DateTime.UtcNow - context.LastInteraction.Value).TotalSeconds
                    : 999;
                if (gap > 12)
                {
                    shouldRespond = true;
                    reason = "engagement_cues";
                }
            }

            AdaptiveResponse? adaptiveResponse = null;
            List<IoTAction>? iotActions = null;

            if (shouldRespond)
            {
                _logger.LogInformation("Responding to facial emotion: {Emotion} (confidence: {Confidence:P2}, reason: {Reason})",
                    emotionType, request.Confidence, reason);

                var facialWellbeing = HttpContext.RequestServices.GetService<FacialWellbeingService>();
                if (facialWellbeing != null)
                {
                    adaptiveResponse = facialWellbeing.BuildReaction(userId, request, emotionType);
                    conversationMemory?.AddEntry(userId, emotionResult.OriginalText ?? "facial", emotionResult, adaptiveResponse);
                    var ctx = conversationMemory?.GetOrCreateContext(userId);
                    if (ctx != null)
                    {
                        ctx.LastEmotion = emotionType;
                        ctx.LastInteraction = DateTime.UtcNow;
                    }
                }
                else
                {
                    adaptiveResponse = await _decisionEngine.GenerateResponseAsync(emotionResult, userId,
                        $"I look {request.Emotion}. Eye contact feels {request.GazeState}. {request.CueNotes}");
                }

                iotActions = new List<IoTAction>();

                await EmotionHubUserScope.SendToUserAsync(_hubContext, userId, "EmotionDetected", emotionResult);
                await EmotionHubUserScope.SendToUserAsync(_hubContext, userId, "AdaptiveResponse", adaptiveResponse);
            }
            else
            {
                _logger.LogDebug("Facial emotion detected but not responding: {Emotion} (confidence: {Confidence:P2}) - too soon or neutral",
                    emotionType, request.Confidence);

                if (context != null)
                {
                    context.LastEmotion = emotionType;
                }
            }

            // No auto CollectData for facial without DataSharingConsent
            TryCollectLearningData(userId, $"I'm feeling {request.Emotion}", emotionResult);

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
            if (!UserIdSanitizer.TryNormalize(request.UserId, out var userId))
                return BadRequest(new { error = "Invalid userId (use letters, digits, _ or - only, max 64)" });

            var ethicalFramework = HttpContext.RequestServices.GetService<EthicalAIFrameworkService>();
            if (ethicalFramework != null && !ethicalFramework.HasConsent(userId, ConsentType.EmotionSensing))
            {
                return BadRequest(new { error = "Emotion sensing consent required. Please provide consent first." });
            }

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

            EmotionResult? textEmotionResult = null;
            if (!string.IsNullOrEmpty(request.Text))
            {
                textEmotionResult = _emotionDetectionService.DetectEmotion(request.Text);
                if (contextualData == null && contextualService != null)
                {
                    contextualData = contextualService.AnalyzeContext(userId: userId);
                }
            }

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

            var emotionResultForResponse = textEmotionResult ?? new EmotionResult
            {
                Emotion = fusedResult.PrimaryEmotion,
                Confidence = fusedResult.OverallConfidence,
                OriginalText = request.Text ?? "Multi-layer emotion detection"
            };

            var adaptiveResponse = await _decisionEngine.GenerateResponseAsync(emotionResultForResponse, userId, request.Text);

            List<IoTAction> iotActions;
            if (DecisionEngine.ShouldTriggerIoT(request.Text))
            {
                if (ethicalFramework != null && !ethicalFramework.HasConsent(userId, ConsentType.IoT))
                {
                    iotActions = new List<IoTAction>();
                    adaptiveResponse.Parameters["iotBlocked"] = "IoTConsent is off — enable it in privacy settings first.";
                    adaptiveResponse.Message =
                        "I can help with that — enable IoT / Quiet Mode in privacy settings first, then ask again.";
                }
                else
                {
                    var actionOrchestrator = HttpContext.RequestServices.GetService<AdvancedActionOrchestrator>();
                    iotActions = actionOrchestrator != null
                        ? await actionOrchestrator.OrchestrateActions(fusedResult, userId)
                        : await _decisionEngine.GetIoTActionsAsync(fusedResult.PrimaryEmotion);
                }
            }
            else
            {
                iotActions = new List<IoTAction>();
            }

            await EmotionHubUserScope.SendToUserAsync(_hubContext, userId, "EmotionDetected", emotionResultForResponse);
            await EmotionHubUserScope.SendToUserAsync(_hubContext, userId, "AdaptiveResponse", adaptiveResponse);
            await EmotionHubUserScope.SendToUserAsync(_hubContext, userId, "MultiLayerEmotion", fusedResult);

            foreach (var action in iotActions)
            {
                await EmotionHubUserScope.SendToUserAsync(_hubContext, userId, "IoTAction", action);
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

    /// <summary>
    /// V1: RealWorldDataCollector.CollectData must NOT run without DataSharingConsent (default OFF).
    /// </summary>
    private void TryCollectLearningData(string userId, string text, EmotionResult emotionResult)
    {
        if (emotionResult.Confidence < 0.7f)
            return;

        var ethical = HttpContext.RequestServices.GetService<EthicalAIFrameworkService>();
        if (ethical == null || !ethical.HasConsent(userId, ConsentType.DataSharing))
            return; // default OFF — no auto-collect

        var dataCollector = HttpContext.RequestServices.GetService<RealWorldDataCollector>();
        dataCollector?.CollectData(text, emotionResult.Emotion, emotionResult.Confidence);
    }

    /// <summary>
    /// Enforce IoTConsent before GetIoTActionsAsync / execution.
    /// If user asks for lights but consent OFF → no execute; ask-permission message.
    /// </summary>
    private async Task<List<IoTAction>> ResolveIoTActionsAsync(
        string userId,
        string text,
        EmotionResult emotionResult,
        AdaptiveResponse adaptiveResponse)
    {
        if (!DecisionEngine.ShouldTriggerIoT(text))
            return new List<IoTAction>();

        var ethical = HttpContext.RequestServices.GetService<EthicalAIFrameworkService>();
        var allowed = ethical != null && ethical.HasConsent(userId, ConsentType.IoT);
        if (!allowed)
        {
            adaptiveResponse.Parameters["iotBlocked"] = "IoTConsent is off — enable it in privacy settings first.";
            if (!adaptiveResponse.Parameters.ContainsKey("actionOffer"))
            {
                adaptiveResponse.Parameters["actionOffer"] =
                    "I can help with lights/music once you enable IoT consent in privacy settings.";
            }

            // Prefer a clear ask-permission companion message
            if (adaptiveResponse.Parameters.TryGetValue("intent", out var intentObj) &&
                intentObj?.ToString() == nameof(UserIntent.EnvironmentAction))
            {
                adaptiveResponse.Message =
                    "I can help with that — enable IoT / Quiet Mode in privacy settings first, then ask again.";
            }

            return new List<IoTAction>();
        }

        return await _decisionEngine.GetIoTActionsAsync(emotionResult.Emotion);
    }
}

public class EmotionCorrectionRequest
{
    public string? Text { get; set; }
    public string? CorrectEmotion { get; set; }
}
