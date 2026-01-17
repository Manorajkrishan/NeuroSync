using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;
using NeuroSync.Core.Models;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/collapse")]
public class CollapseController : ControllerBase
{
    private readonly CollapseRiskPredictorService _collapseService;
    private readonly ILogger<CollapseController> _logger;

    public CollapseController(
        CollapseRiskPredictorService collapseService,
        ILogger<CollapseController> logger)
    {
        _collapseService = collapseService;
        _logger = logger;
    }

    /// <summary>
    /// Calculate overall collapse risk assessment
    /// </summary>
    [HttpGet("risk")]
    public async Task<IActionResult> GetCollapseRisk([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var assessment = await _collapseService.CalculateCollapseRiskAsync(userId);
            return Ok(assessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating collapse risk");
            return StatusCode(500, new { error = "Failed to calculate collapse risk", details = ex.Message });
        }
    }

    /// <summary>
    /// Detect warning signs
    /// </summary>
    [HttpGet("warnings")]
    public async Task<IActionResult> GetWarningSigns([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var warnings = await _collapseService.DetectWarningSignsAsync(userId);
            return Ok(new { WarningSigns = warnings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting warning signs");
            return StatusCode(500, new { error = "Failed to detect warning signs", details = ex.Message });
        }
    }
}
