using Microsoft.ML.Data;

namespace NeuroSync.ML;

/// <summary>
/// Input data model for emotion classification.
/// </summary>
public class EmotionData
{
    [LoadColumn(0)]
    public string Text { get; set; } = string.Empty;

    [LoadColumn(1)]
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Prediction result from the ML model.
/// </summary>
public class EmotionPrediction
{
    [ColumnName("PredictedLabel")]
    public string? PredictedLabel { get; set; }

    [ColumnName("Score")]
    public float[]? Score { get; set; }
}


