using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/identity")]
public class IdentityController : ControllerBase
{
    private readonly IdentityPurposeEngineService _identityService;
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(
        IdentityPurposeEngineService identityService,
        ILogger<IdentityController> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    /// <summary>
    /// Get identity profile
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetIdentityProfile([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var profile = await _identityService.ExtractIdentityAsync(userId);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting identity profile");
            return StatusCode(500, new { error = "Failed to get identity profile", details = ex.Message });
        }
    }

    /// <summary>
    /// Get purpose profile
    /// </summary>
    [HttpGet("purpose")]
    public async Task<IActionResult> GetPurposeProfile([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var purpose = await _identityService.MapPurposeAsync(userId);
            return Ok(purpose);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purpose profile");
            return StatusCode(500, new { error = "Failed to get purpose profile", details = ex.Message });
        }
    }

    /// <summary>
    /// Analyze life direction
    /// </summary>
    [HttpGet("direction")]
    public async Task<IActionResult> AnalyzeLifeDirection([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var direction = await _identityService.AnalyzeLifeDirectionAsync(userId);
            return Ok(direction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing life direction");
            return StatusCode(500, new { error = "Failed to analyze life direction", details = ex.Message });
        }
    }

    /// <summary>
    /// Track identity evolution
    /// </summary>
    [HttpGet("evolution")]
    public async Task<IActionResult> TrackIdentityEvolution([FromQuery] string? userId = null, [FromQuery] int months = 12)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var evolution = await _identityService.TrackIdentityEvolutionAsync(userId, months);
            return Ok(evolution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking identity evolution");
            return StatusCode(500, new { error = "Failed to track identity evolution", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate purpose insights
    /// </summary>
    [HttpGet("insights")]
    public async Task<IActionResult> GeneratePurposeInsights([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var insights = await _identityService.GeneratePurposeInsightsAsync(userId);
            return Ok(new { Insights = insights });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating purpose insights");
            return StatusCode(500, new { error = "Failed to generate insights", details = ex.Message });
        }
    }
}
