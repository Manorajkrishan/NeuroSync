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

    /// <summary>Get recent decisions (stored for dashboard and self-learning).</summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentDecisions([FromQuery] string? userId = null, [FromQuery] int limit = 10)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var list = await _decisionService.GetRecentDecisionsAsync(userId, Math.Min(limit, 50));
            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading recent decisions");
            return StatusCode(500, new { error = "Failed to load recent decisions", details = ex.Message });
        }
    }

    /// <summary>
    /// Frame and analyze a decision. Stores decision, creates LifeEvent, and nudges related LifeDomain (self-learn).
    /// </summary>
    [HttpPost("frame")]
    public async Task<IActionResult> FrameDecision([FromBody] FrameDecisionRequest request)
    {
        try
        {
            var userId = request.UserId ?? Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var decision = await _decisionService.FrameDecisionAsync(userId, request.DecisionText);

            // Self-learn: store as LifeEvent and nudge relevant LifeDomain
            var memory = HttpContext.RequestServices.GetService<LifeMemoryGraphService>();
            var domains = HttpContext.RequestServices.GetService<LifeDomainsEngineService>();
            var affected = MapDecisionTypeToDomain(decision.DecisionType);
            if (memory != null)
            {
                await memory.StoreLifeEventAsync(userId, LifeEventType.Decision,
                    "Decision: " + (decision.DecisionText.Length > 200 ? decision.DecisionText.Substring(0, 200) + "..." : decision.DecisionText),
                    emotionalSignificance: 60, lifeImpact: LifeImpactLevel.Medium, affectedDomain: affected,
                    tags: new List<string> { "decision", decision.DecisionType.ToString().ToLower() });
            }
            if (domains != null && affected.HasValue)
            {
                await domains.NudgeDomainStressAsync(userId, affected.Value, 5);
            }

            return Ok(decision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error framing decision");
            return StatusCode(500, new { error = "Failed to frame decision", details = ex.Message });
        }
    }

    private static LifeDomainType? MapDecisionTypeToDomain(DecisionType t)
    {
        return t switch
        {
            DecisionType.Career => LifeDomainType.CareerWork,
            DecisionType.Relationship => LifeDomainType.Relationships,
            DecisionType.Financial => LifeDomainType.MoneySurvival,
            DecisionType.Life => LifeDomainType.SelfGrowth,
            DecisionType.Crisis => LifeDomainType.MentalHealth,
            _ => null
        };
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
