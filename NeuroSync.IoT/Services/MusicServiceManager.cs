using NeuroSync.IoT.Interfaces;

namespace NeuroSync.IoT.Services;

/// <summary>
/// Manages multiple music services and selects the best available one.
/// </summary>
public class MusicServiceManager
{
    private readonly List<IMusicService> _services;
    private readonly Action<string>? _logger;

    public MusicServiceManager(IEnumerable<IMusicService> services, Action<string>? logger = null)
    {
        _services = services.ToList();
        _logger = logger;
    }

    /// <summary>
    /// Gets the best available music service.
    /// Priority: Spotify > YouTube Music > First available
    /// </summary>
    public async Task<IMusicService?> GetBestAvailableServiceAsync()
    {
        foreach (var service in _services.OrderBy(s => s.ServiceName == "Spotify" ? 0 : s.ServiceName == "YouTube Music" ? 1 : 2))
        {
            if (await service.IsAvailableAsync())
            {
                _logger?.Invoke($"MusicServiceManager: Selected {service.ServiceName}");
                return service;
            }
        }

        _logger?.Invoke("MusicServiceManager: No music services available");
        return null;
    }

    /// <summary>
    /// Gets a specific service by name.
    /// </summary>
    public IMusicService? GetService(string serviceName)
    {
        return _services.FirstOrDefault(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all available services.
    /// </summary>
    public async Task<List<IMusicService>> GetAvailableServicesAsync()
    {
        var available = new List<IMusicService>();
        foreach (var service in _services)
        {
            if (await service.IsAvailableAsync())
            {
                available.Add(service);
            }
        }
        return available;
    }
}

