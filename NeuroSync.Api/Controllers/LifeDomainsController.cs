using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;
using NeuroSync.Core.Models;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/domains")]
public class LifeDomainsController : ControllerBase
{
    private readonly LifeDomainsEngineService _domainsService;
    private readonly ILogger<LifeDomainsController> _logger;

    public LifeDomainsController(
        LifeDomainsEngineService domainsService,
        ILogger<LifeDomainsController> logger)
    {
        _domainsService = domainsService;
        _logger = logger;
    }

    /// <summary>
    /// Get state of a specific life domain
    /// </summary>
    [HttpGet("state/{domain}")]
    public async Task<IActionResult> GetDomainState(
        [FromRoute] LifeDomainType domain,
        [FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var domainState = await _domainsService.GetDomainStateAsync(userId, domain);
            return Ok(domainState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting domain state for {Domain}", domain);
            return StatusCode(500, new { error = $"Failed to get {domain} domain state", details = ex.Message });
        }
    }

    /// <summary>
    /// Get health report for all life domains
    /// </summary>
    [HttpGet("health-report")]
    public async Task<IActionResult> GetDomainHealthReport([FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var report = await _domainsService.GetDomainHealthReportAsync(userId);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting domain health report");
            return StatusCode(500, new { error = "Failed to get domain health report", details = ex.Message });
        }
    }

    /// <summary>
    /// Analyze stress in a specific domain
    /// </summary>
    [HttpGet("stress/{domain}")]
    public async Task<IActionResult> AnalyzeDomainStress(
        [FromRoute] LifeDomainType domain,
        [FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var analysis = await _domainsService.AnalyzeDomainStressAsync(userId, domain);
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing stress in {Domain}", domain);
            return StatusCode(500, new { error = $"Failed to analyze {domain} domain stress", details = ex.Message });
        }
    }

    /// <summary>
    /// Get recommended actions for a domain
    /// </summary>
    [HttpGet("actions/{domain}")]
    public async Task<IActionResult> GetDomainActions(
        [FromRoute] LifeDomainType domain,
        [FromQuery] string? userId = null)
    {
        try
        {
            userId ??= Request.Headers["X-User-Id"].FirstOrDefault() ?? "default";
            var actions = await _domainsService.GetDomainActionsAsync(userId, domain);
            return Ok(actions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting actions for {Domain}", domain);
            return StatusCode(500, new { error = $"Failed to get {domain} domain actions", details = ex.Message });
        }
    }
}
