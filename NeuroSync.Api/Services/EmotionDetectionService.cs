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

    public EmotionDetectionService(
        ITransformer model, 
        ILogger<EmotionDetectionService> logger,
        PredictionCache? cache = null)
    {
        _predictionService = new EmotionPredictionService(model);
        _logger = logger;
        _cache = cache;
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

            // Check cache first for faster response
            if (_cache != null)
            {
                var cached = _cache.GetCached(text);
                if (cached != null)
                {
                    _logger.LogDebug($"Cache hit for: {text.Substring(0, Math.Min(50, text.Length))}...");
                    return cached;
                }
            }

            // Predict using model
            var result = _predictionService.Predict(text);
            
            // Cache the result for faster future responses
            _cache?.Cache(text, result);
            
            _logger.LogInformation($"Emotion detected: {result.Emotion} with confidence: {result.Confidence:P2}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting emotion: {Message}", ex.Message);
            _logger.LogError(ex, "Inner exception: {InnerException}", ex.InnerException?.Message);
            _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            throw; // Re-throw to see the actual error in controller
        }
    }
}

