using System.Text.RegularExpressions;
using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Routes user messages to intents so greetings aren't treated as emotion diagnoses.
/// </summary>
public class IntentRouterService
{
    private static readonly Regex GreetingRx = new(
        @"^(hi+|hii+|hello|hey+|yo|sup|hiya|hola|good\s*(morning|afternoon|evening|night)|gm|gn)\b[!?. ]*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HowAreYouRx = new(
        @"\b(how are you|how's it going|how r you|how r u|whats up|what's up|wyd)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public UserIntent Detect(string? message, ConversationContext? context = null, SafetyLevel safety = SafetyLevel.Normal)
    {
        if (safety is SafetyLevel.PotentialCrisis or SafetyLevel.ImmediateDanger)
            return UserIntent.SafetySensitive;

        if (string.IsNullOrWhiteSpace(message))
            return UserIntent.Unknown;

        var t = message.Trim();
        var lower = t.ToLowerInvariant();
        var stripped = new string(lower.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray()).Trim();

        if (EmotionalIntelligence.IsIoTRequest(t))
            return UserIntent.EnvironmentAction;

        if (ContainsAny(lower, "just listen", "listen to me", "don't advise", "dont advise", "i just need to vent", "let me vent"))
            return UserIntent.ListeningRequest;

        if (ContainsAny(lower, "what should i do", "help me decide", "advice", "how do i fix", "make a plan", "problem solve"))
            return UserIntent.AdviceRequest;

        // Social "how are you" before generic greeting/small-talk bucket
        if (HowAreYouRx.IsMatch(lower))
            return UserIntent.CasualConversation;

        if (GreetingRx.IsMatch(stripped) || (EmotionalIntelligence.IsGreetingOrSmallTalk(t) && stripped.Split(' ').Length <= 3
            && !HowAreYouRx.IsMatch(lower)))
            return UserIntent.Greeting;

        if (ContainsAny(lower, "i miss ", "miss her", "miss him", "miss them", "i feel", "feeling", "lonely",
                "anxious", "scared", "heartbroken", "crying", "can't cope", "overwhelmed", "depressed",
                "everything feels", "heavier", "i'm tired of", "im tired of"))
            return UserIntent.EmotionalDisclosure;

        if (lower.Contains('?') || lower.StartsWith("what ") || lower.StartsWith("why ") || lower.StartsWith("how ")
            || lower.StartsWith("when ") || lower.StartsWith("where ") || lower.StartsWith("who "))
            return UserIntent.Question;

        if (ContainsAny(lower, "remind me", "set a timer", "help me study", "focus mode", "todo"))
            return UserIntent.TaskRequest;

        // Very short acknowledgements
        if (stripped is "ok" or "okay" or "k" or "lol" or "lmao" or "yeah" or "yea" or "yep" or "no" or "nah"
            or "hmm" or "hm" or "fine" or "nothing" or "nothing da" or "idk")
            return UserIntent.CasualConversation;

        if (context?.History.Count > 0)
        {
            var last = context.History.LastOrDefault()?.UserMessage?.ToLowerInvariant() ?? "";
            if (last.Contains("miss") || last.Contains("sad") || last.Contains("lonely"))
            {
                // Follow-up in emotional thread
                if (stripped.Split(' ').Length <= 8)
                    return UserIntent.EmotionalDisclosure;
            }
        }

        return UserIntent.Unknown;
    }

    private static bool ContainsAny(string text, params string[] cues) =>
        cues.Any(c => text.Contains(c, StringComparison.Ordinal));
}
