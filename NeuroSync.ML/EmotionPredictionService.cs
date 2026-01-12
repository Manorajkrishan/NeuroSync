using Microsoft.ML;
using NeuroSync.Core;

namespace NeuroSync.ML;

/// <summary>
/// Service for predicting emotions using the trained ML model.
/// </summary>
public class EmotionPredictionService
{
    private readonly PredictionEngine<EmotionData, EmotionPrediction> _predictionEngine;
    private readonly EmotionModelTrainer _trainer;

    public EmotionPredictionService(ITransformer model)
    {
        _trainer = new EmotionModelTrainer();
        _predictionEngine = _trainer.CreatePredictionEngine(model);
    }

    /// <summary>
    /// Predicts the emotion from text input.
    /// </summary>
    public EmotionResult Predict(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new EmotionResult(EmotionType.Neutral, 1.0f, text);
        }

        var input = new EmotionData { Text = text, Label = string.Empty };
        var prediction = _predictionEngine.Predict(input);

        var emotion = EmotionModelTrainer.ParseEmotion(prediction.PredictedLabel);
        
        // Calculate confidence from scores
        float confidence = 0.0f;
        if (prediction.Score != null && prediction.Score.Length > 0)
        {
            confidence = prediction.Score.Max();
        }

        return new EmotionResult(emotion, confidence, text);
    }

    public void Dispose()
    {
        _predictionEngine?.Dispose();
    }
}


