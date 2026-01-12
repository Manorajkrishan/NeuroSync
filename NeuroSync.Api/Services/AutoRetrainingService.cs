using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeuroSync.ML;
using System.Collections.Concurrent;

namespace NeuroSync.Api.Services;

/// <summary>
/// Automatically retrains the model when enough new real-world data is collected.
/// Runs in the background to continuously improve the system.
/// </summary>
public class AutoRetrainingService : BackgroundService
{
    private readonly ILogger<AutoRetrainingService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWebHostEnvironment _environment;
    private readonly RealWorldDataCollector _dataCollector;
    private readonly SemaphoreSlim _retrainingLock = new(1, 1);
    
    // Retrain when we have this many new examples
    private const int RetrainThreshold = 50;
    private const int CheckIntervalMinutes = 5; // Check every 5 minutes
    private int _lastDataCount = 0;
    private DateTime _lastRetrainTime = DateTime.MinValue;
    private const int MinRetrainIntervalMinutes = 30; // Don't retrain more than once per 30 minutes

    public AutoRetrainingService(
        ILogger<AutoRetrainingService> logger,
        IServiceProvider serviceProvider,
        IWebHostEnvironment environment,
        RealWorldDataCollector dataCollector)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _environment = environment;
        _dataCollector = dataCollector;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auto-retraining service started. Will check for new data every {Interval} minutes", CheckIntervalMinutes);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndRetrainIfNeeded(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in auto-retraining service");
            }

            // Wait before next check
            await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes), stoppingToken);
        }
    }

    private async Task CheckAndRetrainIfNeeded(CancellationToken cancellationToken)
    {
        // Check if enough time has passed since last retrain
        if ((DateTime.UtcNow - _lastRetrainTime).TotalMinutes < MinRetrainIntervalMinutes)
        {
            return;
        }

        // Get current data count
        var currentData = _dataCollector.LoadFromFile();
        var currentCount = currentData.Count;
        var newDataCount = currentCount - _lastDataCount;

        _logger.LogDebug($"Checking for retraining: {newDataCount} new examples since last check (total: {currentCount})");

        // Retrain if we have enough new data
        if (newDataCount >= RetrainThreshold)
        {
            _logger.LogInformation($"Found {newDataCount} new examples (threshold: {RetrainThreshold}). Starting automatic retraining...");
            
            await RetrainModelAsync(cancellationToken);
            
            _lastDataCount = currentCount;
            _lastRetrainTime = DateTime.UtcNow;
        }
        else if (_lastDataCount == 0)
        {
            // Initialize on first check
            _lastDataCount = currentCount;
        }
    }

    private async Task RetrainModelAsync(CancellationToken cancellationToken)
    {
        // Prevent concurrent retraining
        if (!await _retrainingLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogWarning("Retraining already in progress, skipping...");
            return;
        }

        try
        {
            _logger.LogInformation("🔄 Starting automatic model retraining with new real-world data...");
            
            // Run retraining in background thread to avoid blocking
            await Task.Run(() =>
            {
                try
                {
                    var modelPath = Path.Combine(_environment.ContentRootPath, "Models", "emotion-model.zip");
                    var trainer = new EmotionModelTrainer();
                    
                    // Load all training data
                    var trainingData = new List<EmotionData>();
                    
                    // Load base comprehensive data
                    trainingData.AddRange(TrainingDataGenerator.GenerateComprehensiveData());
                    _logger.LogInformation($"Loaded {trainingData.Count} base training examples");
                    
                    // Load real-world collected data
                    var realWorldData = _dataCollector.LoadFromFile();
                    if (realWorldData.Count > 0)
                    {
                        trainingData.AddRange(realWorldData);
                        _logger.LogInformation($"Added {realWorldData.Count} real-world examples");
                    }
                    
                    // Try to load external dataset
                    var dataDir = Path.Combine(_environment.ContentRootPath, "Data");
                    var datasetPath = Path.Combine(dataDir, "emotions.csv");
                    if (File.Exists(datasetPath))
                    {
                        try
                        {
                            var externalData = DatasetLoader.LoadFromFile(datasetPath);
                            trainingData.AddRange(externalData);
                            _logger.LogInformation($"Added {externalData.Count} examples from external dataset");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to load external dataset");
                        }
                    }
                    
                    _logger.LogInformation($"Training model with {trainingData.Count} total examples...");
                    
                    // Train new model
                    var model = trainer.TrainModel(trainingData, modelPath);
                    
                    _logger.LogInformation("✅ Model retraining completed successfully!");
                    _logger.LogInformation($"New model saved to: {modelPath}");
                    
                    // Reload model in services (this will happen on next request or we can trigger it)
                    _logger.LogInformation("⚠️ Note: Restart the application to use the newly trained model");
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during automatic retraining");
                }
            });
        }
        finally
        {
            _retrainingLock.Release();
        }
    }

    /// <summary>
    /// Manually trigger retraining (for testing or immediate retraining)
    /// </summary>
    public async Task TriggerRetrainAsync()
    {
        await RetrainModelAsync(CancellationToken.None);
    }

    public override void Dispose()
    {
        _retrainingLock?.Dispose();
        base.Dispose();
    }
}

