namespace NeuroSync.Core;

/// <summary>
/// Structured context for companion response generation.
/// Emotion is a sensor input — never the final user-facing product.
/// Safety / IoT decisions are made before this is passed to a provider.
/// </summary>
public class CompanionContext
{
    /// <summary>Current user message for this turn.</summary>
    public string CurrentMessage { get; set; } = string.Empty;

    /// <summary>Alias used by policy / templates (same as <see cref="CurrentMessage"/>).</summary>
    public string UserMessage
    {
        get => CurrentMessage;
        set => CurrentMessage = value;
    }

    public string? DisplayName { get; set; }
    public UserIntent Intent { get; set; } = UserIntent.Unknown;
    public CompanionInteractionMode Mode { get; set; } = CompanionInteractionMode.Talk;
    public EmotionResult? Emotion { get; set; }
    public SafetyAssessment Safety { get; set; } = new();
    public UncertaintyLevel Uncertainty { get; set; } = UncertaintyLevel.InsufficientEvidence;
    public ConversationContext? Conversation { get; set; }
    public EmotionalBaselineSnapshot? Baseline { get; set; }
    public string? TopicHint { get; set; }

    /// <summary>Emotion signal estimates (sensor layer — not for user-facing claims).</summary>
    public IReadOnlyDictionary<string, float> EmotionSignals { get; set; }
        = new Dictionary<string, float>();

    /// <summary>Recent conversation turns (consent-gated by caller).</summary>
    public IReadOnlyList<CompanionConversationTurn> RecentTurns { get; set; }
        = Array.Empty<CompanionConversationTurn>();

    /// <summary>Consent-approved relevant memory summary (null if memory consent is off).</summary>
    public string? RelevantMemory { get; set; }

    /// <summary>Optional policy guidance for LLM providers (never overrides SafetyGate).</summary>
    public string? PolicyGuidance { get; set; }
}

/// <summary>Back-compat name for <see cref="CompanionContext"/>.</summary>
public class CompanionTurnContext : CompanionContext
{
}

/// <summary>One turn in recent conversation history passed to the companion provider.</summary>
public sealed class CompanionConversationTurn
{
    public string Role { get; set; } = "user";
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CompanionReply
{
    public string Message { get; set; } = string.Empty;
    public string ProviderId { get; set; } = "template-companion";
    public bool ExposedEmotionToUser { get; set; }
}
