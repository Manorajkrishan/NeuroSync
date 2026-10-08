using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Hard rules for what NeuroSync is allowed to say.
/// </summary>
public class ResponsePolicyService
{
    public sealed class PolicyResult
    {
        public bool AllowEmotionClaim { get; init; }
        public bool AllowConfidenceDisplay { get; init; }
        public bool PreferListening { get; init; }
        public bool MaxOneFollowUp { get; init; } = true;
        public string SystemGuidance { get; init; } = string.Empty;
        public List<string> ViolationsBlocked { get; init; } = new();
    }

    public PolicyResult Evaluate(CompanionContext ctx)
    {
        var blocked = new List<string>();
        var allowEmotionClaim = false; // NEVER state inferred emotion as fact in user chat
        var allowConfidence = false;   // NEVER show model % unless debug panel

        if (ctx.Intent is UserIntent.Greeting or UserIntent.CasualConversation or UserIntent.Question)
        {
            blocked.Add("emotion_label_in_chat");
            blocked.Add("confidence_in_chat");
        }

        if (ctx.Uncertainty is UncertaintyLevel.InsufficientEvidence or UncertaintyLevel.ConflictingSignals
            or UncertaintyLevel.Uncertain)
        {
            blocked.Add("strong_emotion_claim");
        }

        var shortMsg = (ctx.UserMessage ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 2;
        if (shortMsg)
            blocked.Add("emotion_from_tiny_message");

        var preferListen = ctx.Intent is UserIntent.EmotionalDisclosure or UserIntent.ListeningRequest
                           || ctx.Mode == CompanionInteractionMode.Listen;

        var guidance =
            "You are NeuroSync, an emotion-aware wellbeing companion — not a doctor or therapist. " +
            "Never diagnose. Never say you know exactly how the user feels. " +
            "Never expose model confidence in normal chat. " +
            "Prefer natural conversation. Ask at most one useful follow-up. " +
            "Respect the active interaction mode. Environment actions need permission. " +
            "Safety policy overrides personality.";

        if (preferListen)
            guidance += " Prioritise listening over advice.";

        return new PolicyResult
        {
            AllowEmotionClaim = allowEmotionClaim,
            AllowConfidenceDisplay = allowConfidence,
            PreferListening = preferListen,
            MaxOneFollowUp = true,
            SystemGuidance = guidance,
            ViolationsBlocked = blocked
        };
    }

    /// <summary>Strip forbidden patterns from a draft reply.</summary>
    public string Sanitize(string draft, PolicyResult policy)
    {
        if (string.IsNullOrWhiteSpace(draft))
            return "I'm here with you. What's on your mind?";

        var text = draft;
        // Remove diagnostic / confidence leakage
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"I (sense|detect|understand you're feeling|can tell you're)[^.!?]*[.!?]?",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"\(\s*\d{1,3}\s*%\s*confidence\s*\)",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"\b\d{1,3}%\s*confidence\b",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s{2,}", " ").Trim();

        if (string.IsNullOrWhiteSpace(text))
            return policy.PreferListening
                ? "I'm listening. Take your time."
                : "Hey — what's going on?";

        return text;
    }
}
