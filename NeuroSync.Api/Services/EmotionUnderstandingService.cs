using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Deepens emotion understanding: corrects clear language, intensity, and likely cause.
/// ML detects — this layer understands like a best friend reading between the lines.
/// </summary>
public class EmotionUnderstandingService
{
    private readonly ILogger<EmotionUnderstandingService> _logger;

    public EmotionUnderstandingService(ILogger<EmotionUnderstandingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Refine ML prediction into a richer, more accurate emotional read.
    /// </summary>
    public EmotionResult Understand(EmotionResult mlResult, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            mlResult.UnderstoodAs = "You're quiet right now — I'm here when you're ready.";
            mlResult.Intensity = "mild";
            return mlResult;
        }

        var lower = text.Trim().ToLowerInvariant();
        var result = new EmotionResult
        {
            Emotion = mlResult.Emotion,
            Confidence = mlResult.Confidence,
            OriginalText = text,
            Timestamp = DateTime.UtcNow
        };

        // 1) Explicit self-labels beat the model ("I feel sad")
        var explicitEmotion = DetectExplicitEmotion(lower);
        if (explicitEmotion.HasValue)
        {
            result.Emotion = explicitEmotion.Value;
            result.Confidence = Math.Max(mlResult.Confidence, 0.92f);
        }

        // 2) Greetings / identity statements are Neutral conversation, not Happy
        if (EmotionalIntelligence.IsGreetingOrSmallTalk(lower) ||
            lower.StartsWith("my name is") || lower.StartsWith("call me ") ||
            lower.StartsWith("i'm ") && lower.Split(' ').Length <= 4 && !HasFeelingWords(lower))
        {
            // Only force Neutral if they didn't also express a feeling
            if (!HasFeelingWords(lower) && explicitEmotion == null)
            {
                result.Emotion = EmotionType.Neutral;
                result.Confidence = 0.95f;
            }
        }

        // 3) Strong lexical signals when ML is uncertain or clearly wrong
        var lexical = DetectLexicalEmotion(lower);
        if (lexical.HasValue)
        {
            if (explicitEmotion == null && (mlResult.Confidence < 0.65f || Conflicts(mlResult.Emotion, lexical.Value, lower)))
            {
                result.Emotion = lexical.Value;
                result.Confidence = Math.Max(mlResult.Confidence, 0.78f);
            }
            else if (lexical.Value != result.Emotion)
            {
                result.SecondaryEmotion = lexical.Value;
            }
        }

        // 4) Intensity
        result.Intensity = DetectIntensity(lower);

        // 5) Likely cause
        result.LikelyCause = DetectLikelyCause(lower);

        // 6) Uncertain multi-signal estimates (vision: not a single label)
        result.SignalEstimates = BuildSignalEstimates(lower, result);

        // 7) Human-readable understanding
        result.UnderstoodAs = BuildUnderstoodAs(result);
        result.Disclaimer =
            "Emotion signals are uncertain estimates for wellbeing support. NeuroSync does not diagnose mental illness or replace professional care.";

        _logger.LogInformation(
            "Understood emotion: {Emotion} ({Intensity}, {Confidence:P0}) cause={Cause} | {Text}",
            result.Emotion, result.Intensity, result.Confidence, result.LikelyCause ?? "—",
            text.Length > 60 ? text[..60] + "…" : text);

        return result;
    }

    private static Dictionary<string, float> BuildSignalEstimates(string lower, EmotionResult result)
    {
        var signals = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        void Set(string key, float value)
        {
            if (value <= 0) return;
            signals[key] = Math.Clamp(value, 0f, 1f);
        }

        Set(result.Emotion.ToString(), result.Confidence);
        if (result.SecondaryEmotion.HasValue)
            Set(result.SecondaryEmotion.Value.ToString(), Math.Max(0.35f, result.Confidence * 0.7f));

        if (ContainsAny(lower, "lonely", "alone", "no one", "nobody", "isolated"))
            Set("Loneliness", 0.72f);
        if (ContainsAny(lower, "stress", "stressed", "pressure", "overwhelmed", "anxious", "anxiety"))
            Set("Stress", 0.65f);
        if (ContainsAny(lower, "tired", "exhausted", "drained", "fatigue", "no energy", "slept"))
            Set("Fatigue", 0.6f);
        if (ContainsAny(lower, "hopeless", "pointless", "no point", "give up", "worthless"))
            Set("HopelessnessLanguage", 0.45f);
        if (ContainsAny(lower, "tired of everything", "sick of everything", "can't do this anymore"))
        {
            Set("Fatigue", Math.Max(signals.GetValueOrDefault("Fatigue"), 0.7f));
            Set("Stress", Math.Max(signals.GetValueOrDefault("Stress"), 0.55f));
        }

        return signals;
    }

    private static bool HasFeelingWords(string lower) =>
        lower.Contains("feel") || lower.Contains("sad") || lower.Contains("happy") ||
        lower.Contains("anxious") || lower.Contains("angry") || lower.Contains("worried") ||
        lower.Contains("stressed") || lower.Contains("lonely") || lower.Contains("scared") ||
        lower.Contains("frustrated") || lower.Contains("depressed") || lower.Contains("excited");

    private static EmotionType? DetectExplicitEmotion(string lower)
    {
        // "i feel X" / "i'm feeling X" / "i am X"
        var maps = new (string[] cues, EmotionType emotion)[]
        {
            (new[] { "feel sad", "feeling sad", "i'm sad", "i am sad", "so sad", "really sad", "depressed", "down today", "feeling down", "not okay", "not ok", "not good" }, EmotionType.Sad),
            (new[] { "feel happy", "feeling happy", "i'm happy", "i am happy", "so happy", "really happy", "feeling great", "feeling good" }, EmotionType.Happy),
            (new[] { "feel angry", "feeling angry", "i'm angry", "i am angry", "so angry", "furious", "pissed", "mad at" }, EmotionType.Angry),
            (new[] { "feel anxious", "feeling anxious", "i'm anxious", "worried", "nervous", "panic", "overthinking", "stressed out", "feel stressed", "anxiety" }, EmotionType.Anxious),
            (new[] { "feel calm", "feeling calm", "i'm calm", "peaceful", "relaxed", "at peace" }, EmotionType.Calm),
            (new[] { "feel excited", "feeling excited", "i'm excited", "so excited", "pumped", "thrilled" }, EmotionType.Excited),
            (new[] { "feel frustrated", "feeling frustrated", "i'm frustrated", "so frustrated", "fed up", "this is annoying" }, EmotionType.Frustrated),
            (new[] { "feel lonely", "feeling lonely", "i'm lonely", "i am lonely", "so alone", "nobody cares" }, EmotionType.Sad),
            (new[] { "miss my", "i miss ", "missing my" }, EmotionType.Sad),
        };

        foreach (var (cues, emotion) in maps)
        {
            if (cues.Any(c => lower.Contains(c)))
                return emotion;
        }
        return null;
    }

    private static EmotionType? DetectLexicalEmotion(string lower)
    {
        var scores = new Dictionary<EmotionType, int>();
        void Add(EmotionType e, int w) => scores[e] = scores.GetValueOrDefault(e) + w;

        if (ContainsAny(lower, "sad", "cry", "tears", "heartbroken", "lonely", "empty", "hopeless", "worthless")) Add(EmotionType.Sad, 2);
        if (ContainsAny(lower, "happy", "glad", "joy", "smile", "grateful", "blessed", "awesome")) Add(EmotionType.Happy, 2);
        if (ContainsAny(lower, "angry", "rage", "hate", "furious", "annoyed", "irritated")) Add(EmotionType.Angry, 2);
        if (ContainsAny(lower, "anxious", "worry", "worried", "nervous", "panic", "stress", "scared", "afraid", "overwhelmed")) Add(EmotionType.Anxious, 2);
        if (ContainsAny(lower, "calm", "peace", "relaxed", "chill", "serene")) Add(EmotionType.Calm, 1);
        if (ContainsAny(lower, "excited", "thrilled", "hyped", "can't wait", "amazing news")) Add(EmotionType.Excited, 2);
        if (ContainsAny(lower, "frustrated", "stuck", "blocked", "why won't", "keeps failing", "fed up")) Add(EmotionType.Frustrated, 2);
        if (ContainsAny(lower, "failed", "fail", "exam", "rejected", "broke up")) Add(EmotionType.Sad, 1);

        if (scores.Count == 0) return null;
        return scores.OrderByDescending(kv => kv.Value).First().Key;
    }

    private static bool Conflicts(EmotionType ml, EmotionType lexical, string lower)
    {
        // Model said Happy but text is clearly negative
        if (ml == EmotionType.Happy && lexical is EmotionType.Sad or EmotionType.Angry or EmotionType.Anxious or EmotionType.Frustrated)
            return true;
        if (ml == EmotionType.Neutral && lexical != EmotionType.Neutral && HasFeelingWords(lower))
            return true;
        return false;
    }

    private static string DetectIntensity(string lower)
    {
        if (ContainsAny(lower, "extremely", "so so", "can't take", "unbearable", "worst", "dying inside", "completely", "absolutely devastated", "panic attack"))
            return "intense";
        if (ContainsAny(lower, "really", "so ", "very", "terribly", "deeply", "overwhelmed", "a lot"))
            return "intense";
        if (ContainsAny(lower, "a bit", "a little", "slightly", "kinda", "kind of", "somewhat", "mildly"))
            return "mild";
        return "moderate";
    }

    private static string? DetectLikelyCause(string lower)
    {
        if (ContainsAny(lower, "exam", "test", "grade", "study", "university", "college", "assignment"))
            return "school / exams";
        if (ContainsAny(lower, "work", "boss", "job", "colleague", "office", "deadline"))
            return "work";
        if (ContainsAny(lower, "family", "mom", "dad", "parents", "brother", "sister"))
            return "family";
        if (ContainsAny(lower, "friend", "relationship", "girlfriend", "boyfriend", "breakup", "broke up", "partner"))
            return "relationships";
        if (ContainsAny(lower, "alone", "lonely", "no one", "nobody"))
            return "loneliness";
        if (ContainsAny(lower, "money", "bills", "broke", "debt", "rent"))
            return "money stress";
        if (ContainsAny(lower, "health", "sick", "pain", "tired", "sleep", "insomnia"))
            return "health / energy";
        if (ContainsAny(lower, "miss", "far away", "distance"))
            return "missing someone";
        return null;
    }

    private static string BuildUnderstoodAs(EmotionResult r)
    {
        var intensity = r.Intensity switch
        {
            "intense" => "really ",
            "mild" => "a little ",
            _ => ""
        };

        var emotionWord = r.Emotion switch
        {
            EmotionType.Sad => "sad",
            EmotionType.Happy => "happy",
            EmotionType.Angry => "angry",
            EmotionType.Anxious => "anxious",
            EmotionType.Calm => "calm",
            EmotionType.Excited => "excited",
            EmotionType.Frustrated => "frustrated",
            _ => "okay / neutral"
        };

        var line = $"I understand you're feeling {intensity}{emotionWord}";
        if (!string.IsNullOrEmpty(r.LikelyCause))
            line += $" — this seems connected to {r.LikelyCause}";
        if (r.SecondaryEmotion.HasValue && r.SecondaryEmotion != r.Emotion)
            line += $", with some {r.SecondaryEmotion.Value.ToString().ToLowerInvariant()} mixed in";
        return line + ".";
    }

    private static bool ContainsAny(string text, params string[] words) =>
        words.Any(w => text.Contains(w, StringComparison.Ordinal));
}
