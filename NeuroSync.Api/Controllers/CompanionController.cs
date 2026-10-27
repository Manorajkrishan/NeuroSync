using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using NeuroSync.Api.Hubs;
using NeuroSync.Api.Services;
using NeuroSync.Core;

namespace NeuroSync.Api.Controllers;

/// <summary>
/// Thin V1 companion surface — natural message is the product; emotion stays in developerInsights.
/// </summary>
[ApiController]
[EnableRateLimiting("companion")]
[Route("api/companion")]
public class CompanionController : ControllerBase
{
    private readonly EmotionDetectionService _emotionDetection;
    private readonly DecisionEngine _decisionEngine;
    private readonly IHubContext<EmotionHub> _hubContext;
    private readonly EthicalAIFrameworkService _consent;
    private readonly ILogger<CompanionController> _logger;
    private readonly LiveBetaMetrics _metrics;
    private readonly IEphemeralSessionContext _sessions;
    private readonly CompanionFeedbackService _feedback;

    public CompanionController(
        EmotionDetectionService emotionDetection,
        DecisionEngine decisionEngine,
        IHubContext<EmotionHub> hubContext,
        EthicalAIFrameworkService consent,
        ILogger<CompanionController> logger,
        LiveBetaMetrics metrics,
        IEphemeralSessionContext sessions,
        CompanionFeedbackService feedback)
    {
        _emotionDetection = emotionDetection;
        _decisionEngine = decisionEngine;
        _hubContext = hubContext;
        _consent = consent;
        _logger = logger;
        _metrics = metrics;
        _sessions = sessions;
        _feedback = feedback;
    }

    [HttpPost("message")]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> Message([FromBody] CompanionMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Text))
            return BadRequest(new { error = "Text is required" });
        if (request.Text.Length > 4000)
            return BadRequest(new { error = "Message is too long." });
        var started = Stopwatch.StartNew();

        if (!UserIdSanitizer.TryNormalize(request.UserId, out var userId))
            return BadRequest(new { error = "Invalid userId (use letters, digits, _ or - only, max 64)" });

        try
        {
            var sessionId = ResolveSessionId(request.SessionId);
            var emotionResult = _emotionDetection.DetectEmotion(request.Text);
            var adaptive = await _decisionEngine.GenerateResponseAsync(
                emotionResult, userId, request.Text, sessionId);

            // Learning / real-world collection: OFF unless DataSharingConsent
            TryCollectLearningData(userId, request.Text, emotionResult);

            // IoT: enforce consent before any execute
            var iotActions = new List<IoTAction>();
            if (DecisionEngine.ShouldTriggerIoT(request.Text))
            {
                if (_consent.HasConsent(userId, ConsentType.IoT))
                {
                    iotActions = await _decisionEngine.GetIoTActionsAsync(emotionResult.Emotion);
                }
                else
                {
                    adaptive.Parameters["iotBlocked"] = "IoTConsent is off — enable it in privacy settings first.";
                    if (!adaptive.Parameters.ContainsKey("actionOffer"))
                        adaptive.Parameters["actionOffer"] =
                            "I can help with lights/music once you enable IoT consent in privacy settings.";
                    if (string.IsNullOrWhiteSpace(adaptive.Message) ||
                        adaptive.Parameters["intent"]?.ToString() == nameof(UserIntent.EnvironmentAction))
                    {
                        adaptive.Message =
                            "I can help with that — enable IoT / Quiet Mode in privacy settings first, then ask again.";
                    }
                }
            }

            await EmotionHubUserScope.SendToUserAsync(_hubContext, userId, "AdaptiveResponse", adaptive);
            foreach (var action in iotActions)
                await EmotionHubUserScope.SendToUserAsync(_hubContext, userId, "IoTAction", action);

            var p = adaptive.Parameters;
            object? insights = p.TryGetValue("developerInsights", out var di) ? di : null;
            object? actionOffer = p.TryGetValue("actionOffer", out var ao) ? ao : null;
            if (actionOffer == null && p.TryGetValue("iotBlocked", out var blocked))
                actionOffer = blocked;

            _metrics.CompanionTurn(started.ElapsedMilliseconds);
            return Ok(new
            {
                message = adaptive.Message,
                sessionId,
                responseId = p.TryGetValue("responseId", out var responseId) ? responseId : null,
                mode = p.TryGetValue("interactionMode", out var mode) ? mode : null,
                uncertainty = p.TryGetValue("uncertainty", out var unc) ? unc : emotionResult.Uncertainty.ToString(),
                actionOffer,
                developerInsights = insights,
                iotExecuted = iotActions.Count > 0,
                iotBlocked = p.ContainsKey("iotBlocked")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Companion message failed");
            return StatusCode(500, new { error = "An error occurred while processing the request" });
        }
    }

    [HttpPost("feedback")]
    [RequestSizeLimit(8 * 1024)]
    public async Task<IActionResult> Feedback(
        [FromBody] CompanionFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.Rating is not -1 and not 1)
            return BadRequest(new { error = "Rating must be -1 or 1." });

        var authenticatedUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var requestedUser = request.UserId;
        var candidateUser = !string.IsNullOrWhiteSpace(authenticatedUser) ? authenticatedUser : requestedUser;
        if (!UserIdSanitizer.TryNormalize(candidateUser, out var userId))
            return BadRequest(new { error = "Invalid userId." });

        if (!string.IsNullOrWhiteSpace(authenticatedUser)
            && !string.IsNullOrWhiteSpace(requestedUser)
            && !string.Equals(authenticatedUser, requestedUser, StringComparison.Ordinal))
            return Forbid();

        if (!UserIdSanitizer.TryNormalizeSessionId(request.SessionId, out var sessionId))
            return BadRequest(new { error = "Valid sessionId is required." });

        if (!Guid.TryParseExact(request.ResponseId, "N", out _))
            return BadRequest(new { error = "Valid responseId is required." });

        var metadata = _sessions.GetResponseMetadata(userId, sessionId, request.ResponseId);
        if (metadata == null)
            return NotFound(new { error = "Response feedback window expired or response was not found." });

        var shareForLearning = _consent.HasConsent(userId, ConsentType.DataSharing);
        var saved = await _feedback.SaveAsync(
            userId,
            metadata,
            request.Rating,
            request.ReasonCode,
            shareForLearning,
            cancellationToken).ConfigureAwait(false);

        return Ok(new
        {
            responseId = saved.ResponseId,
            rating = saved.Rating,
            shareForLearning = saved.ShareForLearning,
            message = shareForLearning
                ? "Feedback saved and eligible for consented quality learning."
                : "Feedback saved. Learning-data sharing is off, so it will not be used for training."
        });
    }

    private string ResolveSessionId(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested)
            && UserIdSanitizer.TryNormalizeSessionId(requested, out var normalized))
        {
            return normalized;
        }

        return EphemeralSessionContextService.GenerateSessionId();
    }

    private void TryCollectLearningData(string userId, string text, EmotionResult emotionResult)
    {
        // V1: auto-collect disabled unless explicit DataSharingConsent (learning consent).
        if (!_consent.HasConsent(userId, ConsentType.DataSharing))
            return;
        if (emotionResult.Confidence < 0.7f)
            return;

        var dataCollector = HttpContext.RequestServices.GetService<RealWorldDataCollector>();
        dataCollector?.CollectData(text, emotionResult.Emotion, emotionResult.Confidence);
    }
}

public class CompanionFeedbackRequest
{
    public string? UserId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string ResponseId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? ReasonCode { get; set; }
}

public class CompanionMessageRequest
{
    public string Text { get; set; } = string.Empty;
    public string? UserId { get; set; }

    /// <summary>Optional client session id (tab-scoped). Server issues one when missing.</summary>
    public string? SessionId { get; set; }
}
