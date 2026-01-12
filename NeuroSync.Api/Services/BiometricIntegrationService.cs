using NeuroSync.Core;
using Microsoft.Extensions.Logging;

namespace NeuroSync.Api.Services;

/// <summary>
/// Biometric integration service for Layer 3: Biometric Emotional Analysis
/// Integrates heart rate variability, skin conductivity, and temperature tracking
/// </summary>
public class BiometricIntegrationService
{
    private readonly ILogger<BiometricIntegrationService> _logger;

    public BiometricIntegrationService(ILogger<BiometricIntegrationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyze biometric data for emotion detection
    /// </summary>
    public BiometricEmotionData AnalyzeBiometrics(
        float? heartRate = null,
        float? hrv = null,
        float? skinConductivity = null,
        float? temperature = null)
    {
        var result = new BiometricEmotionData
        {
            Confidence = 0.0f
        };

        // Heart rate analysis
        if (heartRate.HasValue || hrv.HasValue)
        {
            result.HeartRate = new HeartRateData
            {
                HeartRate = heartRate,
                HRV = hrv
            };
            result.HeartRate.InferredEmotion = InferEmotionFromHeartRate(heartRate, hrv);
        }

        // Skin conductivity (Galvanic Skin Response - GSR)
        if (skinConductivity.HasValue)
        {
            result.SkinConductivity = skinConductivity.Value;
        }

        // Temperature tracking
        if (temperature.HasValue)
        {
            result.Temperature = temperature.Value;
        }

        // Determine emotion from all biometric indicators
        result.Emotion = DetermineEmotionFromBiometrics(result);
        result.Confidence = CalculateBiometricConfidence(result);

        _logger.LogDebug("Biometric analysis - Emotion: {Emotion}, Confidence: {Confidence:P2}, HR: {HR}, GSR: {GSR}, Temp: {Temp}",
            result.Emotion, result.Confidence, heartRate, skinConductivity, temperature);

        return result;
    }

    /// <summary>
    /// Infer emotion from heart rate data
    /// </summary>
    private EmotionType? InferEmotionFromHeartRate(float? heartRate, float? hrv)
    {
        if (!heartRate.HasValue)
            return null;

        var hr = heartRate.Value;

        // High heart rate (>100 bpm) = stress/anxiety/excitement
        if (hr > 100)
        {
            // Low HRV = stress/anxiety
            if (hrv.HasValue && hrv < 30)
                return EmotionType.Anxious;
            // High HRV = excitement
            if (hrv.HasValue && hrv > 50)
                return EmotionType.Excited;
            
            return EmotionType.Anxious;
        }

        // Low heart rate (<60 bpm) = calm/relaxed
        if (hr < 60)
        {
            return EmotionType.Calm;
        }

        // Normal heart rate (60-100 bpm) = neutral
        return EmotionType.Neutral;
    }

    /// <summary>
    /// Determine emotion from all biometric indicators
    /// </summary>
    private EmotionType? DetermineEmotionFromBiometrics(BiometricEmotionData data)
    {
        var emotions = new List<EmotionType?>();

        if (data.HeartRate?.InferredEmotion != null)
            emotions.Add(data.HeartRate.InferredEmotion);

        // High skin conductivity = stress/anxiety
        if (data.SkinConductivity.HasValue && data.SkinConductivity > 5.0f)
            emotions.Add(EmotionType.Anxious);

        // Elevated temperature = stress/excitement
        if (data.Temperature.HasValue && data.Temperature > 98.6f)
        {
            // Could be excitement or stress
            emotions.Add(EmotionType.Excited);
        }

        // Return most common emotion
        if (emotions.Count == 0)
            return EmotionType.Neutral;

        return emotions.GroupBy(e => e)
                      .OrderByDescending(g => g.Count())
                      .FirstOrDefault()?.Key ?? EmotionType.Neutral;
    }

    /// <summary>
    /// Calculate confidence score for biometric analysis
    /// </summary>
    private float CalculateBiometricConfidence(BiometricEmotionData data)
    {
        var confidence = 0.0f;
        var factors = 0;

        if (data.HeartRate != null)
        {
            confidence += 0.5f;
            factors++;
        }

        if (data.SkinConductivity.HasValue)
        {
            confidence += 0.3f;
            factors++;
        }

        if (data.Temperature.HasValue)
        {
            confidence += 0.2f;
            factors++;
        }

        return factors > 0 ? Math.Min(1.0f, confidence) : 0.0f;
    }
}
