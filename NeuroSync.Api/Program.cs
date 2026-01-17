using Microsoft.AspNetCore.SignalR;
using Microsoft.ML;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using NeuroSync.Api.Hubs;
using NeuroSync.Api.Services;
using NeuroSync.Api.Data;
using NeuroSync.IoT;
using NeuroSync.IoT.Configuration;
using NeuroSync.IoT.Interfaces;
using NeuroSync.IoT.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework Core (Human OS v2.0 Database)
// Using InMemory for development - switch to SQL Server for production
builder.Services.AddDbContext<NeuroSyncDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connectionString))
    {
        // Use SQL Server if connection string is provided
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.MigrationsAssembly("NeuroSync.Api");
        });
    }
    else
    {
        // Fallback to InMemory for development
        options.UseInMemoryDatabase("NeuroSyncHumanOS");
    }
});

// Configure CORS for SignalR and API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add SignalR
builder.Services.AddSignalR();

// Add ML Model Service (singleton to load model once)
builder.Services.AddSingleton<ModelService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ModelService>>();
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    return new ModelService(logger, environment);
});

// Add Prediction Cache for faster responses
builder.Services.AddSingleton<PredictionCache>();

// Add Emotion Detection Service (uses the loaded model with caching)
builder.Services.AddSingleton<EmotionDetectionService>(sp =>
{
    try
    {
        var logger = sp.GetRequiredService<ILogger<EmotionDetectionService>>();
        logger.LogInformation("Initializing Emotion Detection Service...");
        
        var modelService = sp.GetRequiredService<ModelService>();
        logger.LogInformation("Loading or creating ML model...");
        
        var model = modelService.LoadOrCreateModel();
        logger.LogInformation("ML model loaded successfully");
        
        var cache = sp.GetService<PredictionCache>();
        logger.LogInformation("Prediction cache initialized for faster responses");
        
        return new EmotionDetectionService(model, logger, cache);
    }
    catch (Exception ex)
    {
        var logger = sp.GetRequiredService<ILogger<EmotionDetectionService>>();
        logger.LogError(ex, "CRITICAL: Failed to initialize Emotion Detection Service: {Message}", ex.Message);
        logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
        throw; // Re-throw to prevent silent failure
    }
});

// Load IoT Configuration
var iotConfig = builder.Configuration.GetSection("IoT").Get<IoTConfig>() ?? new IoTConfig();

// Add HttpClient for API calls
builder.Services.AddHttpClient();

// Register Spotify Music Service (if configured)
if (!string.IsNullOrEmpty(iotConfig.Spotify?.ClientId) || !string.IsNullOrEmpty(iotConfig.Spotify?.AccessToken))
{
    builder.Services.AddSingleton<IMusicService>(sp =>
    {
        var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
        var logger = sp.GetRequiredService<ILogger<SpotifyMusicService>>();
        var logAction = new Action<string>(msg => logger.LogInformation(msg));
        return new SpotifyMusicService(
            httpClient,
            iotConfig.Spotify?.AccessToken,
            iotConfig.Spotify?.RefreshToken,
            iotConfig.Spotify?.ClientId,
            iotConfig.Spotify?.ClientSecret,
            logAction);
    });
}

// Register YouTube Music Service (if configured)
if (!string.IsNullOrEmpty(iotConfig.YouTubeMusic?.AccessToken) || !string.IsNullOrEmpty(iotConfig.YouTubeMusic?.ApiKey))
{
    builder.Services.AddSingleton<IMusicService>(sp =>
    {
        var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
        var logger = sp.GetRequiredService<ILogger<YouTubeMusicService>>();
        var logAction = new Action<string>(msg => logger.LogInformation(msg));
        return new YouTubeMusicService(
            httpClient,
            iotConfig.YouTubeMusic?.ApiKey,
            iotConfig.YouTubeMusic?.AccessToken,
            logAction);
    });
}

// Register Music Service Manager
builder.Services.AddSingleton<MusicServiceManager>(sp =>
{
    var services = sp.GetServices<IMusicService>();
    var logger = sp.GetRequiredService<ILogger<MusicServiceManager>>();
    var logAction = new Action<string>(msg => logger.LogInformation(msg));
    return new MusicServiceManager(services, logAction);
});

// Register Smart Light Service
builder.Services.AddSingleton<SmartLightService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var logger = sp.GetRequiredService<ILogger<SmartLightService>>();
    var logAction = new Action<string>(msg => logger.LogInformation(msg));
    return new SmartLightService(httpClient, iotConfig, logAction);
});

// Register Real IoT Controller
builder.Services.AddSingleton<RealIoTController>(sp =>
{
    var musicManager = sp.GetService<MusicServiceManager>();
    var lightService = sp.GetService<SmartLightService>();
    var logger = sp.GetRequiredService<ILogger<RealIoTController>>();
    var logAction = new Action<string>(msg => logger.LogInformation(msg));
    return new RealIoTController(musicManager, lightService, iotConfig, logAction);
});

// Add IoT Simulator (fallback)
builder.Services.AddSingleton<IoTDeviceSimulator>();

// Add Conversation Memory (singleton to maintain state across requests)
builder.Services.AddSingleton<ConversationMemory>();

// Add Emotional Intelligence
builder.Services.AddSingleton<EmotionalIntelligence>();

// Add Voice Note Service
builder.Services.AddSingleton<VoiceNoteService>();

// Add Voice Service (TTS and Voice Cloning)
builder.Services.AddSingleton<VoiceService>();

// Add User Profile Service (Baby Learning System)
builder.Services.AddSingleton<UserProfileService>();

// Add Person Memory
builder.Services.AddSingleton<PersonMemory>();

// Add Action Executor
builder.Services.AddScoped<ActionExecutor>();

// Add Real-World Data Collector for continuous learning
builder.Services.AddSingleton<RealWorldDataCollector>();

// Add Auto-Retraining Service (background service for self-learning)
builder.Services.AddHostedService<AutoRetrainingService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AutoRetrainingService>>();
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    var dataCollector = sp.GetRequiredService<RealWorldDataCollector>();
    return new AutoRetrainingService(logger, sp, environment, dataCollector);
});

// Add Multi-Layer Emotion Fusion Service
builder.Services.AddSingleton<MultiLayerEmotionFusionService>();

// Add Advanced Audio Analysis Service
builder.Services.AddSingleton<AdvancedAudioAnalysisService>();

// Add Biometric Integration Service
builder.Services.AddSingleton<BiometricIntegrationService>();

// Add Contextual Awareness Service
builder.Services.AddSingleton<ContextualAwarenessService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ContextualAwarenessService>>();
    var conversationMemory = sp.GetService<ConversationMemory>();
    var userProfileService = sp.GetService<UserProfileService>();
    return new ContextualAwarenessService(logger, conversationMemory, userProfileService);
});

// Add Ethical AI Framework Service
builder.Services.AddSingleton<EthicalAIFrameworkService>();

// Add Decision Engine
builder.Services.AddScoped<DecisionEngine>(sp =>
{
    var iotSimulator = sp.GetRequiredService<IoTDeviceSimulator>();
    var realIoTController = sp.GetService<RealIoTController>();
    var logger = sp.GetRequiredService<ILogger<DecisionEngine>>();
    var conversationMemory = sp.GetService<ConversationMemory>();
    var emotionalIntelligence = sp.GetService<EmotionalIntelligence>();
    return new DecisionEngine(iotSimulator, realIoTController, logger, conversationMemory, emotionalIntelligence);
});

// Add Advanced Action Orchestrator
builder.Services.AddScoped<AdvancedActionOrchestrator>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AdvancedActionOrchestrator>>();
    var realIoTController = sp.GetService<RealIoTController>();
    var iotSimulator = sp.GetRequiredService<IoTDeviceSimulator>();
    var decisionEngine = sp.GetRequiredService<DecisionEngine>();
    return new AdvancedActionOrchestrator(logger, realIoTController, iotSimulator, decisionEngine);
});

// Human OS v2.0 Services
builder.Services.AddScoped<EmotionalOSDashboardService>(sp =>
{
    var context = sp.GetRequiredService<NeuroSyncDbContext>();
    var logger = sp.GetRequiredService<ILogger<EmotionalOSDashboardService>>();
    var emotionDetection = sp.GetRequiredService<EmotionDetectionService>();
    var collapsePredictor = sp.GetService<ICollapseRiskPredictor>();
    return new EmotionalOSDashboardService(context, logger, emotionDetection, collapsePredictor);
});

builder.Services.AddScoped<LifeDomainsEngineService>(sp =>
{
    var context = sp.GetRequiredService<NeuroSyncDbContext>();
    var logger = sp.GetRequiredService<ILogger<LifeDomainsEngineService>>();
    return new LifeDomainsEngineService(context, logger);
});

// Phase 2 Services
builder.Services.AddScoped<DecisionIntelligenceEngineService>(sp =>
{
    var context = sp.GetRequiredService<NeuroSyncDbContext>();
    var logger = sp.GetRequiredService<ILogger<DecisionIntelligenceEngineService>>();
    return new DecisionIntelligenceEngineService(context, logger);
});

builder.Services.AddScoped<CollapseRiskPredictorService>(sp =>
{
    var context = sp.GetRequiredService<NeuroSyncDbContext>();
    var logger = sp.GetRequiredService<ILogger<CollapseRiskPredictorService>>();
    var dashboardService = sp.GetService<EmotionalOSDashboardService>();
    return new CollapseRiskPredictorService(context, logger, dashboardService);
});

// Register as ICollapseRiskPredictor for dependency injection
builder.Services.AddScoped<ICollapseRiskPredictor>(sp => sp.GetRequiredService<CollapseRiskPredictorService>());

// Phase 3 Services
builder.Services.AddScoped<IdentityPurposeEngineService>(sp =>
{
    var context = sp.GetRequiredService<NeuroSyncDbContext>();
    var logger = sp.GetRequiredService<ILogger<IdentityPurposeEngineService>>();
    return new IdentityPurposeEngineService(context, logger);
});

builder.Services.AddScoped<LifeMemoryGraphService>(sp =>
{
    var context = sp.GetRequiredService<NeuroSyncDbContext>();
    var logger = sp.GetRequiredService<ILogger<LifeMemoryGraphService>>();
    return new LifeMemoryGraphService(context, logger);
});

builder.Services.AddScoped<EmotionalGrowthAnalyticsService>(sp =>
{
    var context = sp.GetRequiredService<NeuroSyncDbContext>();
    var logger = sp.GetRequiredService<ILogger<EmotionalGrowthAnalyticsService>>();
    return new EmotionalGrowthAnalyticsService(context, logger);
});

// Phase 4 Service
builder.Services.AddScoped<TrustSafetyLayerService>(sp =>
{
    var context = sp.GetRequiredService<NeuroSyncDbContext>();
    var logger = sp.GetRequiredService<ILogger<TrustSafetyLayerService>>();
    return new TrustSafetyLayerService(context, logger);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Only use HTTPS redirection if HTTPS is available
if (app.Environment.IsDevelopment())
{
    // In development, allow HTTP without redirect warning
    // app.UseHttpsRedirection(); // Commented out to avoid warning when only HTTP is configured
}

// Use CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<EmotionHub>("/emotionHub");

// Serve static files (for frontend)
app.UseStaticFiles();

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();