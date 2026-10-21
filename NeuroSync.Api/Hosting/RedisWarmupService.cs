using StackExchange.Redis;

namespace NeuroSync.Api.Hosting;

/// <summary>
/// Verifies an explicitly configured Redis/Valkey dependency during startup.
/// Production must not silently switch to a broken shared-session store.
/// </summary>
public sealed class RedisWarmupService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RedisWarmupService> _logger;

    public RedisWarmupService(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<RedisWarmupService> logger)
    {
        _services = services;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_configuration["Redis:ConnectionString"]))
            return;

        var redis = _services.GetService<IConnectionMultiplexer>()
            ?? throw new InvalidOperationException("Redis is configured but IConnectionMultiplexer is not registered.");

        Exception? lastError = null;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var latency = await redis.GetDatabase().PingAsync().ConfigureAwait(false);
                _logger.LogInformation(
                    "Redis startup verification succeeded in {LatencyMs} ms on attempt {Attempt}.",
                    latency.TotalMilliseconds,
                    attempt);
                return;
            }
            catch (Exception ex) when (attempt < 5)
            {
                lastError = ex;
                _logger.LogWarning(
                    "Redis startup verification attempt {Attempt} failed; retrying.",
                    attempt);
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException(
            "Redis is configured but could not be reached after startup retries.",
            lastError);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
