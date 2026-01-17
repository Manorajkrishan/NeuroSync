using Microsoft.ML;
using Microsoft.Extensions.Logging;
using NeuroSync.Api.Services;
using NeuroSync.ML;
using Moq;
using NeuroSync.Core;

namespace NeuroSync.Api.Tests;

/// <summary>
/// Helper class for test setup and model initialization
/// </summary>
public static class TestHelper
{
    private static ITransformer? _testModel;
    private static readonly object _modelLock = new object();

    /// <summary>
    /// Creates or gets a test ML model
    /// </summary>
    public static ITransformer GetTestModel()
    {
        if (_testModel != null)
            return _testModel;

        lock (_modelLock)
        {
            if (_testModel != null)
                return _testModel;

            try
            {
                var trainer = new EmotionModelTrainer();
                // Use sample data for tests (faster than comprehensive data)
                var trainingData = new List<EmotionData>
                {
                    new EmotionData { Text = "I'm so happy!", Label = "Happy" },
                    new EmotionData { Text = "I feel sad", Label = "Sad" },
                    new EmotionData { Text = "I'm angry", Label = "Angry" },
                    new EmotionData { Text = "I'm anxious", Label = "Anxious" },
                    new EmotionData { Text = "I'm calm", Label = "Calm" },
                    new EmotionData { Text = "I'm excited!", Label = "Excited" },
                    new EmotionData { Text = "I'm frustrated", Label = "Frustrated" },
                    new EmotionData { Text = "I'm okay", Label = "Neutral" },
                    // Add more samples for each emotion
                    new EmotionData { Text = "This is wonderful!", Label = "Happy" },
                    new EmotionData { Text = "I'm feeling down", Label = "Sad" },
                    new EmotionData { Text = "I'm mad", Label = "Angry" },
                    new EmotionData { Text = "I'm worried", Label = "Anxious" },
                    new EmotionData { Text = "I'm relaxed", Label = "Calm" },
                    new EmotionData { Text = "I'm thrilled!", Label = "Excited" },
                    new EmotionData { Text = "This is annoying", Label = "Frustrated" },
                    new EmotionData { Text = "Nothing special", Label = "Neutral" }
                };
                _testModel = trainer.TrainModel(trainingData);
                return _testModel;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create test model: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Creates a test EmotionDetectionService
    /// </summary>
    public static EmotionDetectionService CreateEmotionDetectionService(
        ILogger<EmotionDetectionService>? logger = null,
        PredictionCache? cache = null)
    {
        var model = GetTestModel();
        logger ??= Mock.Of<ILogger<EmotionDetectionService>>();
        cache ??= CreatePredictionCache();
        
        return new EmotionDetectionService(model, logger, cache);
    }

    /// <summary>
    /// Creates a test PredictionCache
    /// </summary>
    public static PredictionCache CreatePredictionCache()
    {
        // PredictionCache has no constructor parameters
        return new PredictionCache();
    }
}
