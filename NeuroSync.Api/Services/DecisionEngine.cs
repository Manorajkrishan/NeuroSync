using NeuroSync.Core;
using NeuroSync.IoT;

namespace NeuroSync.Api.Services;

/// <summary>
/// Decision engine: safety → emotion context → companion mode → response.
/// Does not diagnose. IoT only when the user asks.
/// </summary>
public class DecisionEngine
{
    private readonly IoTDeviceSimulator _iotSimulator;
    private readonly RealIoTController? _realIoTController;
    private readonly ILogger<DecisionEngine> _logger;
    private readonly ConversationMemory? _conversationMemory;
    private readonly EmotionalIntelligence? _emotionalIntelligence;
    private readonly BestFriendCompanionService? _companion;
    private readonly SafetyGateService? _safetyGate;
    private readonly CompanionModeService? _modes;
    private readonly EmotionalBaselineService? _baseline;

    public DecisionEngine(
        IoTDeviceSimulator iotSimulator,
        RealIoTController? realIoTController,
        ILogger<DecisionEngine> logger,
        ConversationMemory? conversationMemory = null,
        EmotionalIntelligence? emotionalIntelligence = null,
        BestFriendCompanionService? companion = null,
        SafetyGateService? safetyGate = null,
        CompanionModeService? modes = null,
        EmotionalBaselineService? baseline = null)
    {
        _iotSimulator = iotSimulator;
        _realIoTController = realIoTController;
        _logger = logger;
        _conversationMemory = conversationMemory;
        _emotionalIntelligence = emotionalIntelligence;
        _companion = companion;
        _safetyGate = safetyGate;
        _modes = modes;
        _baseline = baseline;
    }

    public AdaptiveResponse GenerateResponse(EmotionResult emotionResult, string? userId = "default", string? userMessage = null)
    {
        userId ??= "default";

        ConversationContext? context = null;
        if (_conversationMemory != null)
            context = _conversationMemory.GetOrCreateContext(userId);

        var safety = _safetyGate?.Assess(userMessage, context)
                     ?? new SafetyAssessment();

        CompanionTurn? turn = null;
        if (_companion != null && !string.IsNullOrWhiteSpace(userMessage))
        {
            turn = _companion.BuildTurn(userId, userMessage, emotionResult);
            if (safety.BlockNormalCompanionFlow)
                turn.IsCrisis = true;
        }

        var mode = _modes?.Resolve(userMessage, emotionResult.Emotion, safety.Level)
                   ?? CompanionInteractionMode.Talk;

        string message;
        if (safety.BlockNormalCompanionFlow && _emotionalIntelligence != null)
        {
            message = _emotionalIntelligence.PersonalizeMessage(
                safety.Guidance,
                turn?.Profile,
                turn ?? new CompanionTurn { IsCrisis = true });
        }
        else if (_emotionalIntelligence != null)
        {
            message = _emotionalIntelligence.GenerateEmpatheticMessage(emotionResult.Emotion, context, userMessage);

            if (!string.IsNullOrEmpty(emotionResult.UnderstoodAs) &&
                !EmotionalIntelligence.IsGreetingOrSmallTalk(userMessage) &&
                emotionResult.Emotion != EmotionType.Neutral)
            {
                if (!message.Contains("I understand you're feeling", StringComparison.OrdinalIgnoreCase))
                    message = $"{emotionResult.UnderstoodAs} {message}";
            }

            if (EmotionalIntelligence.IsGreetingOrSmallTalk(userMessage) && !string.IsNullOrEmpty(turn?.DisplayName))
                message = $"{turn.DisplayName} — {message}";

            message = _emotionalIntelligence.PersonalizeMessage(message, turn?.Profile, turn);

            if (_modes != null && !EmotionalIntelligence.IsGreetingOrSmallTalk(userMessage))
                message = _modes.ShapeMessage(message, mode, turn?.DisplayName);
        }
        else
        {
            message = GetDefaultMessage(emotionResult.Emotion);
        }

        string? followUpQuestion = null;
        if (!safety.BlockNormalCompanionFlow && _emotionalIntelligence != null && turn?.IsCrisis != true)
        {
            followUpQuestion = _modes != null &&
                               emotionResult.Emotion is EmotionType.Sad or EmotionType.Anxious or EmotionType.Frustrated
                ? _modes.ModeChoicePrompt(mode)
                : _emotionalIntelligence.GenerateFollowUpQuestion(
                    emotionResult.Emotion, userMessage ?? emotionResult.OriginalText, context);
        }

        if (string.IsNullOrEmpty(followUpQuestion) && !string.IsNullOrEmpty(turn?.LearningQuestion))
            followUpQuestion = turn.LearningQuestion;

        string? encouragement = null;
        if (!safety.BlockNormalCompanionFlow && _emotionalIntelligence != null && context != null)
            encouragement = _emotionalIntelligence.GenerateEncouragement(context);

        var response = new AdaptiveResponse
        {
            Emotion = emotionResult.Emotion,
            Message = message
        };

        var isGreeting = EmotionalIntelligence.IsGreetingOrSmallTalk(userMessage);
        var parameters = new Dictionary<string, object>
        {
            ["disclaimer"] =
                "NeuroSync is a privacy-first wellbeing companion. It does not diagnose mental illness or replace therapists.",
            ["safetyLevel"] = safety.Level.ToString(),
            ["interactionMode"] = mode.ToString()
        };

        if (safety.Level != SafetyLevel.Normal)
        {
            parameters["safetyReason"] = safety.Reason;
            parameters["safetyGuidance"] = safety.Guidance;
            if (!string.IsNullOrEmpty(safety.CrisisResourceHint))
                parameters["crisisResources"] = safety.CrisisResourceHint;
        }

        if (safety.BlockNormalCompanionFlow)
        {
            response.Action = "crisis_support";
            parameters["mode"] = "safety_protocol";
            parameters["priority"] = "human_support_first";
            parameters["resources"] = safety.CrisisResourceHint
                ?? "Please contact local emergency services or a crisis helpline if you are in danger.";
        }
        else if (isGreeting)
        {
            response.Action = "converse";
            parameters["mode"] = "companion";
        }
        else
        {
            response.Action = mode switch
            {
                CompanionInteractionMode.Listen => "supportive_listen",
                CompanionInteractionMode.ProblemSolving => "problem_friend",
                CompanionInteractionMode.Focus => "focus_mode",
                CompanionInteractionMode.Calm => "calm_companion",
                CompanionInteractionMode.Companion => "companion_chat",
                _ => "converse"
            };
            parameters["suggestion"] = mode switch
            {
                CompanionInteractionMode.Listen => "I'm listening. No pressure.",
                CompanionInteractionMode.ProblemSolving => "One small next step when you're ready.",
                CompanionInteractionMode.Focus => "Want quiet mode / Do Not Disturb style support?",
                CompanionInteractionMode.Calm => turn?.PersonalizedHelp != null
                    ? $"Want {turn.PersonalizedHelp}, or just company?"
                    : "Want quiet mode or a short reset?",
                _ => "I'm with you. What's on your mind?"
            };
        }

        if (turn != null)
        {
            parameters["companionMode"] = "best_friend";
            parameters["selfThoughts"] = turn.SelfThoughts;
            if (!string.IsNullOrEmpty(turn.DisplayName))
                parameters["ownerName"] = turn.DisplayName;
            if (turn.HasConcerningPattern)
                parameters["support_note"] =
                    "I've noticed things have felt heavier than your usual pattern. I'm here — we don't have to fix everything today. This is not a diagnosis.";
            if (!string.IsNullOrEmpty(turn.PersonalizedHelp))
                parameters["knownHelp"] = turn.PersonalizedHelp;
            var known = turn.Profile.GetWhatIKnow();
            if (!string.IsNullOrWhiteSpace(known))
                parameters["whatIKnowAboutYou"] = known;
        }

        parameters["understoodAs"] = emotionResult.UnderstoodAs ?? $"You're feeling {emotionResult.Emotion}";
        parameters["intensity"] = emotionResult.Intensity;
        if (!string.IsNullOrEmpty(emotionResult.LikelyCause))
            parameters["likelyCause"] = emotionResult.LikelyCause;
        if (emotionResult.SecondaryEmotion.HasValue)
            parameters["secondaryEmotion"] = emotionResult.SecondaryEmotion.Value.ToString();
        if (emotionResult.SignalEstimates.Count > 0)
            parameters["signalEstimates"] = emotionResult.SignalEstimates;

        var baseline = _baseline?.GetSnapshot(userId);
        if (baseline != null && baseline.SampleCount >= 3)
        {
            parameters["baseline"] = new
            {
                baseline.BaselineMoodScore,
                baseline.RecentMoodScore,
                baseline.Deviation,
                baseline.IsSignificantlyDifferent,
                baseline.Summary
            };
            if (baseline.IsSignificantlyDifferent && !safety.BlockNormalCompanionFlow)
                parameters["baselineCheckIn"] =
                    "You seem a bit different from your usual pattern. Want to talk, or switch off for a while?";
        }

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
                parameters["insight"] =
                    $"I've noticed {mostCommon.Emotion.ToString().ToLower()} shows up often in our chats. We can face that together whenever you want — still not a diagnosis.";
        }

        response.Parameters = parameters;
        response.Message = message;

        if (_conversationMemory != null && !string.IsNullOrEmpty(userMessage))
            _conversationMemory.AddEntry(userId, userMessage, emotionResult, response, followUpQuestion);

        _logger.LogInformation(
            "Companion response for {UserId}: emotion={Emotion}, mode={Mode}, safety={Safety}",
            userId, emotionResult.Emotion, mode, safety.Level);
        return response;
    }

    private string GetDefaultMessage(EmotionType emotion)
    {
        return emotion switch
        {
            EmotionType.Happy => "Glad you're feeling good. Want to share what's going well?",
            EmotionType.Sad => "That sounds heavy. Want me to listen, help work through it, or distract you?",
            EmotionType.Angry => "I hear the frustration. I'm listening — no lectures.",
            EmotionType.Anxious => "Anxiety is loud sometimes. Want calm mode, or to unpack one worry?",
            EmotionType.Calm => "Calm is a good place to be. Want focus mode?",
            EmotionType.Excited => "Love the energy. Tell me more?",
            EmotionType.Frustrated => "That sounds stuck. Want a small next step, or just to vent?",
            _ => "I'm here with you. How are you feeling?"
        };
    }

    public static bool ShouldTriggerIoT(string? userMessage) =>
        EmotionalIntelligence.IsIoTRequest(userMessage);

    public async Task<List<IoTAction>> GetIoTActionsAsync(EmotionType emotion)
    {
        var actions = _iotSimulator.ProcessEmotion(emotion);

        if (_realIoTController != null)
        {
            foreach (var action in actions)
            {
                try
                {
                    var result = await _realIoTController.ExecuteActionAsync(action);
                    if (result)
                        _logger.LogInformation("Executed IoT action: {Action} on {Device}", action.ActionType, action.DeviceId);
                    else
                        _logger.LogWarning("Failed IoT action {Action} on {Device}", action.ActionType, action.DeviceId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error executing IoT action {Action}", action.ActionType);
                }
            }
        }

        return actions;
    }
}
