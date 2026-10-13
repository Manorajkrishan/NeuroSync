using NeuroSync.Api.Services;

namespace NeuroSync.Api.Hosting;

/// <summary>Deletes stored conversation and memory rows older than the retention window.</summary>
public sealed class RetentionSweepService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _config;
    private readonly ILogger<RetentionSweepService> _logger;

    public RetentionSweepService(IServiceScopeFactory scopes, IConfiguration config, ILogger<RetentionSweepService> logger)
    {
        _scopes = scopes;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.GetValue("Database:RetentionSweep", true))
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken).ConfigureAwait(false);
                using var scope = _scopes.CreateScope();
                var accounts = scope.ServiceProvider.GetRequiredService<IAccountDataService>();
                await accounts.SweepExpiredAsync(stoppingToken).ConfigureAwait(false);
                _logger.LogInformation("Retention sweep finished");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retention sweep failed");
            }
        }
    }
}
