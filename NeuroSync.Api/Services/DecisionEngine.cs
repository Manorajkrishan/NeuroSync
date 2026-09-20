using NeuroSync.Core;
using NeuroSync.IoT;

namespace NeuroSync.Api.Services;

/// <summary>
/// V1 DecisionEngine: Safety → Uncertainty → Mode → CompanionProvider → optional action ask.
/// Emits DecisionTrace for debugging. Does not diagnose.
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
    private readonly ICompanionProvider _companionProvider;
    private readonly EthicalAIFrameworkService? _consent;

    public DecisionEngine(
        IoTDeviceSimulator iotSimulator,
        RealIoTController? realIoTController,
        ILogger<DecisionEngine> logger,
        ConversationMemory? conversationMemory = null,
        EmotionalIntelligence? emotionalIntelligence = null,
        BestFriendCompanionService? companion = null,
        SafetyGateService? safetyGate = null,
        CompanionModeService? modes = null,
        EmotionalBaselineService? baseline = null,
        ICompanionProvider? companionProvider = null,
        EthicalAIFrameworkService? consent = null)
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
        _companionProvider = companionProvider ?? new TemplateCompanionProvider();
        _consent = consent;
    }

    public AdaptiveResponse GenerateResponse(EmotionResult emotionResult, string? userId = "default", string? userMessage = null)
    {
        userId ??= "default";

        var memoryAllowed = _consent == null || _consent.HasConsent(userId, ConsentType.Memory)
                            || _consent.HasConsent(userId, ConsentType.DataStorage);
        var emotionHistoryAllowed = _consent == null || _consent.HasConsent(userId, ConsentType.EmotionHistory)
                                    || _consent.HasConsent(userId, ConsentType.DataStorage);
        var iotConsent = _consent != null && _consent.HasConsent(userId, ConsentType.IoT);

        ConversationContext? context = null;
        if (_conversationMemory != null && (memoryAllowed || emotionHistoryAllowed))
            context = _conversationMemory.GetOrCreateContext(userId);

        var safety = _safetyGate?.Assess(userMessage, context) ?? new SafetyAssessment();
        var mode = _modes?.Resolve(userMessage, emotionResult.Emotion, safety.Level)
                   ?? CompanionInteractionMode.Talk;

        CompanionTurn? turn = null;
        if (_companion != null && !string.IsNullOrWhiteSpace(userMessage) && memoryAllowed)
        {
            turn = _companion.BuildTurn(userId, userMessage, emotionResult);
            if (safety.BlockNormalCompanionFlow)
                turn.IsCrisis = true;
        }

        // Prefer provider (template/LLM) for core message — keeps DecisionEngine model-agnostic
        var message = _companionProvider.Generate(
            emotionResult, mode, safety, emotionResult.Uncertainty, userMessage, turn?.DisplayName);

        // Soft-merge empathetic templates when high confidence and not crisis
        if (!safety.BlockNormalCompanionFlow
            && emotionResult.Uncertainty == UncertaintyLevel.HighConfidence
            && _emotionalIntelligence != null
            && !EmotionalIntelligence.IsGreetingOrSmallTalk(userMessage))
        {
            var rich = _emotionalIntelligence.GenerateEmpatheticMessage(emotionResult.Emotion, context, userMessage);
            if (!string.IsNullOrWhiteSpace(rich) && rich.Length > 20)
                message = _modes != null
                    ? _modes.ShapeMessage(rich, mode, turn?.DisplayName)
                    : rich;
            message = _emotionalIntelligence.PersonalizeMessage(message, turn?.Profile, turn);
        }
        else if (EmotionalIntelligence.IsGreetingOrSmallTalk(userMessage) && _emotionalIntelligence != null)
        {
            message = _emotionalIntelligence.GenerateEmpatheticMessage(emotionResult.Emotion, context, userMessage);
            if (!string.IsNullOrEmpty(turn?.DisplayName))
                message = $"{turn.DisplayName} — {message}";
        }

        string? followUpQuestion = null;
        if (!safety.BlockNormalCompanionFlow && emotionResult.Uncertainty == UncertaintyLevel.HighConfidence)
        {
            if (_modes != null && emotionResult.Emotion is EmotionType.Sad or EmotionType.Anxious or EmotionType.Frustrated)
                followUpQuestion = _modes.ModeChoicePrompt(mode);
            else if (_emotionalIntelligence != null)
                followUpQuestion = _emotionalIntelligence.GenerateFollowUpQuestion(
                    emotionResult.Emotion, userMessage ?? emotionResult.OriginalText, context);
        }
        else if (!safety.BlockNormalCompanionFlow && emotionResult.Uncertainty != UncertaintyLevel.HighConfidence)
        {
            followUpQuestion = "Want to tell me a bit more, or leave it for now?";
        }

        var offerQuiet = !safety.BlockNormalCompanionFlow
                         && mode is CompanionInteractionMode.Calm or CompanionInteractionMode.Focus
                         && emotionResult.Emotion is EmotionType.Anxious or EmotionType.Sad or EmotionType.Frustrated;

        var action = safety.BlockNormalCompanionFlow
            ? "crisis_support"
            : mode switch
            {
                CompanionInteractionMode.Listen => "supportive_listen",
                CompanionInteractionMode.ProblemSolving => "problem_friend",
                CompanionInteractionMode.Focus => "focus_mode",
                CompanionInteractionMode.Calm => "calm_companion",
                CompanionInteractionMode.Companion => "companion_chat",
                _ => EmotionalIntelligence.IsGreetingOrSmallTalk(userMessage) ? "converse" : "converse"
            };

        var baseline = emotionHistoryAllowed ? _baseline?.GetSnapshot(userId) : null;
        if (baseline != null && !baseline.HasSufficientData)
            baseline.IsSignificantlyDifferent = false;

        var trace = new DecisionTrace
        {
            Safety = safety.Level,
            Mode = mode,
            Uncertainty = emotionResult.Uncertainty,
            EmotionSignals = emotionResult.SignalEstimates.Count > 0
                ? new Dictionary<string, float>(emotionResult.SignalEstimates)
                : new Dictionary<string, float> { [emotionResult.Emotion.ToString()] = emotionResult.Confidence },
            BaselineDeviation = baseline is { HasSufficientData: true } ? baseline.Deviation : null,
            BaselineConfidence = baseline?.BaselineConfidence,
            Action = offerQuiet ? "AskQuietMode" : (ShouldTriggerIoT(userMessage) && iotConsent ? "IoTOnRequest" : "None"),
            MemoryWriteAllowed = memoryAllowed,
            Summary = string.Empty
        };
        trace.Summary = trace.ToString();

        var response = new AdaptiveResponse
        {
            Emotion = emotionResult.Emotion,
            Action = action,
            Message = message,
            Parameters = new Dictionary<string, object>
            {
                ["disclaimer"] =
                    "NeuroSync is an emotion-aware wellbeing companion / affective computing system — not a mental-health diagnostic product.",
                ["safetyLevel"] = safety.Level.ToString(),
                ["interactionMode"] = mode.ToString(),
                ["uncertainty"] = emotionResult.Uncertainty.ToString(),
                ["companionProvider"] = _companionProvider.ProviderId,
                ["decisionTrace"] = trace.Summary,
                ["memoryWriteAllowed"] = memoryAllowed
            }
        };

        if (!string.IsNullOrEmpty(emotionResult.UncertaintyNote))
            response.Parameters["uncertaintyNote"] = emotionResult.UncertaintyNote;
        if (safety.Level != SafetyLevel.Normal)
        {
            response.Parameters["safetyReason"] = safety.Reason;
            response.Parameters["safetyGuidance"] = safety.Guidance;
            if (!string.IsNullOrEmpty(safety.CrisisResourceHint))
                response.Parameters["crisisResources"] = safety.CrisisResourceHint;
        }
        if (offerQuiet)
            response.Parameters["actionOffer"] = "Would you like me to enable Quiet Mode?";
        if (ShouldTriggerIoT(userMessage) && !iotConsent)
            response.Parameters["iotBlocked"] = "IoTConsent is off — enable it in privacy settings to allow environment actions.";

        response.Parameters["understoodAs"] = emotionResult.UnderstoodAs ?? "";
        response.Parameters["intensity"] = emotionResult.Intensity;
        if (emotionResult.SignalEstimates.Count > 0)
            response.Parameters["signalEstimates"] = emotionResult.SignalEstimates;
        if (baseline != null)
        {
            response.Parameters["baseline"] = new
            {
                baseline.BaselineMoodScore,
                baseline.RecentMoodScore,
                baseline.Deviation,
                baseline.BaselineConfidence,
                baseline.HasSufficientData,
                baseline.IsSignificantlyDifferent,
                baseline.SampleCount,
                baseline.Summary
            };
            if (baseline.IsSignificantlyDifferent && baseline.HasSufficientData && !safety.BlockNormalCompanionFlow)
                response.Parameters["baselineCheckIn"] =
                    "You seem a bit different from your usual pattern. Want to talk, or switch off for a while?";
        }
        if (!string.IsNullOrEmpty(followUpQuestion))
            response.Parameters["followUpQuestion"] = followUpQuestion;
        if (turn != null)
        {
            response.Parameters["companionMode"] = "best_friend";
            if (!string.IsNullOrEmpty(turn.DisplayName))
                response.Parameters["ownerName"] = turn.DisplayName;
        }

        if (!memoryAllowed)
        {
            response.Parameters["memoryOff"] = true;
            response.Parameters["consentHint"] =
                "Memory is off — I won't keep long-term history. Enable MemoryConsent in privacy settings if you want me to remember.";
        }

        // Internal trace only in logs — not medical UI
        _logger.LogInformation("DecisionTrace {Trace}", trace.Summary);

        if (_conversationMemory != null && memoryAllowed && !string.IsNullOrEmpty(userMessage))
            _conversationMemory.AddEntry(userId, userMessage, emotionResult, response, followUpQuestion);

        return response;
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
                    await _realIoTController.ExecuteActionAsync(action);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "IoT action failed");
                }
            }
        }
        return actions;
    }
}
