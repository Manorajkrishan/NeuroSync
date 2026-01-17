using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;
using NeuroSync.Core.Models;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/decisions")]
public class DecisionsController : ControllerBase
{
    private readonly DecisionIntelligenceEngineService _decisionService;
    private readonly ILogger<DecisionsController> _logger;

    public DecisionsController(
        DecisionIntelligenceEngineService decisionService,
        ILogger<DecisionsController> logger)
    {
        _decisionService = decisionService;
        _logger = logger;
    }

    /// <summary>
    /// Frame and analyze a decision
    /// </summary>
    [HttpPost("frame")]
    public async Task<IActionResult> FrameDecision([FromBody] FrameDecisionRequest request)
    {
        try
        {
            var userId = request.UserId ?? Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var decision = await _decisionService.FrameDecisionAsync(userId, request.DecisionText);
            return Ok(decision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error framing decision");
            return StatusCode(500, new { error = "Failed to frame decision", details = ex.Message });
        }
    }

    /// <summary>
    /// Analyze decision options
    /// </summary>
    [HttpPost("{decisionId}/analyze")]
    public async Task<IActionResult> AnalyzeOptions(
        [FromRoute] int decisionId,
        [FromBody] AnalyzeOptionsRequest request)
    {
        try
        {
            var userId = request.UserId ?? Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var analysis = await _decisionService.AnalyzeDecisionOptionsAsync(userId, decisionId, request.Options);
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing decision options");
            return StatusCode(500, new { error = "Failed to analyze options", details = ex.Message });
        }
    }

    /// <summary>
    /// Predict emotional outcome for a decision option
    /// </summary>
    [HttpPost("{decisionId}/options/{optionId}/predict")]
    public async Task<IActionResult> PredictOutcome(
        [FromRoute] int decisionId,
        [FromRoute] int optionId,
        [FromBody] PredictOutcomeRequest request)
    {
        try
        {
            var userId = request.UserId ?? Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var prediction = await _decisionService.PredictEmotionalOutcomeAsync(
                userId, decisionId, optionId, request.Timeframe);
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting emotional outcome");
            return StatusCode(500, new { error = "Failed to predict outcome", details = ex.Message });
        }
    }

    /// <summary>
    /// Model decision scenarios (best/worst/most likely)
    /// </summary>
    [HttpPost("{decisionId}/scenarios")]
    public async Task<IActionResult> ModelScenarios(
        [FromRoute] int decisionId,
        [FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var scenarios = await _decisionService.ModelDecisionScenariosAsync(userId, decisionId);
            return Ok(scenarios);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error modeling decision scenarios");
            return StatusCode(500, new { error = "Failed to model scenarios", details = ex.Message });
        }
    }
}

// Request DTOs
public class FrameDecisionRequest
{
    public string? UserId { get; set; }
    public string DecisionText { get; set; } = string.Empty;
}

public class AnalyzeOptionsRequest
{
    public string? UserId { get; set; }
    public List<string> Options { get; set; } = new();
}

public class PredictOutcomeRequest
{
    public string? UserId { get; set; }
    public Timeframe Timeframe { get; set; }
}
