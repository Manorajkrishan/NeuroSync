using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// V1 template companion — no LLM dependency.
/// </summary>
public class TemplateCompanionProvider : ICompanionProvider
{
    public string ProviderId => "template-v1";

    public string Generate(
        EmotionResult emotion,
        CompanionInteractionMode mode,
        SafetyAssessment safety,
        UncertaintyLevel uncertainty,
        string? userMessage,
        string? displayName)
    {
        if (safety.BlockNormalCompanionFlow)
            return safety.Guidance;

        var name = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        var prefix = name == null ? "" : $"{name} — ";

        if (uncertainty is UncertaintyLevel.InsufficientEvidence or UncertaintyLevel.ConflictingSignals
            or UncertaintyLevel.Uncertain)
        {
            var hedge = uncertainty switch
            {
                UncertaintyLevel.InsufficientEvidence =>
                    "I'm not sure I have enough to go on yet.",
                UncertaintyLevel.ConflictingSignals =>
                    "I'm getting mixed signals — I may be reading this wrong.",
                _ => "I may be reading this wrong."
            };

            return mode switch
            {
                CompanionInteractionMode.Listen =>
                    $"{prefix}{hedge} I'm still here if you want to say more, or we can leave it.",
                CompanionInteractionMode.ProblemSolving =>
                    $"{prefix}{hedge} If you want, tell me the concrete problem and we can take one step.",
                _ => $"{prefix}{hedge} Want to clarify how you're feeling, or talk about something else?"
            };
        }

        var understood = string.IsNullOrWhiteSpace(emotion.UnderstoodAs)
            ? "Thanks for telling me."
            : emotion.UnderstoodAs!;

        return mode switch
        {
            CompanionInteractionMode.Listen =>
                $"{prefix}{understood} I'm listening — no pressure to fix anything.",
            CompanionInteractionMode.ProblemSolving =>
                $"{prefix}{understood} Want to break this into one small next step?",
            CompanionInteractionMode.Focus =>
                $"{prefix}Focus mode. I can keep things simple so you can work. Need Quiet Mode?",
            CompanionInteractionMode.Calm =>
                $"{prefix}{understood} Want a quiet minute, or just company?",
            CompanionInteractionMode.Companion =>
                $"{prefix}I'm around — we can talk about anything, heavy or not.",
            _ => $"{prefix}{understood} What's on your mind?"
        };
    }
}
