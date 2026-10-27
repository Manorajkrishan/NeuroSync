using System.Text.RegularExpressions;
using NeuroSync.Core;

namespace NeuroSync.Api.Services;

public sealed record ResponseQualityResult(
    double Score,
    IReadOnlyList<string> Flags)
{
    public bool IsAcceptable(double threshold = 0.65) => Score >= threshold;
}

/// <summary>
/// Deterministic product-quality gate for companion replies.
/// This is not an emotion model; it checks conversational hygiene and policy alignment.
/// </summary>
public sealed class ResponseQualityService
{
    private static readonly string[] RoboticPhrases =
    {
        "not sure i have enough",
        "clarify how you're feeling",
        "i may be reading this wrong",
        "as an ai language model",
        "based on my analysis of your emotion",
        "my confidence is"
    };

    private static readonly string[] PromptLeakPhrases =
    {
        "server-supplied structured context",
        "begin untrusted recent turns",
        "end untrusted recent turns",
        "policyguidance",
        "system prompt"
    };

    private static readonly Regex DiagnosticClaimRx = new(
        @"\b(you have|you are suffering from|you definitely have)\s+(depression|anxiety disorder|bipolar|ptsd|adhd)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public ResponseQualityResult Evaluate(CompanionContext context, CompanionReply reply)
    {
        var flags = new List<string>();
        var text = (reply.Message ?? "").Trim();
        if (text.Length == 0)
            return new ResponseQualityResult(0, new[] { "empty_reply" });

        var lower = text.ToLowerInvariant();
        var score = 1.0;

        if (text.Length > 1200)
        {
            score -= 0.12;
            flags.Add("too_long");
        }

        var questionCount = text.Count(c => c == '?');
        if (questionCount > 1)
        {
            score -= Math.Min(0.25, 0.10 * (questionCount - 1));
            flags.Add("too_many_questions");
        }

        if (RoboticPhrases.Any(lower.Contains))
        {
            score -= 0.40;
            flags.Add("robotic_uncertainty");
        }

        if (PromptLeakPhrases.Any(lower.Contains))
        {
            score -= 0.70;
            flags.Add("prompt_leak");
        }

        if (DiagnosticClaimRx.IsMatch(text))
        {
            score -= 0.60;
            flags.Add("diagnostic_claim");
        }

        if (Regex.IsMatch(lower, @"\b\d{1,3}%\s*confidence\b"))
        {
            score -= 0.50;
            flags.Add("confidence_leak");
        }

        if (context.Safety.BlockNormalCompanionFlow &&
            !string.Equals(reply.ProviderId, "safety-protocol", StringComparison.OrdinalIgnoreCase))
        {
            score -= 0.60;
            flags.Add("safety_provider_mismatch");
        }

        if (context.Intent is UserIntent.Greeting or UserIntent.CasualConversation && text.Length > 600)
        {
            score -= 0.10;
            flags.Add("casual_overlong");
        }

        if (ConversationLanguageService.IsShortCauseAnswer(context.CurrentMessage))
        {
            var topic = context.ThreadState.Topic;
            if (topic == "fatigue" && !ContainsAny(lower, "work", "sleep", "tired", "drain", "load", "energy", "both"))
            {
                score -= 0.20;
                flags.Add("thread_continuity_miss");
            }
            else if (topic == "stress" && !ContainsAny(lower, "pressure", "work", "study", "stress", "deadline", "people"))
            {
                score -= 0.20;
                flags.Add("thread_continuity_miss");
            }
        }

        var wantsTanglish = context.Locale is "ta" or "tanglish"
                           || ConversationLanguageService.LooksTanglish(context.CurrentMessage);
        if (wantsTanglish &&
            !ConversationLanguageService.LooksTanglish(text) &&
            !ContainsAny(lower, "da", "seri", "sollu", "enna", "epdi", "iruk"))
        {
            score -= 0.08;
            flags.Add("locale_style_miss");
        }

        return new ResponseQualityResult(Math.Round(Math.Clamp(score, 0, 1), 3), flags);
    }

    private static bool ContainsAny(string text, params string[] cues) =>
        cues.Any(c => text.Contains(c, StringComparison.Ordinal));
}
