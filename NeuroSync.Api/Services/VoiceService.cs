using Microsoft.AspNetCore.Hosting;
using System.Net.Http.Json;

namespace NeuroSync.Api.Services;

/// <summary>
/// Service for text-to-speech and voice cloning capabilities.
/// </summary>
public class VoiceService
{
    private readonly ILogger<VoiceService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _voiceStoragePath;
    private readonly string? _elevenLabsApiKey;
    private readonly HttpClient _httpClient;

    public VoiceService(
        ILogger<VoiceService> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _logger = logger;
        _environment = environment;
        _voiceStoragePath = Path.Combine(environment.ContentRootPath, "VoiceClones");
        
        // Ensure storage directory exists
        if (!Directory.Exists(_voiceStoragePath))
        {
            Directory.CreateDirectory(_voiceStoragePath);
        }

        // Get ElevenLabs API key from configuration (optional)
        _elevenLabsApiKey = configuration["ElevenLabs:ApiKey"];
        
        _httpClient = new HttpClient();
        if (!string.IsNullOrEmpty(_elevenLabsApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("xi-api-key", _elevenLabsApiKey);
        }
    }

    /// <summary>
    /// Generates speech from text using TTS.
    /// For now, returns text response with instructions to use frontend TTS.
    /// Can be extended to use Azure Speech SDK or ElevenLabs.
    /// </summary>
    public async Task<VoiceResponse> SpeakAsync(string text, string? voiceId = null, string? userId = null)
    {
        try
        {
            // If voice cloning is requested and voiceId is provided
            if (!string.IsNullOrEmpty(voiceId) && !string.IsNullOrEmpty(userId))
            {
                var clonedVoice = await SpeakWithClonedVoiceAsync(text, voiceId, userId);
                if (clonedVoice != null)
                {
                    return clonedVoice;
                }
            }

            // Default TTS response (frontend will handle actual speech)
            return new VoiceResponse
            {
                Text = text,
                RequiresFrontendTTS = true,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating speech");
            return new VoiceResponse
            {
                Text = text,
                RequiresFrontendTTS = true,
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Speaks text using a cloned voice.
    /// Uses ElevenLabs API for voice cloning.
    /// </summary>
    private async Task<VoiceResponse?> SpeakWithClonedVoiceAsync(string text, string voiceId, string userId)
    {
        if (string.IsNullOrEmpty(_elevenLabsApiKey))
        {
            _logger.LogWarning("ElevenLabs API key not configured. Using default TTS.");
            return null;
        }

        try
        {
            // Check if we have a cloned voice for this user
            var voiceClonePath = Path.Combine(_voiceStoragePath, userId, $"{voiceId}.json");
            if (!File.Exists(voiceClonePath))
            {
                _logger.LogWarning($"Voice clone not found for voiceId: {voiceId}");
                return null;
            }

            // Read voice clone metadata
            var voiceMetadata = await System.Text.Json.JsonSerializer.DeserializeAsync<VoiceCloneMetadata>(
                new FileStream(voiceClonePath, FileMode.Open));

            if (voiceMetadata == null || string.IsNullOrEmpty(voiceMetadata.ElevenLabsVoiceId))
            {
                _logger.LogWarning($"Invalid voice clone metadata for voiceId: {voiceId}");
                return null;
            }

            // Call ElevenLabs API to generate speech
            var requestBody = new
            {
                text = text,
                model_id = "eleven_multilingual_v2", // or "eleven_monolingual_v1"
                voice_settings = new
                {
                    stability = 0.5,
                    similarity_boost = 0.75
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"https://api.elevenlabs.io/v1/text-to-speech/{voiceMetadata.ElevenLabsVoiceId}",
                requestBody);

            if (response.IsSuccessStatusCode)
            {
                var audioBytes = await response.Content.ReadAsByteArrayAsync();
                
                // Save audio file temporarily
                var audioPath = Path.Combine(_voiceStoragePath, userId, $"temp_{Guid.NewGuid()}.mp3");
                await File.WriteAllBytesAsync(audioPath, audioBytes);

                return new VoiceResponse
                {
                    Text = text,
                    AudioPath = audioPath,
                    VoiceId = voiceId,
                    Success = true,
                    RequiresFrontendTTS = false
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"ElevenLabs API error: {response.StatusCode} - {errorContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error speaking with cloned voice");
            return null;
        }
    }

    /// <summary>
    /// Clones a voice from an audio sample using ElevenLabs.
    /// Note: Without ElevenLabs API key, returns a placeholder result.
    /// </summary>
    public async Task<VoiceCloneResult> CloneVoiceAsync(
        string userId,
        string personName,
        Stream audioStream,
        string fileName)
    {
        // If no API key, create a placeholder (for testing without API key)
        if (string.IsNullOrEmpty(_elevenLabsApiKey))
        {
            _logger.LogWarning("ElevenLabs API key not configured. Creating placeholder voice clone.");
            
            try
            {
                // Create user directory if it doesn't exist
                var userDir = Path.Combine(_voiceStoragePath, userId);
                if (!Directory.Exists(userDir))
                {
                    Directory.CreateDirectory(userDir);
                }

                // Save audio sample for future use
                var audioPath = Path.Combine(userDir, $"sample_{Guid.NewGuid()}{Path.GetExtension(fileName)}");
                using (var fileStream = new FileStream(audioPath, FileMode.Create))
                {
                    await audioStream.CopyToAsync(fileStream);
                }

                // Create placeholder voice clone metadata (without actual cloning)
                var voiceId = Guid.NewGuid().ToString();
                var metadata = new VoiceCloneMetadata
                {
                    VoiceId = voiceId,
                    PersonName = personName,
                    ElevenLabsVoiceId = "placeholder", // Placeholder
                    CreatedAt = DateTime.UtcNow,
                    AudioSamplePath = audioPath
                };

                var metadataPath = Path.Combine(userDir, $"{voiceId}.json");
                await System.Text.Json.JsonSerializer.SerializeAsync(
                    new FileStream(metadataPath, FileMode.Create),
                    metadata,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                _logger.LogInformation($"Created placeholder voice clone for {personName}. Voice ID: {voiceId}");

                return new VoiceCloneResult
                {
                    Success = true,
                    VoiceId = voiceId,
                    PersonName = personName,
                    Message = $"Voice sample saved for {personName}! (Note: ElevenLabs API key not configured - using placeholder. Configure API key for actual voice cloning.)"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating placeholder voice clone");
                return new VoiceCloneResult
                {
                    Success = false,
                    Error = $"Error saving voice sample: {ex.Message}"
                };
            }
        }

        try
        {
            // Create user directory if it doesn't exist
            var userDir = Path.Combine(_voiceStoragePath, userId);
            if (!Directory.Exists(userDir))
            {
                Directory.CreateDirectory(userDir);
            }

            // Save audio sample
            var audioPath = Path.Combine(userDir, $"sample_{Guid.NewGuid()}{Path.GetExtension(fileName)}");
            using (var fileStream = new FileStream(audioPath, FileMode.Create))
            {
                await audioStream.CopyToAsync(fileStream);
            }

            // Upload to ElevenLabs to create voice clone
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(personName), "name");
            formData.Add(new StringContent("A cloned voice"), "description");
            
            var audioContent = new ByteArrayContent(await File.ReadAllBytesAsync(audioPath));
            audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
            formData.Add(audioContent, "files", fileName);

            var response = await _httpClient.PostAsync(
                "https://api.elevenlabs.io/v1/voices/add",
                formData);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ElevenLabsVoiceResponse>();
                
                if (result?.voice_id != null)
                {
                    // Save voice clone metadata
                    var voiceId = Guid.NewGuid().ToString();
                    var metadata = new VoiceCloneMetadata
                    {
                        VoiceId = voiceId,
                        PersonName = personName,
                        ElevenLabsVoiceId = result.voice_id,
                        CreatedAt = DateTime.UtcNow,
                        AudioSamplePath = audioPath
                    };

                    var metadataPath = Path.Combine(userDir, $"{voiceId}.json");
                    await System.Text.Json.JsonSerializer.SerializeAsync(
                        new FileStream(metadataPath, FileMode.Create),
                        metadata,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                    _logger.LogInformation($"Voice cloned successfully for {personName}. Voice ID: {voiceId}");

                    return new VoiceCloneResult
                    {
                        Success = true,
                        VoiceId = voiceId,
                        PersonName = personName,
                        Message = $"Voice cloned successfully for {personName}! The AI can now speak in this voice."
                    };
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError($"ElevenLabs API error: {response.StatusCode} - {errorContent}");
            
            return new VoiceCloneResult
            {
                Success = false,
                Error = $"Failed to clone voice: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning voice");
            return new VoiceCloneResult
            {
                Success = false,
                Error = $"Error cloning voice: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets all cloned voices for a user.
    /// </summary>
    public List<VoiceCloneInfo> GetClonedVoices(string userId)
    {
        var userDir = Path.Combine(_voiceStoragePath, userId);
        if (!Directory.Exists(userDir))
        {
            return new List<VoiceCloneInfo>();
        }

        var voices = new List<VoiceCloneInfo>();
        var jsonFiles = Directory.GetFiles(userDir, "*.json");

        foreach (var jsonFile in jsonFiles)
        {
            try
            {
                var metadata = System.Text.Json.JsonSerializer.Deserialize<VoiceCloneMetadata>(
                    File.ReadAllText(jsonFile));
                
                if (metadata != null)
                {
                    voices.Add(new VoiceCloneInfo
                    {
                        VoiceId = metadata.VoiceId,
                        PersonName = metadata.PersonName,
                        CreatedAt = metadata.CreatedAt
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error reading voice clone metadata: {jsonFile}");
            }
        }

        return voices;
    }
}

/// <summary>
/// Response from voice service.
/// </summary>
public class VoiceResponse
{
    public string Text { get; set; } = string.Empty;
    public string? AudioPath { get; set; }
    public string? VoiceId { get; set; }
    public bool Success { get; set; }
    public bool RequiresFrontendTTS { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result of voice cloning operation.
/// </summary>
public class VoiceCloneResult
{
    public bool Success { get; set; }
    public string? VoiceId { get; set; }
    public string? PersonName { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Information about a cloned voice.
/// </summary>
public class VoiceCloneInfo
{
    public string VoiceId { get; set; } = string.Empty;
    public string PersonName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Metadata for a cloned voice.
/// </summary>
internal class VoiceCloneMetadata
{
    public string VoiceId { get; set; } = string.Empty;
    public string PersonName { get; set; } = string.Empty;
    public string ElevenLabsVoiceId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? AudioSamplePath { get; set; }
}

/// <summary>
/// Response from ElevenLabs API when creating a voice.
/// </summary>
internal class ElevenLabsVoiceResponse
{
    public string? voice_id { get; set; }
}
