using NeuroSync.IoT.Interfaces;
using System.Text.Json;

namespace NeuroSync.IoT.Services;

/// <summary>
/// YouTube Music service integration.
/// Uses YouTube Data API v3 and YouTube Music API.
/// </summary>
public class YouTubeMusicService : IMusicService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string? _accessToken;
    private readonly Action<string>? _logger;

    public string ServiceName => "YouTube Music";

    public YouTubeMusicService(
        HttpClient httpClient,
        string? apiKey = null,
        string? accessToken = null,
        Action<string>? logger = null)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _accessToken = accessToken;
        _logger = logger;
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (string.IsNullOrEmpty(_apiKey) && string.IsNullOrEmpty(_accessToken))
        {
            _logger?.Invoke("YouTube Music: Not configured (missing API key)");
            return false;
        }

        // Test connection with a simple search
        try
        {
            var testQuery = "test";
            var query = Uri.EscapeDataString(testQuery);
            var apiKeyParam = string.IsNullOrEmpty(_apiKey) ? "" : $"&key={_apiKey}";
            var authHeader = !string.IsNullOrEmpty(_accessToken) ? $"Bearer {_accessToken}" : null;

            _httpClient.DefaultRequestHeaders.Clear();
            if (authHeader != null)
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
            }

            var response = await _httpClient.GetAsync($"https://www.googleapis.com/youtube/v3/search?part=snippet&q={query}&type=video&maxResults=1{apiKeyParam}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"YouTube Music: Connection test failed - {ex.Message}");
            return false;
        }
    }

    public async Task<(bool Success, string? PlaylistUrl)> PlayMusicWithUrlAsync(string deviceId, string genre, string playlist, int volume)
    {
        try
        {
            _logger?.Invoke($"YouTube Music: Playing {playlist} (genre: {genre}) on {deviceId} at {volume}%");

            // Search for videos/playlists
            var searchQuery = !string.IsNullOrEmpty(playlist) ? playlist : genre;
            var videoIds = await SearchVideosAsync(searchQuery, genre);

            if (videoIds == null || videoIds.Count == 0)
            {
                _logger?.Invoke("YouTube Music: No videos found");
                return (false, null);
            }

            // Note: YouTube Music API doesn't have direct playback control like Spotify
            // This creates a playlist URL that can be opened or sent to a device
            // For actual playback, you'd need:
            // 1. YouTube Music Premium API (limited availability)
            // 2. Cast SDK for Chromecast
            // 3. Or open the URL in a browser/player

            var playlistUrl = $"https://music.youtube.com/watch?v={videoIds.First()}";
            if (videoIds.Count > 1)
            {
                playlistUrl = $"https://music.youtube.com/watch?v={string.Join("&v=", videoIds.Take(5))}";
            }

            _logger?.Invoke($"YouTube Music: Playlist URL generated - {playlistUrl}");
            _logger?.Invoke("YouTube Music: Note - Direct playback requires YouTube Music Premium API or Cast SDK");
            _logger?.Invoke($"YouTube Music: To play music, open this URL: {playlistUrl}");
            
            return (true, playlistUrl);
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"YouTube Music: Error playing music - {ex.Message}");
            return (false, null);
        }
    }

    public async Task<bool> PlayMusicAsync(string deviceId, string genre, string playlist, int volume)
    {
        var (success, _) = await PlayMusicWithUrlAsync(deviceId, genre, playlist, volume);
        return success;
    }

    public async Task<bool> StopMusicAsync(string deviceId)
    {
        try
        {
            // YouTube Music doesn't have a direct stop API without Premium
            _logger?.Invoke("YouTube Music: Stop requires YouTube Music Premium API");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"YouTube Music: Error stopping music - {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SetVolumeAsync(string deviceId, int volume)
    {
        try
        {
            // Volume control requires device integration (Cast SDK, etc.)
            _logger?.Invoke($"YouTube Music: Volume control requires device integration");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"YouTube Music: Error setting volume - {ex.Message}");
            return false;
        }
    }

    public async Task<List<string>> GetPlaylistsByGenreAsync(string genre)
    {
        try
        {
            var query = Uri.EscapeDataString(genre);
            var apiKeyParam = string.IsNullOrEmpty(_apiKey) ? "" : $"&key={_apiKey}";
            var authHeader = !string.IsNullOrEmpty(_accessToken) ? $"Bearer {_accessToken}" : null;

            _httpClient.DefaultRequestHeaders.Clear();
            if (authHeader != null)
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
            }

            // Search for playlists
            var response = await _httpClient.GetAsync($"https://www.googleapis.com/youtube/v3/search?part=snippet&q={query}&type=playlist&maxResults=20{apiKeyParam}");

            if (!response.IsSuccessStatusCode)
                return new List<string>();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);

            var playlists = new List<string>();
            if (result.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("snippet", out var snippet) &&
                        snippet.TryGetProperty("title", out var title))
                    {
                        playlists.Add(title.GetString() ?? "");
                    }
                }
            }

            return playlists;
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"YouTube Music: Error getting playlists - {ex.Message}");
            return new List<string>();
        }
    }

    private async Task<List<string>> SearchVideosAsync(string query, string genre)
    {
        try
        {
            var searchQuery = Uri.EscapeDataString($"{genre} {query} music");
            var apiKeyParam = string.IsNullOrEmpty(_apiKey) ? "" : $"&key={_apiKey}";
            var authHeader = !string.IsNullOrEmpty(_accessToken) ? $"Bearer {_accessToken}" : null;

            _httpClient.DefaultRequestHeaders.Clear();
            if (authHeader != null)
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
            }

            // Search for music videos
            var response = await _httpClient.GetAsync($"https://www.googleapis.com/youtube/v3/search?part=snippet&q={searchQuery}&type=video&videoCategoryId=10&maxResults=20{apiKeyParam}");

            if (!response.IsSuccessStatusCode)
                return new List<string>();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);

            var videoIds = new List<string>();
            if (result.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var id) &&
                        id.TryGetProperty("videoId", out var videoId))
                    {
                        videoIds.Add(videoId.GetString() ?? "");
                    }
                }
            }

            return videoIds;
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"YouTube Music: Error searching videos - {ex.Message}");
            return new List<string>();
        }
    }
}
