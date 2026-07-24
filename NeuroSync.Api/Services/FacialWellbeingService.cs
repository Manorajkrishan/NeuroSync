using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Camera-based wellbeing reactions: eye contact + face motion → human, health-focused talk.
/// </summary>
public class FacialWellbeingService
{
    private readonly UserProfileService _profiles;
    private readonly ILogger<FacialWellbeingService> _logger;
    private static readonly Random Rng = new();

    public FacialWellbeingService(UserProfileService profiles, ILogger<FacialWellbeingService> logger)
    {
        _profiles = profiles;
        _logger = logger;
    }

    public AdaptiveResponse BuildReaction(string userId, FacialEmotionRequest request, EmotionType emotion)
    {
        var profile = _profiles.GetOrCreateProfile(userId);
        var name = profile.PreferredName;
        var eye = request.EyeContactScore ?? 0.5f;
        var motion = request.FaceMotionScore ?? 0f;
        var gaze = request.GazeState ?? "unknown";
        var engagement = request.Engagement ?? "steady";

        var message = PickHumanLine(emotion, eye, motion, gaze, engagement, name);
        var healthNudge = PickHealthNudge(emotion, eye, motion, engagement, profile);

        if (!string.IsNullOrEmpty(healthNudge) && Rng.NextDouble() < 0.85)
            message = $"{message} {healthNudge}";

        var response = new AdaptiveResponse
        {
            Emotion = emotion,
            Action = "wellbeing_checkin",
            Message = message,
            Parameters = new Dictionary<string, object>
            {
                ["mode"] = "real_person_wellbeing",
                ["purpose"] = "help you feel healthier — mentally and physically",
                ["eyeContact"] = eye,
                ["faceMotion"] = motion,
                ["gaze"] = gaze,
                ["engagement"] = engagement,
                ["understoodAs"] = BuildUnderstood(emotion, eye, motion, gaze),
                ["healthFocus"] = true
            }
        };

        if (!string.IsNullOrEmpty(request.CueNotes))
            response.Parameters["cueNotes"] = request.CueNotes;

        _logger.LogInformation(
            "Facial wellbeing: {Emotion} eye={Eye:F2} motion={Motion:F2} gaze={Gaze} → reacted",
            emotion, eye, motion, gaze);

        return response;
    }

    private static string BuildUnderstood(EmotionType emotion, float eye, float motion, string gaze)
    {
        var feel = emotion.ToString().ToLowerInvariant();
        var eyeBit = eye > 0.7f ? "you're meeting my gaze"
            : eye < 0.35f ? "your eyes seem elsewhere or heavy"
            : "your attention is drifting a little";
        var moveBit = motion > 0.5f ? "and you're moving around a lot"
            : motion < 0.1f ? "and you're very still"
            : "";
        return $"I'm reading {feel} — {eyeBit} {moveBit}".Trim();
    }

    private static string PickHumanLine(EmotionType emotion, float eye, float motion, string gaze, string engagement, string? name)
    {
        var n = string.IsNullOrEmpty(name) ? "" : $"{name}, ";

        // Eye / motion specific first
        if (gaze is "eyes_closed_or_down" || eye < 0.3f)
        {
            return Pick(new[]
            {
                $"{n}hey — your eyes look tired. Want to pause for a second with me?",
                $"{n}I'm noticing your eyes dropping. Long day?",
                $"{n}You seem drained. No pressure to talk — I'm just here."
            });
        }

        if (engagement == "restless" || motion > 0.55f)
        {
            return Pick(new[]
            {
                $"{n}you look a bit restless. Everything okay?",
                $"{n}I'm sensing some tension in how you're moving. Want to shake it off together — stretch or a few slow breaths?",
                $"{n}your energy feels jumpy. What's on your mind?"
            });
        }

        if (gaze is "looking_left" or "looking_right" && emotion != EmotionType.Happy)
        {
            return Pick(new[]
            {
                $"{n}you seem distracted. I'm still with you whenever you're ready.",
                $"{n}hey — when you look away like that I wonder if something's pulling at you. Want to share?"
            });
        }

        return emotion switch
        {
            EmotionType.Sad => Pick(new[]
            {
                $"{n}I can see it on your face — something feels heavy. I'm right here.",
                $"{n}you look sad. You don't have to fix it alone. Talk to me?",
                $"{n}that expression… I'm with you. What would feel even 1% kinder right now?"
            }),
            EmotionType.Anxious => Pick(new[]
            {
                $"{n}your face looks tense. Let's slow down — in for 4, out for 6?",
                $"{n}I can see the worry. You're safe in this moment. What's the loudest thought?",
                $"{n}anxious energy — I get it. One thing at a time. I'm here."
            }),
            EmotionType.Angry => Pick(new[]
            {
                $"{n}I see the heat in your expression. Vent if you need — I've got you.",
                $"{n}you're upset. That's allowed. What happened?"
            }),
            EmotionType.Happy => Pick(new[]
            {
                $"{n}there it is — that smile. I love seeing you like this.",
                $"{n}you look good. Hold onto that feeling a second longer."
            }),
            EmotionType.Excited => Pick(new[]
            {
                $"{n}your face lit up. Tell me — what's the good news?",
                $"{n}that excitement looks healthy. Channel it into something that cares for you too."
            }),
            EmotionType.Frustrated => Pick(new[]
            {
                $"{n}you look stuck on something. Want to unpack it?",
                $"{n}frustration's written all over — step back with me for a breath?"
            }),
            EmotionType.Calm => Pick(new[]
            {
                $"{n}you look settled. That's a good place for your health — mind and body.",
                $"{n}calm looks good on you. How are you taking care of yourself today?"
            }),
            _ => Pick(new[]
            {
                $"{n}I'm watching with you — how are you feeling in your body right now?",
                $"{n}hey. Checking in: sleep, water, a little movement — how are those today?",
                eye > 0.7f
                    ? $"{n}thanks for the eye contact. Makes it feel like a real conversation. How are you, really?"
                    : $"{n}I'm here. No rush — just checking you're alright."
            })
        };
    }

    private static string? PickHealthNudge(EmotionType emotion, float eye, float motion, string engagement, UserProfile profile)
    {
        if (emotion is EmotionType.Sad or EmotionType.Anxious)
        {
            if (profile.ThingsThatHelp.Count > 0)
                return $"Sometimes {profile.ThingsThatHelp[0]} helps you — want to try that?";
            return Pick(new[]
            {
                "Tiny health reset: drink some water and roll your shoulders.",
                "If you can, stand up and stretch for 30 seconds — it really helps the nervous system.",
                "Protect your sleep tonight if you can. Your mind heals while you rest."
            });
        }

        if (engagement == "restless" || motion > 0.5f)
            return "A short walk or shaking out your hands can burn that restless energy in a healthy way.";

        if (eye < 0.35f)
            return "Rest your eyes for 20 seconds — look far away, then come back. Screens tire us more than we notice.";

        if (emotion == EmotionType.Happy)
            return "Keep stacking small healthy wins while the mood's good.";

        return null;
    }

    private static string Pick(string[] options) => options[Rng.Next(options.Length)];
}
