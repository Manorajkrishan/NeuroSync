using NeuroSync.Core;
using NeuroSync.ML;
using System.Collections.Concurrent;

namespace NeuroSync.Api.Services;

/// <summary>
/// Collects real-world emotion data from user interactions for continuous learning.
/// </summary>
public class RealWorldDataCollector
{
    private readonly ConcurrentQueue<EmotionData> _collectedData = new();
    private readonly ILogger<RealWorldDataCollector> _logger;
    private readonly string _dataFilePath;
    private const int MaxInMemoryData = 1000;
    private const int FlushThreshold = 100;

    public RealWorldDataCollector(ILogger<RealWorldDataCollector> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        var dataDir = Path.Combine(environment.ContentRootPath, "Data");
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }
        _dataFilePath = Path.Combine(dataDir, "realworld_emotions.csv");
    }

    /// <summary>
    /// Collects emotion data from a real user interaction.
    /// </summary>
    public void CollectData(string userText, EmotionType detectedEmotion, float confidence)
    {
        // Only collect high-confidence predictions (likely correct)
        if (confidence < 0.7f)
        {
            return;
        }

        // Normalize text
        var normalizedText = userText.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(normalizedText) || normalizedText.Length < 3)
        {
            return;
        }

        // Skip if too similar to existing data (simple check)
        if (_collectedData.Any(d => d.Text.Equals(normalizedText, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var emotionData = new EmotionData
        {
            Text = normalizedText,
            Label = detectedEmotion.ToString().ToLower()
        };

        _collectedData.Enqueue(emotionData);

        // Flush to file periodically
        if (_collectedData.Count >= FlushThreshold)
        {
            FlushToFile();
        }

        // Limit in-memory data
        while (_collectedData.Count > MaxInMemoryData)
        {
            _collectedData.TryDequeue(out _);
        }

        _logger.LogDebug($"Collected real-world data: {normalizedText} -> {detectedEmotion}");
    }

    /// <summary>
    /// Flushes collected data to CSV file.
    /// </summary>
    public void FlushToFile()
    {
        if (_collectedData.IsEmpty)
        {
            return;
        }

        List<EmotionData> dataToWrite = new List<EmotionData>();
        try
        {
            while (_collectedData.TryDequeue(out var item))
            {
                dataToWrite.Add(item);
            }

            if (dataToWrite.Count == 0)
            {
                return;
            }

            var lines = new List<string>();
            if (!File.Exists(_dataFilePath))
            {
                lines.Add("Text,Label"); // Header
            }

            foreach (var item in dataToWrite)
            {
                var escapedText = item.Text.Replace("\"", "\"\""); // Escape quotes
                lines.Add($"\"{escapedText}\",{item.Label}");
            }

            File.AppendAllLines(_dataFilePath, lines);
            _logger.LogInformation($"Flushed {dataToWrite.Count} real-world data entries to {_dataFilePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing real-world data to file");
            // Re-queue items on error
            foreach (var item in dataToWrite)
            {
                _collectedData.Enqueue(item);
            }
        }
    }

    /// <summary>
    /// Gets all collected real-world data.
    /// </summary>
    public List<EmotionData> GetCollectedData()
    {
        return _collectedData.ToList();
    }

    /// <summary>
    /// Loads real-world data from file.
    /// </summary>
    public List<EmotionData> LoadFromFile()
    {
        if (!File.Exists(_dataFilePath))
        {
            return new List<EmotionData>();
        }

        try
        {
            var lines = File.ReadAllLines(_dataFilePath);
            var data = new List<EmotionData>();

            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                // Simple CSV parsing
                var parts = line.Split(',');
                if (parts.Length >= 2)
                {
                    var text = parts[0].Trim('"').Replace("\"\"", "\"");
                    var label = parts[1].Trim().ToLower();

                    if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(label))
                    {
                        data.Add(new EmotionData { Text = text, Label = label });
                    }
                }
            }

            _logger.LogInformation($"Loaded {data.Count} real-world data entries from file");
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading real-world data from file");
            return new List<EmotionData>();
        }
    }
}

