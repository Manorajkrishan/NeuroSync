namespace NeuroSync.Core;

/// <summary>
/// Companion response provider — DecisionEngine stays model-agnostic.
/// SafetyGate / DecisionEngine remain authoritative; providers never decide safety or IoT.
/// V1 uses templates; LlmCompanionProvider may swap in with template fallback.
/// </summary>
public interface ICompanionProvider
{
    string ProviderId { get; }

    Task<CompanionReply> GenerateAsync(
        CompanionContext context,
        CancellationToken cancellationToken = default);
}
