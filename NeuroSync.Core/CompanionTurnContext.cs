namespace NeuroSync.Core;

/// <summary>
/// Structured context for companion response generation.
/// Emotion is a sensor input — never the final user-facing product.
/// </summary>
public class CompanionTurnContext
{
    public string UserMessage { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public UserIntent Intent { get; set; } = UserIntent.Unknown;
    public CompanionInteractionMode Mode { get; set; } = CompanionInteractionMode.Talk;
    public EmotionResult? Emotion { get; set; }
    public SafetyAssessment Safety { get; set; } = new();
    public UncertaintyLevel Uncertainty { get; set; } = UncertaintyLevel.InsufficientEvidence;
    public ConversationContext? Conversation { get; set; }
    public EmotionalBaselineSnapshot? Baseline { get; set; }
    public string? TopicHint { get; set; }
}

public class CompanionReply
{
    public string Message { get; set; } = string.Empty;
    public string ProviderId { get; set; } = "template-companion";
    public bool ExposedEmotionToUser { get; set; }
}
