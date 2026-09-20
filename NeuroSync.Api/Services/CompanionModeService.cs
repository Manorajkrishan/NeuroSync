using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Resolves companion interaction mode from explicit user request or gentle inference.
/// </summary>
public class CompanionModeService
{
    public CompanionInteractionMode Resolve(string? userMessage, EmotionType emotion, SafetyLevel safety)
    {
        if (safety is SafetyLevel.PotentialCrisis or SafetyLevel.ImmediateDanger)
            return CompanionInteractionMode.Listen;

        var m = (userMessage ?? string.Empty).Trim().ToLowerInvariant();

        if (ContainsAny(m, "just listen", "don't talk much", "dont talk much", "listen mode", "only listen"))
            return CompanionInteractionMode.Listen;
        if (ContainsAny(m, "help me solve", "what should i do", "break it down", "problem solving", "make a plan"))
            return CompanionInteractionMode.ProblemSolving;
        if (ContainsAny(m, "focus mode", "need to focus", "help me focus", "deep work", "no distractions"))
            return CompanionInteractionMode.Focus;
        if (ContainsAny(m, "calm mode", "help me calm", "breathe", "grounding", "relax mode", "quiet mode"))
            return CompanionInteractionMode.Calm;
        if (ContainsAny(m, "just chat", "talk mode", "let's talk", "lets talk", "conversation"))
            return CompanionInteractionMode.Talk;
        if (ContainsAny(m, "companion mode", "just hang out", "distract me", "keep me company"))
            return CompanionInteractionMode.Companion;

        // Gentle defaults from emotion — never force therapy tone
        return emotion switch
        {
            EmotionType.Sad or EmotionType.Angry => CompanionInteractionMode.Listen,
            EmotionType.Anxious or EmotionType.Frustrated => CompanionInteractionMode.Calm,
            EmotionType.Happy or EmotionType.Excited => CompanionInteractionMode.Companion,
            EmotionType.Calm => CompanionInteractionMode.Focus,
            _ => CompanionInteractionMode.Talk
        };
    }

    public string ShapeMessage(string baseMessage, CompanionInteractionMode mode, string? displayName = null)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        var prefix = name == null ? "" : $"{name} — ";

        return mode switch
        {
            CompanionInteractionMode.Listen =>
                $"{prefix}I'm here. No pressure to fix anything. {TrimLead(baseMessage)} If you want silence, that's okay too.",
            CompanionInteractionMode.ProblemSolving =>
                $"{prefix}{TrimLead(baseMessage)} Want to break this into one small next step together?",
            CompanionInteractionMode.Focus =>
                $"{prefix}Focus mode. {TrimLead(baseMessage)} I can keep things simple so you can work.",
            CompanionInteractionMode.Calm =>
                $"{prefix}{TrimLead(baseMessage)} Want a quiet minute, slower breathing, or just company?",
            CompanionInteractionMode.Companion =>
                $"{prefix}{TrimLead(baseMessage)} I'm around — we can talk about anything, or nothing heavy.",
            _ => $"{prefix}{TrimLead(baseMessage)}"
        };
    }

    public string ModeChoicePrompt(CompanionInteractionMode mode) => mode switch
    {
        CompanionInteractionMode.Listen =>
            "That sounds rough. Do you want me to listen, help you work through it, or distract you?",
        CompanionInteractionMode.Calm =>
            "Want quiet/calm mode, a short grounding pause, or to talk it out?",
        CompanionInteractionMode.ProblemSolving =>
            "Want a plan, a single next step, or just to vent first?",
        _ => "Want me to listen, help solve it, or switch to quiet mode?"
    };

    private static bool ContainsAny(string text, params string[] cues) =>
        cues.Any(c => text.Contains(c, StringComparison.Ordinal));

    private static string TrimLead(string message) =>
        string.IsNullOrWhiteSpace(message) ? "I'm with you." : message.Trim();
}
