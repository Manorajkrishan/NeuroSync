using Microsoft.ML;
using NeuroSync.Core;
using NeuroSync.ML;

namespace NeuroSync.Api.Services;

/// <summary>
/// Service for emotion detection using ML.NET.
/// Optimized with caching for faster responses.
/// </summary>
public class EmotionDetectionService
{
    private readonly EmotionPredictionService _predictionService;
    private readonly ILogger<EmotionDetectionService> _logger;
    private readonly PredictionCache? _cache;
    private readonly EmotionUnderstandingService? _understanding;

    public EmotionDetectionService(
        ITransformer model, 
        ILogger<EmotionDetectionService> logger,
        PredictionCache? cache = null,
        EmotionUnderstandingService? understanding = null)
    {
        _predictionService = new EmotionPredictionService(model);
        _logger = logger;
        _cache = cache;
        _understanding = understanding;
    }

    public EmotionResult DetectEmotion(string text)
    {
        try
        {
            if (_predictionService == null)
            {
                _logger.LogError("Prediction service is null!");
                throw new InvalidOperationException("Prediction service is not initialized");
            }

            // Don't use cache for understanding path — we refine every time
            // (cache key would miss intensity/cause). Still use ML cache internally if needed.
            EmotionResult mlResult;
            if (_cache != null)
            {
                var cached = _cache.GetCached(text);
                if (cached != null)
                {
                    mlResult = new EmotionResult(cached.Emotion, cached.Confidence, text);
                }
                else
                {
                    mlResult = _predictionService.Predict(text);
                    _cache.Cache(text, mlResult);
                }
            }
            else
            {
                mlResult = _predictionService.Predict(text);
            }

            // Deep understanding layer (best-friend reading of emotion)
            var understood = _understanding != null
                ? _understanding.Understand(mlResult, text)
                : mlResult;

            _logger.LogInformation(
                "Emotion: {Emotion} ({Confidence:P0}, {Intensity}) — {Understood}",
                understood.Emotion, understood.Confidence, understood.Intensity, understood.UnderstoodAs);
            return understood;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting emotion: {Message}", ex.Message);
            throw;
        }
    }
}

