using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/trust-safety")]
public class TrustSafetyController : ControllerBase
{
    private readonly TrustSafetyLayerService _safetyService;
    private readonly ILogger<TrustSafetyController> _logger;

    public TrustSafetyController(
        TrustSafetyLayerService safetyService,
        ILogger<TrustSafetyController> logger)
    {
        _safetyService = safetyService;
        _logger = logger;
    }

    /// <summary>
    /// Detect emotional dependency on AI
    /// </summary>
    [HttpGet("dependency")]
    public async Task<IActionResult> DetectDependency([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var assessment = await _safetyService.DetectEmotionalDependencyAsync(userId);
            return Ok(assessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting dependency");
            return StatusCode(500, new { error = "Failed to detect dependency", details = ex.Message });
        }
    }

    /// <summary>
    /// Monitor AI attachment patterns
    /// </summary>
    [HttpGet("attachment")]
    public async Task<IActionResult> MonitorAttachment([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var analysis = await _safetyService.MonitorAIAttachmentAsync(userId);
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring attachment");
            return StatusCode(500, new { error = "Failed to monitor attachment", details = ex.Message });
        }
    }

    /// <summary>
    /// Get safe detachment plan
    /// </summary>
    [HttpGet("detachment-plan")]
    public async Task<IActionResult> GetDetachmentPlan([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var plan = await _safetyService.SupportSafeDetachmentAsync(userId);
            return Ok(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating detachment plan");
            return StatusCode(500, new { error = "Failed to generate detachment plan", details = ex.Message });
        }
    }

    /// <summary>
    /// Get human referrals
    /// </summary>
    [HttpGet("referrals")]
    public async Task<IActionResult> GetHumanReferrals(
        [FromQuery] string? userId = null,
        [FromQuery] ReferralUrgency urgency = ReferralUrgency.Medium)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var referrals = await _safetyService.ManageHumanReferralsAsync(userId, urgency);
            return Ok(new { Referrals = referrals });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting referrals");
            return StatusCode(500, new { error = "Failed to get referrals", details = ex.Message });
        }
    }

    /// <summary>
    /// Get ethical boundaries
    /// </summary>
    [HttpGet("boundaries")]
    public async Task<IActionResult> GetEthicalBoundaries()
    {
        try
        {
            var boundaries = await _safetyService.EnsureEthicalBoundariesAsync();
            return Ok(boundaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ethical boundaries");
            return StatusCode(500, new { error = "Failed to get ethical boundaries", details = ex.Message });
        }
    }
}
