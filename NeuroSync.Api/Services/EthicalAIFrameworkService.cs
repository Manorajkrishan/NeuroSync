using NeuroSync.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace NeuroSync.Api.Services;

/// <summary>
/// Ethical AI Framework Service
/// Manages consent, privacy, transparency, and psychological safety
/// </summary>
public class EthicalAIFrameworkService
{
    private readonly ILogger<EthicalAIFrameworkService> _logger;
    private readonly ConcurrentDictionary<string, EthicalAIConsent> _consents;
    private readonly EthicalAIConfig _config;
    private readonly string _storagePath;

    public EthicalAIFrameworkService(
        ILogger<EthicalAIFrameworkService> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _consents = new ConcurrentDictionary<string, EthicalAIConsent>();
        _config = new EthicalAIConfig();
        _storagePath = Path.Combine(environment.ContentRootPath, "UserConsents");

        // Ensure storage directory exists
        Directory.CreateDirectory(_storagePath);

        // Load existing consents
        LoadConsents();
    }

    /// <summary>
    /// Check if user has consented to emotion sensing
    /// </summary>
    public bool HasConsent(string userId, ConsentType consentType)
    {
        if (!_consents.TryGetValue(userId, out var consent))
        {
            return !_config.RequireExplicitConsent; // Default: allow if not required
        }

        if (consent.OptedOut)
            return false;

        return consentType switch
        {
            ConsentType.EmotionSensing => consent.EmotionSensingConsent,
            ConsentType.VisualLayer => consent.VisualLayerConsent,
            ConsentType.AudioLayer => consent.AudioLayerConsent,
            ConsentType.BiometricLayer => consent.BiometricLayerConsent,
            ConsentType.DataStorage => consent.DataStorageConsent,
            ConsentType.DataSharing => consent.DataSharingConsent,
            _ => false
        };
    }

    /// <summary>
    /// Get user consent settings
    /// </summary>
    public EthicalAIConsent? GetConsent(string userId)
    {
        return _consents.TryGetValue(userId, out var consent) ? consent : null;
    }

    /// <summary>
    /// Set user consent
    /// </summary>
    public void SetConsent(string userId, EthicalAIConsent consent)
    {
        consent.UserId = userId;
        consent.LastUpdated = DateTime.UtcNow;
        if (consent.ConsentTimestamp == default)
            consent.ConsentTimestamp = DateTime.UtcNow;

        _consents.AddOrUpdate(userId, consent, (key, old) => consent);
        SaveConsent(userId, consent);

        _logger.LogInformation("Consent updated for user: {UserId}, EmotionSensing: {EmotionSensing}, Visual: {Visual}, Audio: {Audio}, Biometric: {Biometric}",
            userId, consent.EmotionSensingConsent, consent.VisualLayerConsent, consent.AudioLayerConsent, consent.BiometricLayerConsent);
    }

    /// <summary>
    /// Anonymize user data based on privacy settings
    /// </summary>
    public string AnonymizeData(string userId, string data)
    {
        var consent = GetConsent(userId);
        if (consent?.AnonymizationEnabled == true)
        {
            // Simple anonymization - replace userId with hash
            var hash = userId.GetHashCode().ToString();
            return data.Replace(userId, $"user_{hash}");
        }

        return data;
    }

    /// <summary>
    /// Check if data retention period has expired
    /// </summary>
    public bool IsDataRetentionExpired(DateTime timestamp)
    {
        return DateTime.UtcNow - timestamp > TimeSpan.FromDays(_config.MaxDataRetentionDays);
    }

    /// <summary>
    /// Get ethical AI configuration
    /// </summary>
    public EthicalAIConfig GetConfig()
    {
        return _config;
    }

    /// <summary>
    /// Load consents from storage
    /// </summary>
    private void LoadConsents()
    {
        try
        {
            if (!Directory.Exists(_storagePath))
                return;

            foreach (var file in Directory.GetFiles(_storagePath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var consent = System.Text.Json.JsonSerializer.Deserialize<EthicalAIConsent>(json);
                    if (consent != null && !string.IsNullOrEmpty(consent.UserId))
                    {
                        _consents.TryAdd(consent.UserId, consent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load consent from file: {File}", file);
                }
            }

            _logger.LogInformation("Loaded {Count} user consents", _consents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading consents");
        }
    }

    /// <summary>
    /// Save consent to storage
    /// </summary>
    private void SaveConsent(string userId, EthicalAIConsent consent)
    {
        try
        {
            var filePath = Path.Combine(_storagePath, $"{userId}.json");
            var json = System.Text.Json.JsonSerializer.Serialize(consent, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving consent for user: {UserId}", userId);
        }
    }
}

/// <summary>
/// Consent type enumeration
/// </summary>
public enum ConsentType
{
    EmotionSensing,
    VisualLayer,
    AudioLayer,
    BiometricLayer,
    DataStorage,
    DataSharing
}
