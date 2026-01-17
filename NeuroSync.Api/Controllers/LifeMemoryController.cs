using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;
using NeuroSync.Core.Models;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/memory")]
public class LifeMemoryController : ControllerBase
{
    private readonly LifeMemoryGraphService _memoryService;
    private readonly ILogger<LifeMemoryController> _logger;

    public LifeMemoryController(
        LifeMemoryGraphService memoryService,
        ILogger<LifeMemoryController> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }

    /// <summary>
    /// Store a life event
    /// </summary>
    [HttpPost("event")]
    public async Task<IActionResult> StoreLifeEvent([FromBody] StoreLifeEventRequest request)
    {
        try
        {
            var userId = request.UserId ?? Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var lifeEvent = await _memoryService.StoreLifeEventAsync(
                userId,
                request.EventType,
                request.Description,
                request.EmotionalSignificance,
                request.LifeImpact,
                request.AffectedDomain,
                request.Tags);
            return Ok(lifeEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing life event");
            return StatusCode(500, new { error = "Failed to store life event", details = ex.Message });
        }
    }

    /// <summary>
    /// Build emotional narrative
    /// </summary>
    [HttpGet("narrative")]
    public async Task<IActionResult> BuildNarrative([FromQuery] string? userId = null, [FromQuery] int months = 6)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var narrative = await _memoryService.BuildEmotionalNarrativeAsync(userId, months);
            return Ok(narrative);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building narrative");
            return StatusCode(500, new { error = "Failed to build narrative", details = ex.Message });
        }
    }

    /// <summary>
    /// Identify growth milestones
    /// </summary>
    [HttpGet("milestones")]
    public async Task<IActionResult> GetGrowthMilestones([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var milestones = await _memoryService.IdentifyGrowthMilestonesAsync(userId);
            return Ok(new { Milestones = milestones });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error identifying milestones");
            return StatusCode(500, new { error = "Failed to identify milestones", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate life story
    /// </summary>
    [HttpGet("story")]
    public async Task<IActionResult> GenerateLifeStory([FromQuery] string? userId = null, [FromQuery] int months = 12)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var story = await _memoryService.GenerateLifeStoryAsync(userId, months);
            return Ok(new { Story = story });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating life story");
            return StatusCode(500, new { error = "Failed to generate life story", details = ex.Message });
        }
    }
}

// Request DTOs
public class StoreLifeEventRequest
{
    public string? UserId { get; set; }
    public LifeEventType EventType { get; set; }
    public string Description { get; set; } = string.Empty;
    public double EmotionalSignificance { get; set; } = 50;
    public LifeImpactLevel LifeImpact { get; set; } = LifeImpactLevel.Medium;
    public LifeDomainType? AffectedDomain { get; set; }
    public List<string>? Tags { get; set; }
}
