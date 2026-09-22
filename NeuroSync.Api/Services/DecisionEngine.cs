using NeuroSync.Core;
using NeuroSync.IoT;

namespace NeuroSync.Api.Services;

/// <summary>
/// Companion DecisionEngine:
/// Safety → Intent → Emotion(sensor) → Baseline → Mode → Policy → Natural response.
/// Never exposes emotion labels/confidence as the chat message.
/// LLM providers never control safety or IoT — this engine remains authoritative.
/// </summary>
public class DecisionEngine
{
    private readonly IoTDeviceSimulator _iotSimulator;
    private readonly RealIoTController? _realIoTController;
    private readonly ILogger<DecisionEngine> _logger;
    private readonly ConversationMemory? _conversationMemory;
    private readonly BestFriendCompanionService? _companion;
    private readonly SafetyGateService _safetyGate;
    private readonly IntentRouterService _intents;
    private readonly CompanionModeService _modes;
    private readonly EmotionalBaselineService? _baseline;
    private readonly ResponsePolicyService _policy;
    private readonly ICompanionResponseService _responder;
    private readonly EthicalAIFrameworkService? _consent;

    public DecisionEngine(
        IoTDeviceSimulator iotSimulator,
        RealIoTController? realIoTController,
        ILogger<DecisionEngine> logger,
        SafetyGateService safetyGate,
        IntentRouterService intents,
        CompanionModeService modes,
        ResponsePolicyService policy,
        ICompanionResponseService responder,
        ConversationMemory? conversationMemory = null,
        BestFriendCompanionService? companion = null,
        EmotionalBaselineService? baseline = null,
        EthicalAIFrameworkService? consent = null,
        EmotionalIntelligence? emotionalIntelligence = null,
        ICompanionProvider? companionProvider = null)
    {
        _iotSimulator = iotSimulator;
        _realIoTController = realIoTController;
        _logger = logger;
        _safetyGate = safetyGate;
        _intents = intents;
        _modes = modes;
        _policy = policy;
        _responder = responder;
        _conversationMemory = conversationMemory;
        _companion = companion;
        _baseline = baseline;
        _consent = consent;
        // companionProvider is optional; CompanionResponseService already holds ICompanionProvider.
        _ = companionProvider;
        _ = emotionalIntelligence;
    }

    public AdaptiveResponse GenerateResponse(EmotionResult emotionResult, string? userId = "default", string? userMessage = null)
        => GenerateResponseAsync(emotionResult, userId, userMessage).GetAwaiter().GetResult();

    public async Task<AdaptiveResponse> GenerateResponseAsync(
        EmotionResult emotionResult,
        string? userId = "default",
        string? userMessage = null,
        CancellationToken cancellationToken = default)
    {
        userId ??= "default";
        userMessage ??= emotionResult.OriginalText ?? "";

        var memoryAllowed = _consent == null || _consent.HasConsent(userId, ConsentType.Memory)
                            || _consent.HasConsent(userId, ConsentType.DataStorage);
        var emotionHistoryAllowed = _consent == null || _consent.HasConsent(userId, ConsentType.EmotionHistory)
                                    || _consent.HasConsent(userId, ConsentType.DataStorage);
        var iotConsent = _consent != null && _consent.HasConsent(userId, ConsentType.IoT);

        ConversationContext? context = null;
        if (_conversationMemory != null && (memoryAllowed || emotionHistoryAllowed))
            context = _conversationMemory.GetOrCreateContext(userId);

        // SafetyGate is authoritative before companion generation.
        var safety = _safetyGate.Assess(userMessage, context);

        var intent = _intents.Detect(userMessage, context, safety.Level);
        if (safety.BlockNormalCompanionFlow)
            intent = UserIntent.SafetySensitive;

        if (intent is UserIntent.Greeting or UserIntent.CasualConversation
            || userMessage.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 2)
        {
            if (intent is UserIntent.Greeting or UserIntent.CasualConversation)
            {
                emotionResult.Uncertainty = UncertaintyLevel.InsufficientEvidence;
                emotionResult.UncertaintyNote = "Short/social message — insufficient emotional evidence.";
            }
        }

        CompanionTurn? turn = null;
        if (_companion != null && !string.IsNullOrWhiteSpace(userMessage) && memoryAllowed
            && intent is UserIntent.EmotionalDisclosure or UserIntent.ListeningRequest or UserIntent.AdviceRequest)
        {
            turn = _companion.BuildTurn(userId, userMessage, emotionResult);
            if (safety.BlockNormalCompanionFlow) turn.IsCrisis = true;
        }

        var mode = intent switch
        {
            UserIntent.ListeningRequest => CompanionInteractionMode.Listen,
            UserIntent.AdviceRequest => CompanionInteractionMode.ProblemSolving,
            UserIntent.Greeting or UserIntent.CasualConversation => CompanionInteractionMode.Talk,
            UserIntent.EnvironmentAction => CompanionInteractionMode.Focus,
            UserIntent.SafetySensitive => CompanionInteractionMode.Listen,
            _ => _modes.Resolve(userMessage, emotionResult.Emotion, safety.Level)
        };
        if (intent == UserIntent.EmotionalDisclosure && mode == CompanionInteractionMode.Talk)
            mode = CompanionInteractionMode.Listen;

        var baseline = emotionHistoryAllowed ? _baseline?.GetSnapshot(userId) : null;
        if (baseline is { HasSufficientData: false })
            baseline.IsSignificantlyDifferent = false;

        var emotionSignals = emotionResult.SignalEstimates.Count > 0
            ? new Dictionary<string, float>(emotionResult.SignalEstimates)
            : new Dictionary<string, float> { [emotionResult.Emotion.ToString()] = emotionResult.Confidence };

        var recentTurns = BuildRecentTurns(context, memoryAllowed);
        var relevantMemory = memoryAllowed ? BuildRelevantMemory(turn, context) : null;

        var turnCtx = new CompanionContext
        {
            CurrentMessage = userMessage,
            DisplayName = turn?.DisplayName,
            Intent = intent,
            Mode = mode,
            Emotion = emotionResult,
            Safety = safety,
            Uncertainty = emotionResult.Uncertainty,
            Conversation = context,
            Baseline = baseline,
            EmotionSignals = emotionSignals,
            RecentTurns = recentTurns,
            RelevantMemory = relevantMemory
        };

        var policy = _policy.Evaluate(turnCtx);
        turnCtx.PolicyGuidance = policy.SystemGuidance;

        // Responder → ICompanionProvider (Template or Llm→Template fallback).
        var reply = await _responder.GenerateAsync(turnCtx, policy, cancellationToken).ConfigureAwait(false);

        var offerQuiet = !safety.BlockNormalCompanionFlow
                         && mode is CompanionInteractionMode.Calm or CompanionInteractionMode.Focus
                         && intent is UserIntent.EmotionalDisclosure or UserIntent.TaskRequest;

        var action = safety.BlockNormalCompanionFlow
            ? "crisis_support"
            : intent switch
            {
                UserIntent.Greeting or UserIntent.CasualConversation => "converse",
                UserIntent.EnvironmentAction => "environment_ask",
                UserIntent.ListeningRequest => "supportive_listen",
                UserIntent.AdviceRequest => "problem_friend",
                UserIntent.EmotionalDisclosure => mode == CompanionInteractionMode.Listen ? "supportive_listen" : "converse",
                _ => "converse"
            };

        var trace = new DecisionTrace
        {
            Safety = safety.Level,
            Mode = mode,
            Uncertainty = emotionResult.Uncertainty,
            EmotionSignals = emotionSignals,
            BaselineDeviation = baseline is { HasSufficientData: true } ? baseline.Deviation : null,
            BaselineConfidence = baseline?.BaselineConfidence,
            Action = offerQuiet ? "AskQuietMode" : (ShouldTriggerIoT(userMessage) && iotConsent ? "IoTOnRequest" : "None"),
            MemoryWriteAllowed = memoryAllowed
        };
        trace.Summary = $"Intent={intent} → " + trace.ToString();

        var response = new AdaptiveResponse
        {
            Emotion = emotionResult.Emotion,
            Action = action,
            Message = reply.Message,
            Parameters = new Dictionary<string, object>
            {
                ["disclaimer"] =
                    "NeuroSync is an emotion-aware wellbeing companion — not a mental-health diagnostic product.",
                ["intent"] = intent.ToString(),
                ["interactionMode"] = mode.ToString(),
                ["safetyLevel"] = safety.Level.ToString(),
                ["uncertainty"] = emotionResult.Uncertainty.ToString(),
                ["companionProvider"] = reply.ProviderId,
                ["decisionTrace"] = trace.Summary,
                ["memoryWriteAllowed"] = memoryAllowed,
                ["exposedEmotionToUser"] = false,
                ["developerInsights"] = new
                {
                    intent = intent.ToString(),
                    mode = mode.ToString(),
                    safety = safety.Level.ToString(),
                    primarySignal = emotionResult.Emotion.ToString(),
                    modelScore = emotionResult.Confidence,
                    signals = emotionResult.SignalEstimates,
                    uncertainty = emotionResult.Uncertainty.ToString(),
                    baselineConfidence = baseline?.BaselineConfidence,
                    baselineDeviation = baseline?.Deviation,
                    policyBlocked = policy.ViolationsBlocked
                }
            }
        };

        if (offerQuiet)
            response.Parameters["actionOffer"] = "Would you like me to enable Quiet Mode?";
        if (ShouldTriggerIoT(userMessage) && !iotConsent)
            response.Parameters["iotBlocked"] = "IoTConsent is off — enable it in privacy settings first.";
        if (!memoryAllowed)
            response.Parameters["consentHint"] =
                "Memory is off — I won't keep long-term history unless you enable MemoryConsent.";
        if (!string.IsNullOrEmpty(emotionResult.UncertaintyNote))
            response.Parameters["uncertaintyNote"] = emotionResult.UncertaintyNote;

        _logger.LogInformation("Companion turn {Trace}", trace.Summary);

        if (_conversationMemory != null && memoryAllowed && !string.IsNullOrEmpty(userMessage))
            _conversationMemory.AddEntry(userId, userMessage, emotionResult, response, null);

        return response;
    }

    private static IReadOnlyList<CompanionConversationTurn> BuildRecentTurns(
        ConversationContext? context,
        bool memoryAllowed)
    {
        if (!memoryAllowed || context?.History == null || context.History.Count == 0)
            return Array.Empty<CompanionConversationTurn>();

        var turns = new List<CompanionConversationTurn>();
        foreach (var entry in context.History.TakeLast(4))
        {
            if (!string.IsNullOrWhiteSpace(entry.UserMessage))
            {
                turns.Add(new CompanionConversationTurn
                {
                    Role = "user",
                    Text = entry.UserMessage.Trim(),
                    Timestamp = entry.Timestamp
                });
            }

            var assistant = entry.Response?.Message;
            if (!string.IsNullOrWhiteSpace(assistant))
            {
                turns.Add(new CompanionConversationTurn
                {
                    Role = "assistant",
                    Text = assistant.Trim(),
                    Timestamp = entry.Timestamp
                });
            }
        }

        return turns;
    }

    private static string? BuildRelevantMemory(CompanionTurn? turn, ConversationContext? context)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(turn?.DisplayName))
            parts.Add($"preferredName: {turn!.DisplayName}");
        if (!string.IsNullOrWhiteSpace(turn?.PersonalizedHelp))
            parts.Add(turn!.PersonalizedHelp!);
        if (!string.IsNullOrWhiteSpace(context?.CurrentTopic))
            parts.Add($"topic: {context.CurrentTopic}");
        if (turn?.SelfThoughts is { Count: > 0 })
            parts.Add("notes: " + string.Join("; ", turn.SelfThoughts.Take(3)));

        return parts.Count == 0 ? null : string.Join(" | ", parts);
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
                try { await _realIoTController.ExecuteActionAsync(action); }
                catch (Exception ex) { _logger.LogWarning(ex, "IoT action failed"); }
            }
        }
        return actions;
    }
}
