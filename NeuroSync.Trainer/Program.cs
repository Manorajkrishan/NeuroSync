using System.Text.Json;
using NeuroSync.Core;
using NeuroSync.ML;

static string? GetArg(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static string RequireArg(string[] args, string name) =>
    GetArg(args, name) ?? throw new ArgumentException($"Missing required argument {name}");

static string Label(EmotionType emotion) => emotion switch
{
    EmotionType.Happy => "happy",
    EmotionType.Sad => "sad",
    EmotionType.Angry => "angry",
    EmotionType.Anxious => "anxious",
    EmotionType.Calm => "calm",
    EmotionType.Excited => "excited",
    EmotionType.Frustrated => "frustrated",
    _ => "neutral"
};

var trainPath = RequireArg(args, "--train");
var devPath = RequireArg(args, "--dev");
var testPath = RequireArg(args, "--test");
var modelPath = RequireArg(args, "--model");
var reportPath = RequireArg(args, "--report");
var localPath = GetArg(args, "--local");
var minMacroF1 = double.TryParse(GetArg(args, "--min-macro-f1"), out var mf1) ? mf1 : 0.35;

var train = DatasetLoader.LoadFromFile(trainPath);
var dev = DatasetLoader.LoadFromFile(devPath);
var local = !string.IsNullOrWhiteSpace(localPath) && File.Exists(localPath)
    ? DatasetLoader.LoadFromFile(localPath)
    : new List<EmotionData>();

var combined = DatasetLoader.CombineDataSources(train, dev, local);
var preparedTraining = EmotionDatasetPreprocessor.Prepare(combined);

Console.WriteLine($"Prepared training rows: {preparedTraining.Examples.Count:N0} clean unique examples");
Console.WriteLine($"Removed duplicates: {preparedTraining.DuplicateRowsRemoved:N0}");
Console.WriteLine($"Removed conflicting texts: {preparedTraining.ConflictingTextsRemoved:N0}");

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(modelPath))!);
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))!);

var trainer = new EmotionModelTrainer();
var model = trainer.TrainModel(combined, modelPath);
var predictor = new EmotionPredictionService(model);

var testPrepared = EmotionDatasetPreprocessor.Prepare(DatasetLoader.LoadFromFile(testPath));
var trainingKeys = preparedTraining.Examples
    .Select(x => EmotionDatasetPreprocessor.NormalizeText(x.Text))
    .ToHashSet(StringComparer.Ordinal);

var heldOut = testPrepared.Examples
    .Where(x => !trainingKeys.Contains(EmotionDatasetPreprocessor.NormalizeText(x.Text)))
    .ToList();

if (heldOut.Count == 0)
    throw new InvalidOperationException("No held-out evaluation examples remained after leakage filtering.");

var labels = new[] { "happy", "sad", "angry", "anxious", "calm", "excited", "frustrated", "neutral" };
var confusion = labels.ToDictionary(
    expected => expected,
    expected => labels.ToDictionary(predicted => predicted, _ => 0, StringComparer.OrdinalIgnoreCase),
    StringComparer.OrdinalIgnoreCase);

var correct = 0;
var confidences = new List<double>(heldOut.Count);

foreach (var row in heldOut)
{
    var prediction = predictor.Predict(row.Text);
    var predicted = Label(prediction.Emotion);
    var expected = row.Label.Trim().ToLowerInvariant();

    if (!confusion.ContainsKey(expected))
        continue;

    confusion[expected][predicted]++;
    if (predicted == expected)
        correct++;
    confidences.Add(prediction.Confidence);
}

var perLabel = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
var f1Values = new List<double>();

foreach (var label in labels)
{
    var tp = confusion[label][label];
    var fp = labels.Where(x => x != label).Sum(x => confusion[x][label]);
    var fn = labels.Where(x => x != label).Sum(x => confusion[label][x]);
    var support = confusion[label].Values.Sum();

    var precision = tp + fp == 0 ? 0d : (double)tp / (tp + fp);
    var recall = tp + fn == 0 ? 0d : (double)tp / (tp + fn);
    var f1 = precision + recall == 0 ? 0d : 2 * precision * recall / (precision + recall);

    if (support > 0)
        f1Values.Add(f1);

    perLabel[label] = new
    {
        support,
        precision,
        recall,
        f1
    };
}

var accuracy = (double)correct / heldOut.Count;
var macroF1 = f1Values.Count == 0 ? 0d : f1Values.Average();
var meanConfidence = confidences.Count == 0 ? 0d : confidences.Average();

var report = new
{
    generatedAtUtc = DateTime.UtcNow,
    sources = new
    {
        goEmotionsTrainRows = train.Count,
        goEmotionsDevRows = dev.Count,
        localRows = local.Count,
        preparedTrainingRows = preparedTraining.Examples.Count,
        duplicatesRemoved = preparedTraining.DuplicateRowsRemoved,
        conflictingTextsRemoved = preparedTraining.ConflictingTextsRemoved,
        heldOutRows = heldOut.Count
    },
    metrics = new
    {
        accuracy,
        macroF1,
        meanConfidence
    },
    perLabel,
    confusion
};

File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine($"Held-out accuracy: {accuracy:P2}");
Console.WriteLine($"Held-out macro-F1: {macroF1:P2}");
Console.WriteLine($"Mean confidence: {meanConfidence:P2}");
Console.WriteLine($"Report: {reportPath}");
Console.WriteLine($"Model: {modelPath}");

if (macroF1 < minMacroF1)
{
    Console.Error.WriteLine($"Macro-F1 {macroF1:P2} is below required gate {minMacroF1:P2}.");
    return 2;
}

return 0;
