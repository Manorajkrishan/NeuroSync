namespace NeuroSync.Core;

/// <summary>
/// What the user is trying to do — detected BEFORE emotional claims drive the reply.
/// </summary>
public enum UserIntent
{
    Greeting,
    CasualConversation,
    EmotionalDisclosure,
    AdviceRequest,
    ListeningRequest,
    Question,
    EnvironmentAction,
    TaskRequest,
    SafetySensitive,
    Unknown
}
