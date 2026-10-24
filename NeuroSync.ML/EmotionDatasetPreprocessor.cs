using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NeuroSync.ML;

public sealed record DatasetPreparationResult(
    IReadOnlyList<EmotionData> Examples,
    int InputRows,
    int DuplicateRowsRemoved,
    int ConflictingTextsRemoved,
    IReadOnlyDictionary<string, int> LabelCounts);

/// <summary>
/// Cleans emotion training data before model fitting.
/// Removes exact/normalized duplicates and drops texts that carry conflicting labels.
/// </summary>
public static class EmotionDatasetPreprocessor
{
    private static readonly HashSet<string> AllowedLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        "happy", "sad", "angry", "anxious", "calm", "excited", "frustrated", "neutral"
    };

    public static DatasetPreparationResult Prepare(IEnumerable<EmotionData> source)
    {
        var rows = source
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Text) && !string.IsNullOrWhiteSpace(x.Label))
            .Select(x => new EmotionData
            {
                Text = CleanDisplayText(x.Text),
                Label = x.Label.Trim().ToLowerInvariant()
            })
            .Where(x => AllowedLabels.Contains(x.Label))
            .ToList();

        var groups = rows
            .GroupBy(x => NormalizeText(x.Text), StringComparer.Ordinal)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .ToList();

        var prepared = new List<EmotionData>(groups.Count);
        var duplicateRowsRemoved = 0;
        var conflictingTextsRemoved = 0;

        foreach (var group in groups)
        {
            var labels = group.Select(x => x.Label).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (labels.Count != 1)
            {
                conflictingTextsRemoved++;
                duplicateRowsRemoved += group.Count();
                continue;
            }

            var first = group.First();
            prepared.Add(new EmotionData { Text = first.Text, Label = labels[0] });
            duplicateRowsRemoved += Math.Max(0, group.Count() - 1);
        }

        var labelCounts = prepared
            .GroupBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);

        return new DatasetPreparationResult(
            prepared,
            rows.Count,
            duplicateRowsRemoved,
            conflictingTextsRemoved,
            labelCounts);
    }

    public static (List<EmotionData> Train, List<EmotionData> Test) DeterministicStratifiedSplit(
        IReadOnlyList<EmotionData> prepared,
        double testFraction = 0.2)
    {
        if (prepared.Count < 2)
            throw new InvalidOperationException("At least two prepared examples are required.");

        testFraction = Math.Clamp(testFraction, 0.05, 0.4);
        var train = new List<EmotionData>();
        var test = new List<EmotionData>();

        foreach (var labelGroup in prepared.GroupBy(x => x.Label, StringComparer.OrdinalIgnoreCase))
        {
            var ordered = labelGroup
                .OrderBy(x => StableKey(NormalizeText(x.Text)), StringComparer.Ordinal)
                .ToList();

            if (ordered.Count == 1)
            {
                train.Add(ordered[0]);
                continue;
            }

            var testCount = Math.Clamp((int)Math.Round(ordered.Count * testFraction), 1, ordered.Count - 1);
            test.AddRange(ordered.Take(testCount));
            train.AddRange(ordered.Skip(testCount));
        }

        return (train, test);
    }

    public static string NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^\p{L}\p{N}\s]+", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
        return normalized;
    }

    private static string CleanDisplayText(string text) =>
        Regex.Replace(text.Trim(), @"\s+", " ");

    private static string StableKey(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
