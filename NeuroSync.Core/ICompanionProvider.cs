namespace NeuroSync.Core;

/// <summary>
/// Companion response provider — DecisionEngine stays model-agnostic.
/// V1 uses templates; later swap in LlmCompanionProvider without rewriting the OS.
/// </summary>
public interface ICompanionProvider
{
    string ProviderId { get; }

    string Generate(
        EmotionResult emotion,
        CompanionInteractionMode mode,
        SafetyAssessment safety,
        UncertaintyLevel uncertainty,
        string? userMessage,
        string? displayName);
}
