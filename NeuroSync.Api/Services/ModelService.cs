using Microsoft.ML;
using Microsoft.Extensions.Logging;
using NeuroSync.ML;

namespace NeuroSync.Api.Services;

/// <summary>
/// Service for managing the ML model.
/// </summary>
public class ModelService
{
    private readonly ILogger<ModelService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _modelPath;

    public ModelService(ILogger<ModelService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
        _modelPath = Path.Combine(environment.ContentRootPath, "Models", "emotion-model.zip");
    }

    /// <summary>
    /// Loads or creates the ML model.
    /// </summary>
    public ITransformer LoadOrCreateModel()
    {
        var trainer = new EmotionModelTrainer();

        // Try to load existing model
        if (File.Exists(_modelPath))
        {
            try
            {
                _logger.LogInformation($"Loading model from: {_modelPath}");
                return trainer.LoadModel(_modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load existing model, creating new one");
            }
        }

        // Try to load from external dataset file first
        var contentRoot = Path.GetDirectoryName(_modelPath) ?? "";
        var dataDir = Path.Combine(contentRoot, "..", "Data");
        var datasetPath = Path.Combine(dataDir, "emotions.csv");
        // Normalize the path
        datasetPath = Path.GetFullPath(datasetPath);
        List<EmotionData> trainingData;

        if (File.Exists(datasetPath))
        {
            try
            {
                _logger.LogInformation($"Loading training data from: {datasetPath}");
                trainingData = DatasetLoader.LoadFromFile(datasetPath);
                _logger.LogInformation($"Loaded {trainingData.Count} examples from dataset file");
                
                // Also load real-world collected data
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var realWorldCollector = new RealWorldDataCollector(
                    loggerFactory.CreateLogger<RealWorldDataCollector>(),
                    _environment);
                var realWorldData = realWorldCollector.LoadFromFile();
                if (realWorldData.Count > 0)
                {
                    trainingData.AddRange(realWorldData);
                    _logger.LogInformation($"Added {realWorldData.Count} real-world examples from collected data");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load dataset file, using comprehensive data");
                trainingData = TrainingDataGenerator.GenerateComprehensiveData();
            }
        }
        else
        {
            // Use comprehensive data if no external dataset found
            _logger.LogInformation("No external dataset found. Using comprehensive training data (~1000+ examples).");
            _logger.LogInformation("Generating comprehensive training dataset with real-world scenarios...");
            trainingData = TrainingDataGenerator.GenerateComprehensiveData();
            _logger.LogInformation($"Generated {trainingData.Count} training examples");
            
            // Also load any real-world collected data
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var realWorldCollector = new RealWorldDataCollector(
                loggerFactory.CreateLogger<RealWorldDataCollector>(),
                _environment);
            var realWorldData = realWorldCollector.LoadFromFile();
            if (realWorldData.Count > 0)
            {
                trainingData.AddRange(realWorldData);
                _logger.LogInformation($"Added {realWorldData.Count} real-world examples from collected data");
            }
        }
        
        _logger.LogInformation($"Training model with {trainingData.Count} examples");
        
        // Ensure Models directory exists
        var modelDir = Path.GetDirectoryName(_modelPath);
        if (!string.IsNullOrEmpty(modelDir) && !Directory.Exists(modelDir))
        {
            Directory.CreateDirectory(modelDir);
        }

        var model = trainer.TrainModel(trainingData, _modelPath);
        _logger.LogInformation("Model training completed");
        
        return model;
    }
}

