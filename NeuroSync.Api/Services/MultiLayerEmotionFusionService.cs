using NeuroSync.Core;
using Microsoft.Extensions.Logging;

namespace NeuroSync.Api.Services;

/// <summary>
/// Multi-layer emotion fusion service that combines Visual, Audio, Biometric, and Contextual layers
/// </summary>
public class MultiLayerEmotionFusionService
{
    private readonly ILogger<MultiLayerEmotionFusionService> _logger;
    private readonly LayerWeights _defaultWeights;

    public MultiLayerEmotionFusionService(ILogger<MultiLayerEmotionFusionService> logger)
    {
        _logger = logger;
        _defaultWeights = new LayerWeights
        {
            VisualWeight = 0.3f,
            AudioWeight = 0.3f,
            BiometricWeight = 0.2f,
            ContextualWeight = 0.2f
        };
    }

    /// <summary>
    /// Fuse emotions from all available layers into a single comprehensive result
    /// </summary>
    public MultiLayerEmotionResult FuseEmotions(
        VisualEmotionData? visual = null,
        AudioEmotionData? audio = null,
        BiometricEmotionData? biometric = null,
        ContextualEmotionData? contextual = null,
        LayerWeights? customWeights = null,
        string? userId = null)
    {
        var weights = customWeights ?? _defaultWeights;
        var result = new MultiLayerEmotionResult
        {
            VisualLayer = visual,
            AudioLayer = audio,
            BiometricLayer = biometric,
            ContextualLayer = contextual,
            Weights = weights,
            UserId = userId
        };

        // Collect all available emotions with their scores
        var emotionScores = new Dictionary<EmotionType, float>();

        // Layer 1: Visual
        if (visual?.Emotion != null && visual.Confidence > 0)
        {
            var score = visual.Confidence * weights.VisualWeight;
            emotionScores[visual.Emotion.Value] = emotionScores.GetValueOrDefault(visual.Emotion.Value) + score;
        }

        // Layer 2: Audio
        if (audio?.Emotion != null && audio.Confidence > 0)
        {
            var score = audio.Confidence * weights.AudioWeight;
            emotionScores[audio.Emotion.Value] = emotionScores.GetValueOrDefault(audio.Emotion.Value) + score;
        }

        // Layer 3: Biometric
        if (biometric?.Emotion != null && biometric.Confidence > 0)
        {
            var score = biometric.Confidence * weights.BiometricWeight;
            emotionScores[biometric.Emotion.Value] = emotionScores.GetValueOrDefault(biometric.Emotion.Value) + score;
        }

        // Layer 4: Contextual
        if (contextual?.Emotion != null && contextual.Confidence > 0)
        {
            var score = contextual.Confidence * weights.ContextualWeight;
            emotionScores[contextual.Emotion.Value] = emotionScores.GetValueOrDefault(contextual.Emotion.Value) + score;
        }

        // Determine primary emotion (highest score)
        if (emotionScores.Count > 0)
        {
            result.PrimaryEmotion = emotionScores.OrderByDescending(kvp => kvp.Value).First().Key;
            
            // Calculate overall confidence (normalized sum of all scores)
            var totalScore = emotionScores.Values.Sum();
            var maxPossibleScore = weights.VisualWeight + weights.AudioWeight + weights.BiometricWeight + weights.ContextualWeight;
            result.OverallConfidence = Math.Min(1.0f, totalScore / maxPossibleScore);
        }
        else
        {
            // Default to Neutral if no layers provided
            result.PrimaryEmotion = EmotionType.Neutral;
            result.OverallConfidence = 0.0f;
        }

        _logger.LogInformation(
            "Fused emotions - Primary: {Emotion}, Confidence: {Confidence:P2}, Layers: Visual={Visual}, Audio={Audio}, Biometric={Biometric}, Contextual={Contextual}",
            result.PrimaryEmotion,
            result.OverallConfidence,
            visual != null,
            audio != null,
            biometric != null,
            contextual != null);

        return result;
    }

    /// <summary>
    /// Calculate certainty score based on agreement between layers
    /// </summary>
    public float CalculateCertainty(MultiLayerEmotionResult result)
    {
        var emotions = new List<EmotionType>();
        var confidences = new List<float>();

        if (result.VisualLayer?.Emotion != null)
        {
            emotions.Add(result.VisualLayer.Emotion.Value);
            confidences.Add(result.VisualLayer.Confidence);
        }

        if (result.AudioLayer?.Emotion != null)
        {
            emotions.Add(result.AudioLayer.Emotion.Value);
            confidences.Add(result.AudioLayer.Confidence);
        }

        if (result.BiometricLayer?.Emotion != null)
        {
            emotions.Add(result.BiometricLayer.Emotion.Value);
            confidences.Add(result.BiometricLayer.Confidence);
        }

        if (result.ContextualLayer?.Emotion != null)
        {
            emotions.Add(result.ContextualLayer.Emotion.Value);
            confidences.Add(result.ContextualLayer.Confidence);
        }

        if (emotions.Count == 0)
            return 0.0f;

        // Calculate agreement (how many layers agree with primary emotion)
        var agreement = emotions.Count(e => e == result.PrimaryEmotion) / (float)emotions.Count;
        
        // Average confidence of agreeing layers
        var avgConfidence = confidences.Average();
        
        // Certainty = agreement * average confidence
        return agreement * avgConfidence;
    }
}
