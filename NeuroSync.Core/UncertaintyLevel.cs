namespace NeuroSync.Core;

/// <summary>
/// How confident we are in emotion/signal estimates.
/// Never treat a single percentage as ground truth.
/// </summary>
public enum UncertaintyLevel
{
    /// <summary>Clear explicit cues or strong agreement across signals.</summary>
    HighConfidence = 0,

    /// <summary>Some evidence, but ambiguous — companion should hedge.</summary>
    Uncertain = 1,

    /// <summary>Too little signal to claim an emotional state.</summary>
    InsufficientEvidence = 2,

    /// <summary>Signals disagree (e.g. happy lexicon + sad markers).</summary>
    ConflictingSignals = 3
}
