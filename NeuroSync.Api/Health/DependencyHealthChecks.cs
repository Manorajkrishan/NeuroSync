using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NeuroSync.Api.Data;
using StackExchange.Redis;

namespace NeuroSync.Api.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _config;

    public DatabaseHealthCheck(IServiceScopeFactory scopes, IConfiguration config)
    {
        _scopes = scopes;
        _config = config;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeuroSyncDbContext>();
        var provider = _config["Database:Provider"] ?? "InMemory";
        if (provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase) || !db.Database.IsRelational())
            return HealthCheckResult.Healthy("In-memory database (development or tests).");

        var connected = await db.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
        return connected
            ? HealthCheckResult.Healthy(provider + " is reachable.")
            : HealthCheckResult.Unhealthy(provider + " is not reachable.");
    }
}

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly IConfiguration _config;

    public RedisHealthCheck(IConfiguration config, IServiceProvider services)
    {
        _config = config;
        _redis = services.GetService<IConnectionMultiplexer>();
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_config["Redis:ConnectionString"]))
            return Task.FromResult(HealthCheckResult.Healthy("Redis is not configured. Single-instance session cache is in use."));
        if (_redis == null || !_redis.IsConnected)
            return Task.FromResult(HealthCheckResult.Unhealthy("Redis is configured but not connected."));
        return Task.FromResult(HealthCheckResult.Healthy("Redis is connected."));
    }
}
