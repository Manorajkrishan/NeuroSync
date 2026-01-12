using NeuroSync.IoT.Interfaces;
using System.Text.Json;

namespace NeuroSync.IoT.Services;

/// <summary>
/// Spotify music service integration.
/// Requires Spotify Web API credentials.
/// </summary>
public class SpotifyMusicService : IMusicService
{
    private readonly HttpClient _httpClient;
    private readonly string? _accessToken;
    private readonly string? _refreshToken;
    private readonly string? _clientId;
    private readonly string? _clientSecret;
    private readonly Action<string>? _logger;

    public string ServiceName => "Spotify";

    public SpotifyMusicService(
        HttpClient httpClient,
        string? accessToken = null,
        string? refreshToken = null,
        string? clientId = null,
        string? clientSecret = null,
        Action<string>? logger = null)
    {
        _httpClient = httpClient;
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _logger = logger;
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (string.IsNullOrEmpty(_accessToken) && string.IsNullOrEmpty(_clientId))
        {
            _logger?.Invoke("Spotify: Not configured (missing credentials)");
            return false;
        }

        // Test connection
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            if (!string.IsNullOrEmpty(_accessToken))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            }

            var response = await _httpClient.GetAsync("https://api.spotify.com/v1/me");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"Spotify: Connection test failed - {ex.Message}");
            return false;
        }
    }

    public async Task<bool> PlayMusicAsync(string deviceId, string genre, string playlist, int volume)
    {
        try
        {
            _logger?.Invoke($"Spotify: Playing {playlist} (genre: {genre}) on {deviceId} at {volume}%");

            // Step 1: Search for playlist or create a genre-based search
            var searchQuery = !string.IsNullOrEmpty(playlist) ? playlist : genre;
            var tracks = await SearchTracksAsync(searchQuery, genre);

            if (tracks == null || tracks.Count == 0)
            {
                _logger?.Invoke("Spotify: No tracks found");
                return false;
            }

            // Step 2: Set volume
            await SetVolumeAsync(deviceId, volume);

            // Step 3: Start playback
            var playRequest = new
            {
                uris = tracks.Take(50).Select(t => t.Uri).ToArray(),
                position_ms = 0
            };

            var json = JsonSerializer.Serialize(playRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");

            var endpoint = string.IsNullOrEmpty(deviceId)
                ? "https://api.spotify.com/v1/me/player/play"
                : $"https://api.spotify.com/v1/me/player/play?device_id={deviceId}";

            var response = await _httpClient.PutAsync(endpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger?.Invoke($"Spotify: Play failed - {error}");
                return false;
            }

            _logger?.Invoke("Spotify: Music playback started");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"Spotify: Error playing music - {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StopMusicAsync(string deviceId)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");

            var endpoint = string.IsNullOrEmpty(deviceId)
                ? "https://api.spotify.com/v1/me/player/pause"
                : $"https://api.spotify.com/v1/me/player/pause?device_id={deviceId}";

            var response = await _httpClient.PutAsync(endpoint, null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"Spotify: Error stopping music - {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SetVolumeAsync(string deviceId, int volume)
    {
        try
        {
            volume = Math.Clamp(volume, 0, 100);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");

            var endpoint = string.IsNullOrEmpty(deviceId)
                ? $"https://api.spotify.com/v1/me/player/volume?volume_percent={volume}"
                : $"https://api.spotify.com/v1/me/player/volume?volume_percent={volume}&device_id={deviceId}";

            var response = await _httpClient.PutAsync(endpoint, null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"Spotify: Error setting volume - {ex.Message}");
            return false;
        }
    }

    public async Task<List<string>> GetPlaylistsByGenreAsync(string genre)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");

            // Search for playlists by genre
            var query = Uri.EscapeDataString(genre);
            var response = await _httpClient.GetAsync($"https://api.spotify.com/v1/search?q={query}&type=playlist&limit=20");

            if (!response.IsSuccessStatusCode)
                return new List<string>();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);

            var playlists = new List<string>();
            if (result.TryGetProperty("playlists", out var playlistsObj) &&
                playlistsObj.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out var name))
                    {
                        playlists.Add(name.GetString() ?? "");
                    }
                }
            }

            return playlists;
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"Spotify: Error getting playlists - {ex.Message}");
            return new List<string>();
        }
    }

    private async Task<List<SpotifyTrack>?> SearchTracksAsync(string query, string genre)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");

            // Try playlist first
            var playlistQuery = Uri.EscapeDataString(query);
            var playlistResponse = await _httpClient.GetAsync($"https://api.spotify.com/v1/search?q={playlistQuery}&type=playlist&limit=1");

            if (playlistResponse.IsSuccessStatusCode)
            {
                var playlistJson = await playlistResponse.Content.ReadAsStringAsync();
                var playlistResult = JsonSerializer.Deserialize<JsonElement>(playlistJson);

                if (playlistResult.TryGetProperty("playlists", out var playlists) &&
                    playlists.TryGetProperty("items", out var items) &&
                    items.GetArrayLength() > 0)
                {
                    var playlistId = items[0].GetProperty("id").GetString();
                    if (!string.IsNullOrEmpty(playlistId))
                    {
                        return await GetPlaylistTracksAsync(playlistId);
                    }
                }
            }

            // Fallback to track search
            var trackQuery = Uri.EscapeDataString($"{genre} {query}");
            var trackResponse = await _httpClient.GetAsync($"https://api.spotify.com/v1/search?q={trackQuery}&type=track&limit=20");

            if (!trackResponse.IsSuccessStatusCode)
                return new List<SpotifyTrack>();

            var trackJson = await trackResponse.Content.ReadAsStringAsync();
            var trackResult = JsonSerializer.Deserialize<JsonElement>(trackJson);

            var tracks = new List<SpotifyTrack>();
            if (trackResult.TryGetProperty("tracks", out var tracksObj) &&
                tracksObj.TryGetProperty("items", out var trackItems))
            {
                foreach (var item in trackItems.EnumerateArray())
                {
                    if (item.TryGetProperty("uri", out var uri))
                    {
                        tracks.Add(new SpotifyTrack { Uri = uri.GetString() ?? "" });
                    }
                }
            }

            return tracks;
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"Spotify: Error searching tracks - {ex.Message}");
            return new List<SpotifyTrack>();
        }
    }

    private async Task<List<SpotifyTrack>> GetPlaylistTracksAsync(string playlistId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://api.spotify.com/v1/playlists/{playlistId}/tracks");
            if (!response.IsSuccessStatusCode)
                return new List<SpotifyTrack>();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);

            var tracks = new List<SpotifyTrack>();
            if (result.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("track", out var track) &&
                        track.TryGetProperty("uri", out var uri))
                    {
                        tracks.Add(new SpotifyTrack { Uri = uri.GetString() ?? "" });
                    }
                }
            }

            return tracks;
        }
        catch
        {
            return new List<SpotifyTrack>();
        }
    }

    private class SpotifyTrack
    {
        public string Uri { get; set; } = string.Empty;
    }
}

