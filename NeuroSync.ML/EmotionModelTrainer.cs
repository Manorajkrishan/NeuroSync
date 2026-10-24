using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using NeuroSync.Core;

namespace NeuroSync.ML;

/// <summary>
/// Service for training the emotion classification model.
/// </summary>
public class EmotionModelTrainer
{
    private readonly MLContext _mlContext;

    public EmotionModelTrainer()
    {
        _mlContext = new MLContext(seed: 0);
    }

    /// <summary>
    /// Trains the emotion classification model using only local ML.NET components.
    /// The production runtime must not download pretrained embedding assets at startup.
    /// </summary>
    public ITransformer TrainModel(List<EmotionData> trainingData, string? modelPath = null)
    {
        var prepared = EmotionDatasetPreprocessor.Prepare(trainingData);
        if (prepared.Examples.Count < 16)
            throw new InvalidOperationException("Not enough clean emotion examples to train the model.");

        Console.WriteLine($"Training input rows: {prepared.InputRows:N0}");
        Console.WriteLine($"Clean unique rows: {prepared.Examples.Count:N0}");
        Console.WriteLine($"Duplicate rows removed: {prepared.DuplicateRowsRemoved:N0}");
        Console.WriteLine($"Conflicting texts removed: {prepared.ConflictingTextsRemoved:N0}");
        Console.WriteLine("Label counts: " + string.Join(", ", prepared.LabelCounts.Select(kv => $"{kv.Key}={kv.Value}")));

        var split = EmotionDatasetPreprocessor.DeterministicStratifiedSplit(prepared.Examples, 0.2);
        var balancedTrain = EmotionDatasetPreprocessor.BalanceForTraining(split.Train);
        var balancedFull = EmotionDatasetPreprocessor.BalanceForTraining(prepared.Examples);
        var trainView = _mlContext.Data.LoadFromEnumerable(balancedTrain);
        var testView = _mlContext.Data.LoadFromEnumerable(split.Test);
        var fullView = _mlContext.Data.LoadFromEnumerable(balancedFull);

        Console.WriteLine($"Training set: {split.Train.Count:N0} unique examples -> {balancedTrain.Count:N0} balanced fit rows");
        Console.WriteLine($"Test set: {split.Test.Count:N0} unique examples");
        Console.WriteLine($"Final fit rows: {balancedFull.Count:N0}");

        // Production-safe text pipeline.
        // FeaturizeText and SDCA are fully local and do not fetch pretrained resources.
        // This keeps cold starts deterministic on sandboxed/free hosting environments.
        var trainingPipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label", "Label")
            .Append(_mlContext.Transforms.Text.FeaturizeText(
                outputColumnName: "Features",
                inputColumnName: "Text"))
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName: "Label",
                featureColumnName: "Features"));

        // Train the model
        Console.WriteLine("Training started with local text features...");
        var trainedModel = trainingPipeline.Fit(trainView);

        // Evaluate on a deterministic, deduplicated, label-stratified holdout.
        var predictions = trainedModel.Transform(testView);
        var metrics = _mlContext.MulticlassClassification.Evaluate(
            predictions,
            labelColumnName: "Label",
            scoreColumnName: "Score",
            predictedLabelColumnName: "PredictedLabel");

        // Now add the MapKeyToValue for the final model (for prediction)
        var finalPipeline = trainingPipeline
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel", "PredictedLabel"));
        
        // Retrain with the full pipeline for production use
        // Production model is trained on every clean unique example after honest holdout evaluation.
        var model = finalPipeline.Fit(fullView);

        // Write to both console and use proper logging
        var accuracyMsg = $"Model Accuracy: {metrics.MacroAccuracy:P2}";
        var logLossMsg = $"Log Loss: {metrics.LogLoss:F4}";
        
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine(accuracyMsg);
        Console.WriteLine(logLossMsg);
        if (metrics.PerClassLogLoss != null && metrics.PerClassLogLoss.Count > 0)
        {
            var perClassLogLossMsg = $"Per-class Log Loss: {string.Join(", ", metrics.PerClassLogLoss.Select((l, i) => $"Class{i}:{l:F4}"))}";
            Console.WriteLine(perClassLogLossMsg);
        }
        Console.WriteLine("=".PadRight(60, '='));
        
        System.Diagnostics.Debug.WriteLine(accuracyMsg);
        System.Diagnostics.Debug.WriteLine(logLossMsg);

        // Save the model if path is provided
        if (!string.IsNullOrEmpty(modelPath))
        {
            _mlContext.Model.Save(model, fullView.Schema, modelPath);
            Console.WriteLine($"Model saved to: {modelPath}");
        }

        return model;
    }

    /// <summary>
    /// Creates a prediction engine from a trained model.
    /// </summary>
    public PredictionEngine<EmotionData, EmotionPrediction> CreatePredictionEngine(ITransformer model)
    {
        return _mlContext.Model.CreatePredictionEngine<EmotionData, EmotionPrediction>(model);
    }

    /// <summary>
    /// Loads a model from file.
    /// </summary>
    public ITransformer LoadModel(string modelPath)
    {
        DataViewSchema modelSchema;
        return _mlContext.Model.Load(modelPath, out modelSchema);
    }

    /// <summary>
    /// Converts prediction label to EmotionType.
    /// </summary>
    public static EmotionType ParseEmotion(string? label)
    {
        if (string.IsNullOrEmpty(label))
            return EmotionType.Neutral;

        return label.ToLower() switch
        {
            "happy" or "joy" or "happiness" => EmotionType.Happy,
            "sad" or "sadness" => EmotionType.Sad,
            "angry" or "anger" => EmotionType.Angry,
            "anxious" or "anxiety" or "worried" => EmotionType.Anxious,
            "calm" or "peaceful" or "relaxed" => EmotionType.Calm,
            "excited" or "excitement" => EmotionType.Excited,
            "frustrated" or "frustration" => EmotionType.Frustrated,
            _ => EmotionType.Neutral
        };
    }
}

