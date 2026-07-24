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
    private readonly BestFriendCompanionService? _companion;

    public DecisionEngine(
        IoTDeviceSimulator iotSimulator, 
        RealIoTController? realIoTController, 
        ILogger<DecisionEngine> logger,
        ConversationMemory? conversationMemory = null,
        EmotionalIntelligence? emotionalIntelligence = null,
        BestFriendCompanionService? companion = null)
    {
        _iotSimulator = iotSimulator;
        _realIoTController = realIoTController;
        _logger = logger;
        _conversationMemory = conversationMemory;
        _emotionalIntelligence = emotionalIntelligence;
        _companion = companion;
    }

    /// <summary>
    /// Processes an emotion and generates adaptive responses with emotional intelligence.
    /// </summary>
    public AdaptiveResponse GenerateResponse(EmotionResult emotionResult, string? userId = "default", string? userMessage = null)
    {
        userId ??= "default";

        // Best-friend learning + self-thinking about the owner
        CompanionTurn? turn = null;
        if (_companion != null && !string.IsNullOrWhiteSpace(userMessage))
        {
            turn = _companion.BuildTurn(userId, userMessage, emotionResult);
        }

        ConversationContext? context = null;
        if (_conversationMemory != null)
            context = _conversationMemory.GetOrCreateContext(userId);

        string message;
        if (_emotionalIntelligence != null)
        {
            // Pass userMessage to generate contextual responses
            message = _emotionalIntelligence.GenerateEmpatheticMessage(emotionResult.Emotion, context, userMessage);

            // Lead with what we understood emotionally (best-friend acknowledgment)
            if (!string.IsNullOrEmpty(emotionResult.UnderstoodAs) &&
                !EmotionalIntelligence.IsGreetingOrSmallTalk(userMessage) &&
                emotionResult.Emotion != EmotionType.Neutral)
            {
                // Don't double-prefix if message already mirrors it
                if (!message.Contains("I understand you're feeling", StringComparison.OrdinalIgnoreCase))
                    message = $"{emotionResult.UnderstoodAs} {message}";
            }

            // Personalized greeting with owner's name
            if (EmotionalIntelligence.IsGreetingOrSmallTalk(userMessage) && !string.IsNullOrEmpty(turn?.DisplayName))
            {
                message = $"{turn.DisplayName} — {message}";
            }

            message = _emotionalIntelligence.PersonalizeMessage(message, turn?.Profile, turn);
        }
        else
        {
            message = GetDefaultMessage(emotionResult.Emotion);
        }

        string? followUpQuestion = null;
        if (_emotionalIntelligence != null && turn?.IsCrisis != true)
            followUpQuestion = _emotionalIntelligence.GenerateFollowUpQuestion(emotionResult.Emotion, userMessage ?? emotionResult.OriginalText, context);

        // Prefer gentle learning question over generic follow-up sometimes
        if (string.IsNullOrEmpty(followUpQuestion) && !string.IsNullOrEmpty(turn?.LearningQuestion))
            followUpQuestion = turn.LearningQuestion;

        string? encouragement = null;
        if (_emotionalIntelligence != null && context != null)
            encouragement = _emotionalIntelligence.GenerateEncouragement(context);

        var response = new AdaptiveResponse
        {
            Emotion = emotionResult.Emotion,
            Message = message
        };

        var isGreeting = EmotionalIntelligence.IsGreetingOrSmallTalk(userMessage);
        var parameters = new Dictionary<string, object>();

        if (turn?.IsCrisis == true)
        {
            response.Action = "crisis_support";
            parameters["mode"] = "best_friend_crisis";
            parameters["priority"] = "immediate_support";
            parameters["resources"] = "Please contact local emergency services or a crisis helpline if you are in danger.";
        }
        else if (isGreeting)
        {
            response.Action = "converse";
            parameters["mode"] = "best_friend";
        }
        else
        {
            switch (emotionResult.Emotion)
            {
                case EmotionType.Happy:
                    response.Action = "celebrate_with_friend";
                    parameters["suggestion"] = "I'm genuinely happy for you — tell me more?";
                    break;
                case EmotionType.Sad:
                    response.Action = "support_wellbeing";
                    parameters["suggestion"] = turn?.PersonalizedHelp != null
                        ? $"I'm here. Want to try {turn.PersonalizedHelp}? Or just talk — either is fine."
                        : "I'm here if you want to talk. No pressure.";
                    break;
                case EmotionType.Angry:
                    response.Action = "supportive_listen";
                    parameters["suggestion"] = "I've got you. Vent it out — I'm listening.";
                    break;
                case EmotionType.Anxious:
                    response.Action = "calm_companion";
                    parameters["suggestion"] = turn?.PersonalizedHelp != null
                        ? $"One step at a time. {turn.PersonalizedHelp} has helped you before — want that, or just company?"
                        : "We can take this one breath at a time. What's the loudest worry right now?";
                    break;
                case EmotionType.Frustrated:
                    response.Action = "problem_friend";
                    parameters["suggestion"] = "That sounds rough. Want to unpack it together?";
                    break;
                case EmotionType.Calm:
                case EmotionType.Excited:
                case EmotionType.Neutral:
                default:
                    response.Action = "converse";
                    parameters["suggestion"] = "I'm with you. What's on your mind?";
                    break;
            }
        }

        if (turn != null)
        {
            parameters["companionMode"] = "best_friend";
            parameters["selfThoughts"] = turn.SelfThoughts;
            if (!string.IsNullOrEmpty(turn.DisplayName))
                parameters["ownerName"] = turn.DisplayName;
            if (turn.HasConcerningPattern)
                parameters["support_note"] = "I've noticed things have been heavy lately. I'm still here — we don't have to fix everything today.";
            if (!string.IsNullOrEmpty(turn.PersonalizedHelp))
                parameters["knownHelp"] = turn.PersonalizedHelp;
            var known = turn.Profile.GetWhatIKnow();
            if (!string.IsNullOrWhiteSpace(known))
                parameters["whatIKnowAboutYou"] = known;
        }

        // Surface emotional understanding to the client/UI
        parameters["understoodAs"] = emotionResult.UnderstoodAs ?? $"You're feeling {emotionResult.Emotion}";
        parameters["intensity"] = emotionResult.Intensity;
        if (!string.IsNullOrEmpty(emotionResult.LikelyCause))
            parameters["likelyCause"] = emotionResult.LikelyCause;
        if (emotionResult.SecondaryEmotion.HasValue)
            parameters["secondaryEmotion"] = emotionResult.SecondaryEmotion.Value.ToString();

        if (!string.IsNullOrEmpty(followUpQuestion))
            parameters["followUpQuestion"] = followUpQuestion;
        if (!string.IsNullOrEmpty(encouragement))
            parameters["encouragement"] = encouragement;

        if (context != null && context.ConversationCount > 1)
            parameters["conversationCount"] = context.ConversationCount;

        if (_conversationMemory != null)
        {
            var mostCommon = _conversationMemory.GetMostCommonEmotion(userId);
            if (mostCommon != null && mostCommon.Frequency > 3)
                parameters["insight"] = $"I've noticed you often feel {mostCommon.Emotion.ToString().ToLower()}. We can face that together whenever you want.";
        }

        response.Parameters = parameters;
        response.Message = message;

        if (_conversationMemory != null && !string.IsNullOrEmpty(userMessage))
            _conversationMemory.AddEntry(userId, userMessage, emotionResult, response, followUpQuestion);

        _logger.LogInformation("Best-friend response for {UserId}: emotion={Emotion}, crisis={Crisis}",
            userId, emotionResult.Emotion, turn?.IsCrisis == true);
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
    /// IoT only when the user asks for lights/music/environment — not on every message.
    /// </summary>
    public static bool ShouldTriggerIoT(string? userMessage)
    {
        return EmotionalIntelligence.IsIoTRequest(userMessage);
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

