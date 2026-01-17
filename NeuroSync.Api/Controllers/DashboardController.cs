using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;
using NeuroSync.Core.Models;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly EmotionalOSDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        EmotionalOSDashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get daily emotional summary (Human OS Dashboard)
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetDailySummary([FromQuery] string? userId = null, [FromQuery] DateTime? date = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var summary = await _dashboardService.GetDailyEmotionalSummaryAsync(userId, date);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily emotional summary");
            return StatusCode(500, new { error = "Failed to get emotional summary", details = ex.Message });
        }
    }

    /// <summary>
    /// Get burnout risk score
    /// </summary>
    [HttpGet("burnout-risk")]
    public async Task<IActionResult> GetBurnoutRisk([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var risk = await _dashboardService.GetBurnoutRiskScoreAsync(userId);
            return Ok(risk);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating burnout risk");
            return StatusCode(500, new { error = "Failed to calculate burnout risk", details = ex.Message });
        }
    }

    /// <summary>
    /// Get emotional growth metrics
    /// </summary>
    [HttpGet("growth")]
    public async Task<IActionResult> GetGrowthMetrics([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var metrics = await _dashboardService.GetEmotionalGrowthScoreAsync(userId);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting growth metrics");
            return StatusCode(500, new { error = "Failed to get growth metrics", details = ex.Message });
        }
    }

    /// <summary>
    /// Get mental load analysis
    /// </summary>
    [HttpGet("mental-load")]
    public async Task<IActionResult> GetMentalLoad([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var analysis = await _dashboardService.GetMentalLoadAnalysisAsync(userId);
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing mental load");
            return StatusCode(500, new { error = "Failed to analyze mental load", details = ex.Message });
        }
    }
}
