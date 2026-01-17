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
    /// Trains the emotion classification model using the provided training data.
    /// Upgraded with: Word Embeddings (Priority 2), LightGbm (Priority 3), optimized for 10,000+ datasets (Priority 1).
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

        // PRIORITY 2: Build pipeline with Word Embeddings (FastText Sentiment-Specific)
        // This allows the model to understand word relationships (e.g., "happy" vs "joyful")
        var trainingPipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label", "Label")
            // Tokenize text into words
            .Append(_mlContext.Transforms.Text.TokenizeIntoWords("Words", "Text"))
            // PRIORITY 2: Apply Word Embeddings for better word understanding
            .Append(_mlContext.Transforms.Text.ApplyWordEmbedding(
                outputColumnName: "Features",
                inputColumnName: "Words",
                modelKind: WordEmbeddingEstimator.PretrainedModelKind.SentimentSpecificWordEmbedding))
            // PRIORITY 3: Use LightGbm instead of SDCA for better accuracy (best for text classification)
            .Append(_mlContext.MulticlassClassification.Trainers.LightGbm(
                labelColumnName: "Label",
                featureColumnName: "Features",
                numberOfIterations: dataCount > 5000 ? 200 : 100, // More iterations for larger datasets
                numberOfLeaves: dataCount > 5000 ? 63 : 31, // More leaves for larger datasets
                minimumExampleCountPerLeaf: dataCount > 5000 ? 20 : 10, // Adapt to dataset size
                learningRate: 0.3));

        // Train the model
        Console.WriteLine("Training started... This may take a few minutes for large datasets.");
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

