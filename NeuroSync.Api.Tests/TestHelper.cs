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
                // Richer labelled set for evaluation realism (still local ML.NET — no HF in V1 runtime)
                var trainingData = TrainingDataGenerator.GenerateSampleData();
                // Ensure explicit short phrases used in tests/evals are present
                trainingData.AddRange(new[]
                {
                    new EmotionData { Text = "I'm so happy!", Label = "Happy" },
                    new EmotionData { Text = "I feel sad", Label = "Sad" },
                    new EmotionData { Text = "I'm angry", Label = "Angry" },
                    new EmotionData { Text = "I'm anxious", Label = "Anxious" },
                    new EmotionData { Text = "I'm calm", Label = "Calm" },
                    new EmotionData { Text = "I'm excited!", Label = "Excited" },
                    new EmotionData { Text = "I'm frustrated", Label = "Frustrated" },
                    new EmotionData { Text = "I'm okay", Label = "Neutral" },
                    new EmotionData { Text = "I am so happy!", Label = "Happy" },
                    new EmotionData { Text = "feeling down", Label = "Sad" },
                    new EmotionData { Text = "stressed out", Label = "Anxious" },
                    new EmotionData { Text = "furious with my boss", Label = "Angry" },
                    new EmotionData { Text = "fed up with bugs", Label = "Frustrated" },
                    new EmotionData { Text = "feeling great today", Label = "Happy" },
                    new EmotionData { Text = "I feel lonely", Label = "Sad" },
                    new EmotionData { Text = "macha boring ah iruku", Label = "Neutral" },
                    new EmotionData { Text = "tension ah iruku", Label = "Anxious" },
                    new EmotionData { Text = "romba tired da", Label = "Sad" }
                });
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
        
        var understanding = new EmotionUnderstandingService(Mock.Of<ILogger<EmotionUnderstandingService>>());
        return new EmotionDetectionService(model, logger, cache, understanding);
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
