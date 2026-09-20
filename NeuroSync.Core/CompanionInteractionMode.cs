namespace NeuroSync.Core;

/// <summary>
/// How the companion should interact for this turn.
/// User can request a mode explicitly, or NeuroSync can infer a gentle default.
/// </summary>
public enum CompanionInteractionMode
{
    /// <summary>Minimal interruption — acknowledge and hold space.</summary>
    Listen,

    /// <summary>Natural conversation.</summary>
    Talk,

    /// <summary>Break a problem into small next steps.</summary>
    ProblemSolving,

    /// <summary>Reduce distractions / help work.</summary>
    Focus,

    /// <summary>Grounding / calm options.</summary>
    Calm,

    /// <summary>Ordinary companionship — not therapy-flavoured.</summary>
    Companion
}
