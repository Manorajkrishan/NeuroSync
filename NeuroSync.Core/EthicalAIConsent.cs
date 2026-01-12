namespace NeuroSync.Core;

/// <summary>
/// Ethical AI consent and privacy framework
/// </summary>
public class EthicalAIConsent
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Consent for emotion sensing
    /// </summary>
    public bool EmotionSensingConsent { get; set; }
    
    /// <summary>
    /// Consent for visual layer (facial recognition)
    /// </summary>
    public bool VisualLayerConsent { get; set; }
    
    /// <summary>
    /// Consent for audio layer (voice analysis)
    /// </summary>
    public bool AudioLayerConsent { get; set; }
    
    /// <summary>
    /// Consent for biometric layer (heart rate, etc.)
    /// </summary>
    public bool BiometricLayerConsent { get; set; }
    
    /// <summary>
    /// Consent for data storage
    /// </summary>
    public bool DataStorageConsent { get; set; }
    
    /// <summary>
    /// Consent for data sharing (if applicable)
    /// </summary>
    public bool DataSharingConsent { get; set; }
    
    /// <summary>
    /// Consent timestamp
    /// </summary>
    public DateTime ConsentTimestamp { get; set; }
    
    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; }
    
    /// <summary>
    /// Privacy level (0 = strict, 100 = full sharing)
    /// </summary>
    public int PrivacyLevel { get; set; } = 50;
    
    /// <summary>
    /// Data anonymization enabled
    /// </summary>
    public bool AnonymizationEnabled { get; set; } = true;
    
    /// <summary>
    /// Opt-out flag
    /// </summary>
    public bool OptedOut { get; set; }
}

/// <summary>
/// Ethical AI configuration
/// </summary>
public class EthicalAIConfig
{
    /// <summary>
    /// Require explicit consent before emotion sensing
    /// </summary>
    public bool RequireExplicitConsent { get; set; } = true;
    
    /// <summary>
    /// Enable data anonymization by default
    /// </summary>
    public bool DefaultAnonymization { get; set; } = true;
    
    /// <summary>
    /// Default privacy level (0-100)
    /// </summary>
    public int DefaultPrivacyLevel { get; set; } = 50;
    
    /// <summary>
    /// Enable transparency mode (user can see what data is collected)
    /// </summary>
    public bool TransparencyMode { get; set; } = true;
    
    /// <summary>
    /// Psychological safety compliance enabled
    /// </summary>
    public bool PsychologicalSafetyEnabled { get; set; } = true;
    
    /// <summary>
    /// Maximum data retention period in days
    /// </summary>
    public int MaxDataRetentionDays { get; set; } = 365;
}
