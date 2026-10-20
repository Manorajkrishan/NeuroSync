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
        var dataCount = trainingData.Count;
        Console.WriteLine($"Training model with {dataCount:N0} examples...");

        // Convert list to IDataView
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        // Split data into training and testing sets (80/20)
        var dataSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        Console.WriteLine($"Training set: {dataSplit.TrainSet.GetRowCount():N0} examples");
        Console.WriteLine($"Test set: {dataSplit.TestSet.GetRowCount():N0} examples");

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
        var trainedModel = trainingPipeline.Fit(dataSplit.TrainSet);

        // Evaluate the model BEFORE converting keys to values
        var predictions = trainedModel.Transform(dataSplit.TestSet);
        var metrics = _mlContext.MulticlassClassification.Evaluate(
            predictions,
            labelColumnName: "Label",
            scoreColumnName: "Score",
            predictedLabelColumnName: "PredictedLabel");

        // Now add the MapKeyToValue for the final model (for prediction)
        var finalPipeline = trainingPipeline
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel", "PredictedLabel"));
        
        // Retrain with the full pipeline for production use
        var model = finalPipeline.Fit(dataSplit.TrainSet);

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
            _mlContext.Model.Save(model, dataView.Schema, modelPath);
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

