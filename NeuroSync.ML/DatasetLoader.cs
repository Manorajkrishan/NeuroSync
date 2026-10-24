using System.Text;

namespace NeuroSync.ML;

/// <summary>
/// Loads training data from CSV/TSV files.
/// </summary>
public class DatasetLoader
{
    /// <summary>
    /// Loads training data from a CSV/TSV file.
    /// Expected format: Text,Label (or Text\tLabel for TSV).
    /// CSV parsing supports quoted commas and escaped quotes.
    /// </summary>
    public static List<EmotionData> LoadFromFile(string filePath)
    {
        var data = new List<EmotionData>();

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Dataset file not found: {filePath}");

        var lines = File.ReadAllLines(filePath);
        var isTsv = filePath.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase);

        var startIndex = 0;
        if (lines.Length > 0)
        {
            var first = lines[0].TrimStart('\uFEFF');
            if (first.StartsWith("Text", StringComparison.OrdinalIgnoreCase) ||
                first.StartsWith("Label", StringComparison.OrdinalIgnoreCase))
                startIndex = 1;
        }

        for (var i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts;
            if (isTsv)
            {
                parts = line.Split('\t');
            }
            else
            {
                parts = ParseCsvLine(line).ToArray();
            }

            if (parts.Length < 2)
                continue;

            var text = parts[0].Trim();
            var label = parts[1].Trim().ToLowerInvariant();
            if (text.Length == 0 || label.Length == 0)
                continue;

            data.Add(new EmotionData { Text = text, Label = label });
        }

        return data;
    }

    public static List<EmotionData> CombineDataSources(params List<EmotionData>[] dataSources)
    {
        var combined = new List<EmotionData>();
        foreach (var source in dataSources)
            combined.AddRange(source);
        return combined;
    }

    private static IEnumerable<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        fields.Add(current.ToString());
        return fields;
    }
}
