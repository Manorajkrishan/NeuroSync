namespace NeuroSync.IoT.Interfaces;

/// <summary>
/// Interface for music streaming services.
/// </summary>
public interface IMusicService
{
    /// <summary>
    /// Gets the name of the music service.
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Checks if the service is available and configured.
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Plays music based on genre/playlist.
    /// </summary>
    Task<bool> PlayMusicAsync(string deviceId, string genre, string playlist, int volume);

    /// <summary>
    /// Stops music playback.
    /// </summary>
    Task<bool> StopMusicAsync(string deviceId);

    /// <summary>
    /// Sets the volume.
    /// </summary>
    Task<bool> SetVolumeAsync(string deviceId, int volume);

    /// <summary>
    /// Gets available playlists for a genre.
    /// </summary>
    Task<List<string>> GetPlaylistsByGenreAsync(string genre);
}

