using System.Text.RegularExpressions;

namespace NeuroSync.Api.Services;

/// <summary>
/// Lightweight conversational semantics for natural English + common Tanglish phrasing.
/// This is not a translation engine and never makes safety decisions.
/// </summary>
public static class ConversationLanguageService
{
    private static readonly Regex SpaceRx = new(@"\s+", RegexOptions.Compiled);

    private static readonly string[] TanglishMarkers =
    {
        "romba", "semma", "irukku", "iruku", "iruken", "irukken", "illa", "illai",
        "enna", "epdi", "eppidi", "nee ", "naan ", "neenga", "thaniya", "yarum", "yaarum",
        "thoongala", "thookam", "manasu", "kashtama", "kastama", "seri illa", "sari illa",
        "aama", "ama ", "venum", "vendam"
    };

    private static readonly string[] FatigueCues =
    {
        "tired", "exhausted", "drained", "worn out", "burnt out", "burned out",
        "no energy", "zero energy", "low energy", "fatigued", "barely slept", "slept badly",
        "romba tired", "semma tired", "energy illa", "thookam varala", "sleep varala",
        "nalla thoongala", "thoonga mudiyala"
    };

    private static readonly string[] StressCues =
    {
        "stress", "stressed", "pressure", "overwhelmed", "overwhelming", "anxious", "anxiety",
        "worried", "worry", "nervous", "panic", "scared", "afraid",
        "tension ah", "tension-a", "stress ah", "stress-a", "pressure ah", "pressure-a",
        "bayama", "bayam", "romba tension", "romba stress"
    };

    private static readonly string[] LonelinessCues =
    {
        "lonely", "alone", "no one", "nobody", "completely alone",
        "thaniya iruken", "thaniya irukken", "thaniya feel", "yarum illa", "yaarum illa",
        "pesurathuku yarum illa", "pesurathukku yarum illa"
    };

    private static readonly string[] EmotionalCues =
    {
        "i miss ", "miss her", "miss him", "miss them", "i feel", "feeling", "heartbroken",
        "crying", "can't cope", "cant cope", "everything feels", "heavier", "depressed",
        "manasu seri illa", "manasu sari illa", "kashtama irukku", "kastama irukku",
        "feel pannren", "feel panren"
    };

    private static readonly HashSet<string> Acknowledgements = new(StringComparer.Ordinal)
    {
        "ok", "okay", "k", "lol", "lmao", "yeah", "yea", "yep", "yes", "yes pls",
        "yes please", "please", "sure", "of course", "no", "nah", "hmm", "hm", "fine",
        "nothing", "nothing da", "idk", "ama", "aama", "seri", "sari", "seri da", "sari da",
        "ok da", "okay da", "please da"
    };

    private static readonly HashSet<string> ShortCauseAnswers = new(StringComparer.Ordinal)
    {
        "work", "office", "job", "study", "studies", "school", "uni", "university", "exam",
        "exams", "sleep", "people", "family", "relationship", "friends", "money", "both",
        "everything", "all of it", "all", "work da", "office da", "sleep da", "both da"
    };

    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var value = text.Trim().ToLowerInvariant()
            .Replace("’", "'")
            .Replace("‘", "'");

        value = Regex.Replace(value, @"[^\p{L}\p{N}'\s-]", " ");
        value = SpaceRx.Replace(value, " ").Trim();
        return value;
    }

    public static bool LooksTanglish(string? text)
    {
        var value = Normalize(text);
        return ContainsAny(value, TanglishMarkers);
    }

    public static bool IsHowAreYou(string? text)
    {
        var value = Normalize(text);
        return ContainsAny(value,
            "how are you", "how's it going", "how r you", "how r u", "whats up", "what's up", "wyd",
            "nee epdi", "nee eppidi", "neenga epdi", "neenga eppidi", "epdi iruka", "epdi irukka",
            "eppidi iruka", "eppidi irukka");
    }

    public static bool IsPositiveCheckIn(string? text)
    {
        var value = Normalize(text);
        return ContainsAny(value,
            "i am good", "i'm good", "im good", "doing good", "doing well", "i am fine", "i'm fine",
            "im fine", "good and u", "good and you", "fine and u", "fine and you",
            "nalla iruken", "nalla irukken", "naan nalla iruken", "naan nalla irukken",
            "super ah iruken", "super ah irukken");
    }

    public static bool IsAcknowledgement(string? text) => Acknowledgements.Contains(Normalize(text));

    public static bool IsShortCauseAnswer(string? text) => ShortCauseAnswers.Contains(Normalize(text));

    public static bool HasFatigueCue(string? text) => ContainsAny(Normalize(text), FatigueCues);

    public static bool HasStressCue(string? text) => ContainsAny(Normalize(text), StressCues);

    public static bool HasLonelinessCue(string? text) => ContainsAny(Normalize(text), LonelinessCues);

    public static bool HasEmotionalCue(string? text)
    {
        var value = Normalize(text);
        return HasFatigueCue(value) || HasStressCue(value) || HasLonelinessCue(value) || ContainsAny(value, EmotionalCues);
    }

    public static bool PreviousThreadIsWellbeing(IReadOnlyList<NeuroSync.Core.CompanionConversationTurn>? turns)
    {
        if (turns == null || turns.Count == 0)
            return false;

        return turns
            .Where(t => string.Equals(t.Role, "user", StringComparison.OrdinalIgnoreCase))
            .TakeLast(2)
            .Any(t => HasEmotionalCue(t.Text));
    }

    public static string PreferredLocale(string? configuredLocale, string? currentMessage)
    {
        var locale = (configuredLocale ?? "").Trim().ToLowerInvariant();
        if (locale is "ta" or "tanglish")
            return locale;
        return LooksTanglish(currentMessage) ? "tanglish" : "en";
    }

    private static bool ContainsAny(string text, params string[] cues) =>
        cues.Any(c => text.Contains(c, StringComparison.Ordinal));
}
