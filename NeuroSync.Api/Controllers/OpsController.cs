using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeuroSync.Api.Auth;
using NeuroSync.Api.Services;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/ops")]
public class OpsController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly LiveBetaMetrics _metrics;

    public OpsController(IConfiguration config, LiveBetaMetrics metrics)
    {
        _config = config;
        _metrics = metrics;
    }

    /// <summary>Scrape counters with X-Metrics-Key. Returns 404 when the key is not configured.</summary>
    [HttpGet("metrics")]
    public IActionResult Metrics()
    {
        var expected = _config["Ops:MetricsKey"];
        if (string.IsNullOrWhiteSpace(expected))
            return NotFound();

        var provided = Request.Headers["X-Metrics-Key"].FirstOrDefault() ?? "";
        if (!AuthPolicy.FixedTimeEquals(provided, expected))
            return Unauthorized(new { error = "Metrics key required." });

        return Ok(_metrics.Snapshot());
    }
}
