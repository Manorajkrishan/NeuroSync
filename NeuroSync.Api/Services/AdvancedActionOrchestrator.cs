using NeuroSync.Core;
using NeuroSync.IoT;
using Microsoft.Extensions.Logging;

namespace NeuroSync.Api.Services;

/// <summary>
/// Advanced Action Orchestrator for intelligent multi-device coordination
/// Enhanced emotion-to-action intelligence with multi-layer awareness
/// </summary>
public class AdvancedActionOrchestrator
{
    private readonly ILogger<AdvancedActionOrchestrator> _logger;
    private readonly RealIoTController? _realIoTController;
    private readonly IoTDeviceSimulator _iotSimulator;
    private readonly DecisionEngine _decisionEngine;

    public AdvancedActionOrchestrator(
        ILogger<AdvancedActionOrchestrator> logger,
        RealIoTController? realIoTController,
        IoTDeviceSimulator iotSimulator,
        DecisionEngine decisionEngine)
    {
        _logger = logger;
        _realIoTController = realIoTController;
        _iotSimulator = iotSimulator;
        _decisionEngine = decisionEngine;
    }

    /// <summary>
    /// Orchestrate actions based on multi-layer emotion result
    /// </summary>
    public async Task<List<IoTAction>> OrchestrateActions(
        MultiLayerEmotionResult emotionResult,
        string? userId = null)
    {
        var actions = new List<IoTAction>();

        // Get base actions from decision engine
        var baseActions = await _decisionEngine.GetIoTActionsAsync(emotionResult.PrimaryEmotion);

        // Enhance actions based on multi-layer analysis
        var enhancedActions = EnhanceActions(baseActions, emotionResult);

        // Coordinate multi-device actions
        var coordinatedActions = CoordinateMultiDeviceActions(enhancedActions, emotionResult);

        // Prioritize and filter actions
        var prioritizedActions = PrioritizeActions(coordinatedActions, emotionResult);

        _logger.LogInformation(
            "Orchestrated {Count} actions for emotion: {Emotion}, Confidence: {Confidence:P2}",
            prioritizedActions.Count, emotionResult.PrimaryEmotion, emotionResult.OverallConfidence);

        return prioritizedActions;
    }

    /// <summary>
    /// Enhance actions based on multi-layer emotion analysis
    /// </summary>
    private List<IoTAction> EnhanceActions(List<IoTAction> baseActions, MultiLayerEmotionResult emotionResult)
    {
        var enhanced = new List<IoTAction>(baseActions);

        // Enhance based on visual layer (micro-expressions, eye behavior)
        if (emotionResult.VisualLayer?.EyeBehavior != null)
        {
            var eyeEmotion = emotionResult.VisualLayer.EyeBehavior.InferredEmotion;
            if (eyeEmotion == EmotionType.Anxious)
            {
                // Add calming actions
                enhanced.Add(CreateCalmingLightAction());
            }
        }

        // Enhance based on audio layer (tone stress, voice tremor)
        if (emotionResult.AudioLayer?.ToneStressScore > 0.7f || emotionResult.AudioLayer?.VoiceTremorScore > 0.7f)
        {
            // High stress detected - add stress relief actions
            enhanced.Add(CreateStressReliefMusicAction());
        }

        // Enhance based on biometric layer (heart rate, GSR)
        if (emotionResult.BiometricLayer?.HeartRate?.HeartRate > 100)
        {
            // High heart rate - add calming actions
            enhanced.Add(CreateCalmingAction());
        }

        // Enhance based on contextual layer (activity, task intensity)
        if (emotionResult.ContextualLayer?.TaskIntensity?.InferredEmotion == EmotionType.Frustrated)
        {
            // Task frustration detected - suggest break
            enhanced.Add(CreateBreakSuggestionAction());
        }

        return enhanced;
    }

    /// <summary>
    /// Coordinate multi-device actions
    /// </summary>
    private List<IoTAction> CoordinateMultiDeviceActions(List<IoTAction> actions, MultiLayerEmotionResult emotionResult)
    {
        var coordinated = new List<IoTAction>(actions);

        // Synchronize light and music for emotional consistency
        var lightAction = actions.FirstOrDefault(a => a.ActionType == "setColor");
        var musicAction = actions.FirstOrDefault(a => a.ActionType == "playMusic");

        if (lightAction != null && musicAction != null)
        {
            // Ensure light color matches music mood
            var emotion = emotionResult.PrimaryEmotion;
            var color = GetColorForEmotion(emotion);
            if (lightAction.Parameters != null && !lightAction.Parameters.ContainsKey("color"))
            {
                lightAction.Parameters["color"] = color;
            }
        }

        // Add multi-device synchronization actions
        if (actions.Count > 1)
        {
            // Add synchronization delay to ensure devices respond together
            foreach (var action in coordinated.Skip(1))
            {
                if (action.Parameters != null)
                {
                    action.Parameters["syncDelay"] = "0.2"; // 200ms delay for synchronization
                }
            }
        }

        return coordinated;
    }

    /// <summary>
    /// Prioritize and filter actions
    /// </summary>
    private List<IoTAction> PrioritizeActions(List<IoTAction> actions, MultiLayerEmotionResult emotionResult)
    {
        // Remove duplicates
        var unique = actions
            .GroupBy(a => new { a.DeviceId, a.ActionType })
            .Select(g => g.First())
            .ToList();

        // Prioritize by emotion confidence
        var prioritized = unique.OrderByDescending(a =>
        {
            // Higher confidence = higher priority
            return emotionResult.OverallConfidence;
        }).ToList();

        // Limit to top 5 actions to avoid overwhelming
        return prioritized.Take(5).ToList();
    }

    /// <summary>
    /// Create calming light action
    /// </summary>
    private IoTAction CreateCalmingLightAction()
    {
        return new IoTAction
        {
            DeviceId = "light-1",
            ActionType = "setColor",
            Parameters = new Dictionary<string, object>
            {
                { "color", "soft_blue" },
                { "brightness", 30 }
            }
        };
    }

    /// <summary>
    /// Create stress relief music action
    /// </summary>
    private IoTAction CreateStressReliefMusicAction()
    {
        return new IoTAction
        {
            DeviceId = "speaker",
            ActionType = "playMusic",
            Parameters = new Dictionary<string, object>
            {
                { "genre", "meditation" },
                { "playlist", "Stress Relief" },
                { "volume", 40 }
            }
        };
    }

    /// <summary>
    /// Create calming action
    /// </summary>
    private IoTAction CreateCalmingAction()
    {
        return new IoTAction
        {
            DeviceId = "notification",
            ActionType = "showMessage",
            Parameters = new Dictionary<string, object>
            {
                { "message", "Take a moment to breathe deeply. Your heart rate suggests you might benefit from a short break." }
            }
        };
    }

    /// <summary>
    /// Create break suggestion action
    /// </summary>
    private IoTAction CreateBreakSuggestionAction()
    {
        return new IoTAction
        {
            DeviceId = "notification",
            ActionType = "showMessage",
            Parameters = new Dictionary<string, object>
            {
                { "message", "Consider taking a short break. High task intensity can lead to frustration." }
            }
        };
    }

    /// <summary>
    /// Get color for emotion
    /// </summary>
    private string GetColorForEmotion(EmotionType emotion)
    {
        return emotion switch
        {
            EmotionType.Happy => "warm_yellow",
            EmotionType.Sad => "soft_blue",
            EmotionType.Anxious => "calm_green",
            EmotionType.Calm => "soft_purple",
            EmotionType.Excited => "bright_orange",
            EmotionType.Frustrated => "cool_blue",
            EmotionType.Angry => "deep_red",
            _ => "neutral_white"
        };
    }
}
