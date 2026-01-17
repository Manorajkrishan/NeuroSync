using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/growth")]
public class GrowthController : ControllerBase
{
    private readonly EmotionalGrowthAnalyticsService _growthService;
    private readonly ILogger<GrowthController> _logger;

    public GrowthController(
        EmotionalGrowthAnalyticsService growthService,
        ILogger<GrowthController> logger)
    {
        _growthService = growthService;
        _logger = logger;
    }

    /// <summary>
    /// Calculate maturity score
    /// </summary>
    [HttpGet("maturity")]
    public async Task<IActionResult> GetMaturityScore([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var metrics = await _growthService.CalculateMaturityScoreAsync(userId);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating maturity score");
            return StatusCode(500, new { error = "Failed to calculate maturity score", details = ex.Message });
        }
    }

    /// <summary>
    /// Calculate resilience score
    /// </summary>
    [HttpGet("resilience")]
    public async Task<IActionResult> GetResilienceScore([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var resilience = await _growthService.CalculateResilienceScoreAsync(userId);
            return Ok(resilience);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating resilience score");
            return StatusCode(500, new { error = "Failed to calculate resilience score", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate growth report
    /// </summary>
    [HttpGet("report")]
    public async Task<IActionResult> GetGrowthReport([FromQuery] string? userId = null, [FromQuery] int months = 6)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var report = await _growthService.GenerateGrowthReportAsync(userId, months);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating growth report");
            return StatusCode(500, new { error = "Failed to generate growth report", details = ex.Message });
        }
    }
}
