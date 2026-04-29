using Microsoft.Extensions.Hosting;

namespace NeuroSync.Api.Services;

/// <summary>
/// Flushes real-world collected data to file when the application stops.
/// </summary>
public class FlushRealWorldDataOnShutdown : IHostedService
{
    private readonly RealWorldDataCollector _collector;

    public FlushRealWorldDataOnShutdown(RealWorldDataCollector collector)
    {
        _collector = collector;
    }

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StopAsync(CancellationToken ct)
    {
        _collector.FlushToFile();
        return Task.CompletedTask;
    }
}
