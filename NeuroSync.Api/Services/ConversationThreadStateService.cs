using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Maintains compact, short-lived conversation thread state.
/// It stores topic/follow-up structure only; it is not long-term personal memory.
/// </summary>
public sealed class ConversationThreadStateService
{
    public CompanionThreadState Advance(
        CompanionThreadState? previous,
        string? userMessage,
        UserIntent intent,
        CompanionInteractionMode mode,
        string? assistantReply)
    {
        previous ??= new CompanionThreadState();
        var message = ConversationLanguageService.Normalize(userMessage);
        var topic = InferTopic(message, intent) ?? previous.Topic;
        var pending = InferPendingFollowUp(topic, assistantReply);

        return new CompanionThreadState
        {
            Topic = topic,
            PendingFollowUp = pending,
            LastIntent = intent,
            LastMode = mode,
            TurnCount = Math.Max(0, previous.TurnCount) + 1,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public string? InferTopic(string? userMessage, UserIntent intent = UserIntent.Unknown)
    {
        var m = ConversationLanguageService.Normalize(userMessage);
        if (string.IsNullOrWhiteSpace(m))
            return null;

        if (ConversationLanguageService.HasFatigueCue(m))
            return "fatigue";
        if (ConversationLanguageService.HasStressCue(m))
            return "stress";
        if (ConversationLanguageService.HasLonelinessCue(m))
            return "loneliness";
        if (ContainsAny(m, "work", "office", "job", "boss", "deadline", "workload", "velai"))
            return "work";
        if (ContainsAny(m, "study", "exam", "uni", "university", "school", "assignment", "padippu"))
            return "study";
        if (ContainsAny(m, "relationship", "girlfriend", "boyfriend", "partner", "breakup", "miss her", "miss him"))
            return "relationship";
        if (intent is UserIntent.Greeting or UserIntent.CasualConversation)
            return "casual";
        if (intent == UserIntent.AdviceRequest)
            return "advice";
        return null;
    }

    private static string? InferPendingFollowUp(string? topic, string? assistantReply)
    {
        var reply = (assistantReply ?? "").Trim();
        if (!reply.Contains('?'))
            return null;

        return topic switch
        {
            "fatigue" => "fatigue_cause",
            "stress" => "stress_source",
            "loneliness" => "loneliness_source",
            "work" => "work_source",
            "study" => "study_source",
            "relationship" => "relationship_context",
            _ => "general_followup"
        };
    }

    private static bool ContainsAny(string text, params string[] cues) =>
        cues.Any(c => text.Contains(c, StringComparison.Ordinal));
}
