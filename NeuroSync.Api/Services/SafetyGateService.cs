using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Dedicated safety gate that runs before companion responses.
/// Routes crisis signals to human-support guidance — never "AI replaces people".
/// </summary>
public class SafetyGateService
{
    private readonly EmotionalIntelligence _ei;
    private readonly ILogger<SafetyGateService> _logger;

    private static readonly string[] ImmediateDanger =
    {
        "suicide", "kill myself", "end my life", "want to die",
        "hurt myself", "self harm", "self-harm", "going to kill"
    };

    private static readonly string[] PotentialCrisis =
    {
        "can't go on", "give up on life", "no reason to live",
        "better off dead", "don't want to wake up"
    };

    private static readonly string[] ElevatedDistress =
    {
        "i can't cope", "breaking down", "falling apart",
        "everything is hopeless", "completely alone", "nobody cares"
    };

    public SafetyGateService(EmotionalIntelligence ei, ILogger<SafetyGateService> logger)
    {
        _ei = ei;
        _logger = logger;
    }

    public SafetyAssessment Assess(string? userMessage, ConversationContext? context = null)
    {
        var assessment = new SafetyAssessment();
        if (string.IsNullOrWhiteSpace(userMessage))
            return assessment;

        var m = userMessage.ToLowerInvariant();

        if (ImmediateDanger.Any(k => m.Contains(k)) || _ei.NeedsImmediateSupport(userMessage))
        {
            assessment.Level = SafetyLevel.ImmediateDanger;
            assessment.BlockNormalCompanionFlow = true;
            assessment.Reason = "Message contains language associated with immediate personal danger.";
            assessment.Guidance =
                "Please contact local emergency services or a crisis helpline now. " +
                "NeuroSync can stay with you as a companion, but real human help matters most.";
            assessment.CrisisResourceHint =
                "If you are in the UK: Samaritans 116 123. Elsewhere: find a local crisis line.";
            _logger.LogWarning("Safety gate: ImmediateDanger for conversation turn");
            return assessment;
        }

        if (PotentialCrisis.Any(k => m.Contains(k)))
        {
            assessment.Level = SafetyLevel.PotentialCrisis;
            assessment.BlockNormalCompanionFlow = true;
            assessment.Reason = "Message suggests severe distress that needs human support options.";
            assessment.Guidance =
                "Please reach someone you trust or a crisis helpline. You don't have to face this alone.";
            assessment.CrisisResourceHint =
                "Prefer human support over AI when distress is this high.";
            return assessment;
        }

        if (ElevatedDistress.Any(k => m.Contains(k)) ||
            (context != null && context.ConversationCount >= 5 &&
             context.History.TakeLast(4).Count(e =>
                 e.DetectedEmotion?.Emotion is EmotionType.Sad or EmotionType.Anxious or EmotionType.Angry) >= 3))
        {
            assessment.Level = SafetyLevel.ElevatedDistress;
            assessment.Reason = "Elevated distress signals — stay supportive and offer choice.";
            assessment.Guidance =
                "Offer listen / calm / talk options. Do not diagnose. Encourage real-world support if it continues.";
            return assessment;
        }

        return assessment;
    }
}
