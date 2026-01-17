using Microsoft.Extensions.Diagnostics.HealthChecks;
using NeuroSync.Api.Services;

namespace NeuroSync.Api.HealthChecks;

/// <summary>
/// Health check for ML model availability.
/// </summary>
public class ModelHealthCheck : IHealthCheck
{
    private readonly ModelService _modelService;
    private readonly ILogger<ModelHealthCheck> _logger;

    public ModelHealthCheck(ModelService modelService, ILogger<ModelHealthCheck> logger)
    {
        _modelService = modelService;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var model = _modelService.LoadOrCreateModel();
            if (model != null)
            {
                return Task.FromResult(HealthCheckResult.Healthy("ML model is loaded and ready"));
            }
            
            return Task.FromResult(HealthCheckResult.Degraded("ML model is not available"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Model health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("ML model failed to load", ex));
        }
    }
}
