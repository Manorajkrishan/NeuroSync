using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Safety classification — SEPARATE from emotion detection.
/// Handles negation, quotes, jokes, lyrics, hypotheticals, third-person, and escalation.
/// </summary>
public class SafetyGateService
{
    private readonly ILogger<SafetyGateService> _logger;

    private static readonly string[] ImmediateDanger =
    {
        "kill myself", "end my life", "want to die", "going to kill myself",
        "hurt myself", "self harm", "self-harm", "cut myself",
        "i'm going to end it", "im going to end it", "commit suicide",
        "planning suicide", "my suicide", "i am suicidal", "i'm suicidal", "im suicidal"
    };

    private static readonly string[] PotentialCrisis =
    {
        "can't go on", "cant go on", "give up on life", "no reason to live",
        "better off dead", "don't want to wake up", "dont want to wake up",
        "wish i wasn't here", "wish i wasnt here", "don't want to be alive",
        "dont want to be alive"
    };

    private static readonly string[] ElevatedDistress =
    {
        "i can't cope", "i cant cope", "breaking down", "falling apart",
        "everything is hopeless", "completely alone", "nobody cares about me",
        "i'm drowning", "im drowning", "can't take this anymore", "cant take this anymore"
    };

    private static readonly string[] IndirectEuphemisms =
    {
        "unalive myself", "unalive", "permanent sleep", "go to sleep forever",
        "not wake up tomorrow", "disappear forever", "end it all"
    };

    public SafetyGateService(ILogger<SafetyGateService> logger)
    {
        _logger = logger;
    }

    // Keep ctor overload so existing DI/tests that inject EmotionalIntelligence still compile
    public SafetyGateService(EmotionalIntelligence _, ILogger<SafetyGateService> logger)
        : this(logger)
    {
    }

    public SafetyAssessment Assess(string? userMessage, ConversationContext? context = null)
    {
        var assessment = new SafetyAssessment();
        if (string.IsNullOrWhiteSpace(userMessage))
            return assessment;

        var raw = userMessage.Trim();
        var m = raw.ToLowerInvariant();

        // Non-self / non-literal contexts should not trigger crisis protocol
        if (IsLikelyNonLiteralOrThirdParty(m))
        {
            assessment.Level = SafetyLevel.Normal;
            assessment.Reason = "Treated as non-literal, quoted, joking, hypothetical, or third-person — no crisis escalation.";
            assessment.Guidance = "Emotion-aware companion can continue; do not escalate safety.";
            return assessment;
        }

        if (ContainsAny(m, ImmediateDanger) || ContainsAny(m, IndirectEuphemisms.Select(x => x.ToLowerInvariant()).ToArray()))
        {
            // Negation: "I don't want to kill myself"
            if (IsNegatedCrisisPhrase(m))
            {
                assessment.Level = SafetyLevel.ElevatedDistress;
                assessment.Reason = "Crisis-adjacent words present but appear negated — stay careful, do not full-escalate.";
                assessment.Guidance = "Acknowledge carefully. Ask how they are without assuming intent.";
                return assessment;
            }

            assessment.Level = SafetyLevel.ImmediateDanger;
            assessment.BlockNormalCompanionFlow = true;
            assessment.Reason = "Immediate-danger self-harm language (safety classifier, not emotion model).";
            assessment.Guidance =
                "Please contact local emergency services or a crisis helpline now. " +
                "NeuroSync can stay with you as a companion, but real human help matters most.";
            assessment.CrisisResourceHint =
                "If you are in the UK: Samaritans 116 123. Elsewhere: find a local crisis line.";
            _logger.LogWarning("SafetyGate ImmediateDanger (message length={Len})", raw.Length);
            return assessment;
        }

        if (ContainsAny(m, PotentialCrisis))
        {
            if (IsNegatedCrisisPhrase(m))
            {
                assessment.Level = SafetyLevel.ElevatedDistress;
                assessment.Reason = "Potential-crisis phrasing appears negated.";
                return assessment;
            }

            assessment.Level = SafetyLevel.PotentialCrisis;
            assessment.BlockNormalCompanionFlow = true;
            assessment.Reason = "Potential crisis language (safety classifier).";
            assessment.Guidance =
                "Please reach someone you trust or a crisis helpline. You don't have to face this alone.";
            assessment.CrisisResourceHint = "Prefer human support over AI when distress is this high.";
            return assessment;
        }

        var escalating = context != null && IsEscalatingDistress(context);
        if (ContainsAny(m, ElevatedDistress) || escalating)
        {
            assessment.Level = SafetyLevel.ElevatedDistress;
            assessment.Reason = escalating
                ? "Elevated distress across recent turns (conversation escalation)."
                : "Elevated distress language.";
            assessment.Guidance =
                "Offer listen / calm / talk options. Do not diagnose. Encourage real-world support if it continues.";
            return assessment;
        }

        return assessment;
    }

    private static bool IsEscalatingDistress(ConversationContext context)
    {
        if (context.ConversationCount < 4) return false;
        var last = context.History.TakeLast(4).ToList();
        if (last.Count < 4) return false;
        var heavy = last.Count(e =>
            e.DetectedEmotion?.Emotion is EmotionType.Sad or EmotionType.Anxious or EmotionType.Angry);
        return heavy >= 3;
    }

    private static bool IsLikelyNonLiteralOrThirdParty(string m)
    {
        // Quotes / lyrics / jokes / hypotheticals / third person
        if (m.Contains("lyrics") || m.Contains("this song says") || m.Contains("the song goes"))
            return true;
        if (m.Contains("just kidding") || m.Contains("jk ") || m.EndsWith(" jk") || m.Contains("lol joke")
            || m.Contains("for a joke") || m.Contains("dark humour") || m.Contains("dark humor"))
            return true;
        if (m.Contains("hypothetically") || m.Contains("asking for a friend") || m.Contains("in a movie")
            || m.Contains("in a story") || m.Contains("character says") || m.Contains("what if someone"))
            return true;
        if (m.StartsWith("\"") && m.EndsWith("\""))
            return true;
        if (m.Contains("he wants to die") || m.Contains("she wants to die") || m.Contains("they want to die")
            || m.Contains("my friend wants to") || m.Contains("someone i know"))
            return true;
        // Quoted suicide word without first-person intent
        if (m.Contains("prevention") || m.Contains("statistics") || m.Contains("wikipedia")
            || m.Contains("for class") || m.Contains("research on") || m.Contains("essay about"))
            return true;
        return false;
    }

    private static bool IsNegatedCrisisPhrase(string m)
    {
        var negationWindows = new[]
        {
            "don't want to die", "dont want to die", "not going to kill", "won't kill myself",
            "wont kill myself", "don't want to kill", "dont want to kill",
            "not suicidal", "i'm not going to hurt", "im not going to hurt",
            "never kill myself", "wouldn't hurt myself", "wouldnt hurt myself"
        };
        return negationWindows.Any(m.Contains);
    }

    private static bool ContainsAny(string text, string[] cues) =>
        cues.Any(c => text.Contains(c, StringComparison.Ordinal));
}
