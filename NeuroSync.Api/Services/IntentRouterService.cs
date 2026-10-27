using System.Text.RegularExpressions;
using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Routes user messages to intents so greetings and short conversation turns are not treated as emotion diagnoses.
/// Uses the current ephemeral thread when available so short follow-ups keep their meaning.
/// </summary>
public class IntentRouterService
{
    private static readonly Regex GreetingRx = new(
        @"^(hi+|hii+|hai+|hello|hey+|yo|sup|hiya|hola|vanakkam|good\s*(morning|afternoon|evening|night)|gm|gn)\b[!?. ]*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public UserIntent Detect(
        string? message,
        ConversationContext? context = null,
        SafetyLevel safety = SafetyLevel.Normal,
        IReadOnlyList<CompanionConversationTurn>? recentTurns = null,
        CompanionThreadState? threadState = null)
    {
        if (safety is SafetyLevel.PotentialCrisis or SafetyLevel.ImmediateDanger)
            return UserIntent.SafetySensitive;

        if (string.IsNullOrWhiteSpace(message))
            return UserIntent.Unknown;

        var t = message.Trim();
        var lower = t.ToLowerInvariant();
        var stripped = ConversationLanguageService.Normalize(t);
        var wordCount = stripped.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        if (EmotionalIntelligence.IsIoTRequest(t))
            return UserIntent.EnvironmentAction;

        if (ContainsAny(lower, "just listen", "listen to me", "don't advise", "dont advise",
                "i just need to vent", "let me vent", "just hear me out"))
            return UserIntent.ListeningRequest;

        if (ContainsAny(lower, "what should i do", "help me decide", "advice", "how do i fix",
                "make a plan", "problem solve", "what can i do"))
            return UserIntent.AdviceRequest;

        if (ConversationLanguageService.IsHowAreYou(t))
            return UserIntent.CasualConversation;

        if (GreetingRx.IsMatch(stripped) ||
            (EmotionalIntelligence.IsGreetingOrSmallTalk(t) && wordCount <= 3
             && !ConversationLanguageService.IsHowAreYou(t)))
            return UserIntent.Greeting;

        if (ConversationLanguageService.IsPositiveCheckIn(t))
            return UserIntent.CasualConversation;

        if (ConversationLanguageService.HasEmotionalCue(t))
            return UserIntent.EmotionalDisclosure;

        if (lower.Contains('?') || lower.StartsWith("what ") || lower.StartsWith("why ")
            || lower.StartsWith("how ") || lower.StartsWith("when ") || lower.StartsWith("where ")
            || lower.StartsWith("who "))
            return UserIntent.Question;

        if (ContainsAny(lower, "remind me", "set a timer", "help me study", "focus mode", "todo"))
            return UserIntent.TaskRequest;

        if (ConversationLanguageService.IsAcknowledgement(t))
            return UserIntent.CasualConversation;

        // Short answers such as "work", "sleep", or "both" should continue a
        // wellbeing thread rather than restart the conversation as Unknown.
        if (ConversationLanguageService.IsShortCauseAnswer(t)
            && (ConversationLanguageService.PreviousThreadIsWellbeing(recentTurns)
                || threadState?.Topic is "fatigue" or "stress" or "loneliness" or "work" or "study" or "relationship"))
            return UserIntent.EmotionalDisclosure;

        // A short phrase following an emotional disclosure is usually a continuation.
        if (wordCount <= 8 &&
            (ConversationLanguageService.PreviousThreadIsWellbeing(recentTurns)
             || threadState?.LastIntent == UserIntent.EmotionalDisclosure
             || !string.IsNullOrWhiteSpace(threadState?.PendingFollowUp)))
            return UserIntent.EmotionalDisclosure;

        if (context?.History.Count > 0)
        {
            var last = context.History.LastOrDefault()?.UserMessage ?? "";
            if (ConversationLanguageService.HasEmotionalCue(last) && wordCount <= 8)
                return UserIntent.EmotionalDisclosure;
        }

        // Natural social phrasing that is not a question but is clearly conversational.
        if (wordCount <= 8 && ContainsAny(stripped, "and u", "and you", "what about you", "same here"))
            return UserIntent.CasualConversation;

        return UserIntent.Unknown;
    }

    private static bool ContainsAny(string text, params string[] cues) =>
        cues.Any(c => text.Contains(c, StringComparison.Ordinal));
}
