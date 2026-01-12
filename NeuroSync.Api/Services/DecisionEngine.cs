using NeuroSync.Core;
using NeuroSync.IoT;

namespace NeuroSync.Api.Services;

/// <summary>
/// Decision engine that generates adaptive responses based on detected emotions.
/// Enhanced with emotional intelligence and conversation memory.
/// </summary>
public class DecisionEngine
{
    private readonly IoTDeviceSimulator _iotSimulator;
    private readonly RealIoTController? _realIoTController;
    private readonly ILogger<DecisionEngine> _logger;
    private readonly ConversationMemory? _conversationMemory;
    private readonly EmotionalIntelligence? _emotionalIntelligence;

    public DecisionEngine(
        IoTDeviceSimulator iotSimulator, 
        RealIoTController? realIoTController, 
        ILogger<DecisionEngine> logger,
        ConversationMemory? conversationMemory = null,
        EmotionalIntelligence? emotionalIntelligence = null)
    {
        _iotSimulator = iotSimulator;
        _realIoTController = realIoTController;
        _logger = logger;
        _conversationMemory = conversationMemory;
        _emotionalIntelligence = emotionalIntelligence;
    }

    /// <summary>
    /// Processes an emotion and generates adaptive responses with emotional intelligence.
    /// </summary>
    public AdaptiveResponse GenerateResponse(EmotionResult emotionResult, string? userId = "default", string? userMessage = null)
    {
        // Get conversation context
        ConversationContext? context = null;
        if (_conversationMemory != null && !string.IsNullOrEmpty(userId))
        {
            context = _conversationMemory.GetOrCreateContext(userId);
        }

        // Generate empathetic message using emotional intelligence
        string message;
        if (_emotionalIntelligence != null)
        {
            message = _emotionalIntelligence.GenerateEmpatheticMessage(emotionResult.Emotion, context);
        }
        else
        {
            message = GetDefaultMessage(emotionResult.Emotion);
        }

        // Generate follow-up question
        string? followUpQuestion = null;
        if (_emotionalIntelligence != null)
        {
            followUpQuestion = _emotionalIntelligence.GenerateFollowUpQuestion(emotionResult.Emotion, userMessage ?? emotionResult.OriginalText, context);
        }

        // Generate encouragement if needed
        string? encouragement = null;
        if (_emotionalIntelligence != null && context != null)
        {
            encouragement = _emotionalIntelligence.GenerateEncouragement(context);
        }

        var response = new AdaptiveResponse
        {
            Emotion = emotionResult.Emotion,
            Message = message
        };

        // Add context-aware parameters
        var parameters = new Dictionary<string, object>();

        switch (emotionResult.Emotion)
        {
            case EmotionType.Happy:
                response.Action = "enhance_environment";
                parameters.Add("suggestion", "Would you like to save this moment or share your happiness?");
                parameters.Add("activities", new[] { "Listen to upbeat music", "Dance", "Call a friend", "Do something creative", "Take photos", "Write about this moment" });
                parameters.Add("music", "Upbeat and energetic playlists");
                break;

            case EmotionType.Sad:
                response.Action = "support_wellbeing";
                parameters.Add("suggestion", "Consider taking a break or listening to some calming music.");
                parameters.Add("activities", new[] { "Listen to soothing music", "Watch a favorite show", "Take a warm bath", "Talk to someone you trust", "Write in a journal", "Practice self-compassion", "Reach out to a friend" });
                parameters.Add("music", "Comforting and healing playlists");
                
                // Add special support for recurring sadness
                if (context != null && _conversationMemory != null && !string.IsNullOrEmpty(userId) && _conversationMemory.HasConcerningPattern(userId))
                {
                    parameters.Add("support_note", "I've noticed you've been feeling down more often. I'm here for you, and it's okay to ask for help.");
                }
                break;

            case EmotionType.Angry:
                response.Action = "calm_environment";
                parameters.Add("suggestion", "Deep breathing exercises might help. Would you like a guided session?");
                parameters.Add("activities", new[] { "Listen to meditation music", "Take a walk", "Practice deep breathing", "Do light exercise", "Step away for a moment", "Express your feelings safely" });
                parameters.Add("music", "Meditation and peaceful sounds");
                break;

            case EmotionType.Anxious:
                response.Action = "reduce_stress";
                parameters.Add("suggestion", "Try some mindfulness exercises or take a short walk.");
                parameters.Add("activities", new[] { "Listen to nature sounds", "Practice mindfulness", "Do gentle stretching", "Use breathing techniques", "Focus on one task at a time", "Ground yourself with the 5-4-3-2-1 technique" });
                parameters.Add("music", "Nature sounds and ambient music");
                break;

            case EmotionType.Calm:
                response.Action = "maintain_balance";
                parameters.Add("suggestion", "This is a great time for deep work or creative tasks.");
                parameters.Add("activities", new[] { "Focus on important tasks", "Read a book", "Practice a hobby", "Listen to classical music", "Meditate", "Reflect on your goals" });
                parameters.Add("music", "Peaceful classical or instrumental music");
                break;

            case EmotionType.Excited:
                response.Action = "channel_energy";
                parameters.Add("suggestion", "Consider tackling challenging tasks or starting a new project.");
                parameters.Add("activities", new[] { "Dance to energetic music", "Start a new project", "Exercise or workout", "Share your excitement with others", "Take on a challenge", "Celebrate this moment" });
                parameters.Add("music", "Energetic and upbeat playlists");
                break;

            case EmotionType.Frustrated:
                response.Action = "provide_assistance";
                parameters.Add("suggestion", "Maybe take a step back and approach this from a different angle.");
                parameters.Add("activities", new[] { "Take a short break", "Listen to instrumental music", "Do something different", "Ask for help", "Break the task into smaller steps", "Talk it through" });
                parameters.Add("music", "Focus and productivity instrumental music");
                break;

            case EmotionType.Neutral:
            default:
                response.Action = "maintain_status";
                if (string.IsNullOrEmpty(message))
                {
                    message = "You're in a neutral state. Everything is balanced.";
                }
                break;
        }

        // Add follow-up question and encouragement
        if (!string.IsNullOrEmpty(followUpQuestion))
        {
            parameters.Add("followUpQuestion", followUpQuestion);
        }

        if (!string.IsNullOrEmpty(encouragement))
        {
            parameters.Add("encouragement", encouragement);
        }

        // Add conversation context awareness
        if (context != null)
        {
            if (context.ConversationCount > 1)
            {
                parameters.Add("conversationCount", context.ConversationCount);
            }
            
            if (!string.IsNullOrEmpty(userId))
            {
                var mostCommon = _conversationMemory?.GetMostCommonEmotion(userId);
                if (mostCommon != null && mostCommon.Frequency > 3)
                {
                    parameters.Add("insight", $"I've noticed you often feel {mostCommon.Emotion.ToString().ToLower()}. Let's explore what might be contributing to this.");
                }
            }
        }

        response.Parameters = parameters;
        response.Message = message;

        // Save to conversation memory
        if (_conversationMemory != null && !string.IsNullOrEmpty(userMessage) && !string.IsNullOrEmpty(userId))
        {
            _conversationMemory.AddEntry(userId, userMessage, emotionResult, response, followUpQuestion);
        }

        _logger.LogInformation($"Generated empathetic response for emotion: {emotionResult.Emotion}");
        return response;
    }

    /// <summary>
    /// Gets default message if emotional intelligence is not available.
    /// </summary>
    private string GetDefaultMessage(EmotionType emotion)
    {
        return emotion switch
        {
            EmotionType.Happy => "Great to see you're feeling happy! Let's keep that positive energy going!",
            EmotionType.Sad => "I notice you might be feeling down. How about some calming activities?",
            EmotionType.Angry => "I sense some frustration. Let's create a calmer environment for you.",
            EmotionType.Anxious => "Feeling anxious? Let's help you relax.",
            EmotionType.Calm => "You're in a calm state. Perfect for focused work!",
            EmotionType.Excited => "You're excited! Let's channel that energy productively!",
            EmotionType.Frustrated => "I notice some frustration. How can I help?",
            _ => "I'm here with you. How are you feeling?"
        };
    }

    /// <summary>
    /// Gets IoT actions for the detected emotion.
    /// </summary>
    public async Task<List<IoTAction>> GetIoTActionsAsync(EmotionType emotion)
    {
        var actions = _iotSimulator.ProcessEmotion(emotion);
        
        // Try to execute on real devices if configured (await to ensure parameters are updated)
        if (_realIoTController != null)
        {
            foreach (var action in actions)
            {
                try
                {
                    var result = await _realIoTController.ExecuteActionAsync(action);
                    if (result)
                    {
                        _logger.LogInformation($"Successfully executed IoT action: {action.ActionType} on {action.DeviceId}");
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to execute IoT action {action.ActionType} on {action.DeviceId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error executing IoT action {action.ActionType}: {ex.Message}");
                }
            }
        }
        
        return actions;
    }
}

