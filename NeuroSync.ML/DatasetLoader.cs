using Microsoft.ML.Data;

namespace NeuroSync.ML;

/// <summary>
/// Loads training data from various sources (CSV, TSV, or in-memory).
/// </summary>
public class DatasetLoader
{
    /// <summary>
    /// Loads training data from a CSV/TSV file.
    /// Expected format: Text,Label (or Text\tLabel for TSV)
    /// </summary>
    public static List<EmotionData> LoadFromFile(string filePath)
    {
        var data = new List<EmotionData>();
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Dataset file not found: {filePath}");
        }

        var lines = File.ReadAllLines(filePath);
        bool isTsv = filePath.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase);
        char separator = isTsv ? '\t' : ',';

        // Skip header if present
        int startIndex = 0;
        if (lines.Length > 0 && (lines[0].StartsWith("Text", StringComparison.OrdinalIgnoreCase) || 
                                  lines[0].StartsWith("Label", StringComparison.OrdinalIgnoreCase)))
        {
            startIndex = 1;
        }

        for (int i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var parts = line.Split(separator);
            if (parts.Length >= 2)
            {
                data.Add(new EmotionData
                {
                    Text = parts[0].Trim().Trim('"'),
                    Label = parts[1].Trim().Trim('"').ToLower()
                });
            }
        }

        return data;
    }

    /// <summary>
    /// Combines multiple data sources.
    /// </summary>
    public static List<EmotionData> CombineDataSources(params List<EmotionData>[] dataSources)
    {
        var combined = new List<EmotionData>();
        foreach (var source in dataSources)
        {
            combined.AddRange(source);
        }
        return combined;
    }
}

