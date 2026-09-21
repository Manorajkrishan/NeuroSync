using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Natural companion replies. Emotion signals stay internal.
/// </summary>
public interface ICompanionResponseService
{
    CompanionReply Generate(CompanionTurnContext context, ResponsePolicyService.PolicyResult policy);
}

public class CompanionResponseService : ICompanionResponseService
{
    private readonly ResponsePolicyService _policy;
    private static readonly Random Rng = new();

    public CompanionResponseService(ResponsePolicyService policy)
    {
        _policy = policy;
    }

    public CompanionReply Generate(CompanionTurnContext context, ResponsePolicyService.PolicyResult policy)
    {
        if (context.Safety.BlockNormalCompanionFlow)
        {
            return new CompanionReply
            {
                Message = context.Safety.Guidance,
                ProviderId = "safety-protocol",
                ExposedEmotionToUser = false
            };
        }

        var draft = context.Intent switch
        {
            UserIntent.Greeting => Pick(
                "Hey 👋 How are you doing?",
                "Hey! Good to see you. What's up?",
                "Hi — I'm here. How's your day going?"),
            UserIntent.CasualConversation => Casual(context),
            UserIntent.Question => Question(context),
            UserIntent.ListeningRequest =>
                "Alright. I'm listening — no advice unless you ask. What's on your mind?",
            UserIntent.AdviceRequest =>
                "Okay. Want me to help you think it through, or give one small next step?",
            UserIntent.EnvironmentAction =>
                "I can help with that — want me to enable Quiet Mode / environment changes? Say yes if you do.",
            UserIntent.TaskRequest =>
                "Alright. Give me the smallest task you're avoiding — we'll shrink it.",
            UserIntent.EmotionalDisclosure => Emotional(context),
            UserIntent.SafetySensitive => context.Safety.Guidance,
            _ => General(context)
        };

        // Mode shaping without stating emotions as facts
        draft = ShapeByMode(draft, context.Mode, context.Intent);

        var cleaned = _policy.Sanitize(draft, policy);
        return new CompanionReply
        {
            Message = cleaned,
            ProviderId = "template-companion-v2",
            ExposedEmotionToUser = false
        };
    }

    private static string Casual(CompanionTurnContext ctx)
    {
        var m = (ctx.UserMessage ?? "").Trim().ToLowerInvariant();
        if (m.Contains("how are you") || m.Contains("how's it going") || m.Contains("how r you") || m.Contains("how r u"))
            return Pick(
                "I'm good — I'm here with you. How's your day going?",
                "Doing alright. More importantly — how are you?",
                "All good here. What's going on with you?");
        if (m is "nothing" or "nothing da" or "nm" or "idk")
            return Pick(
                "That's suspicious 😂 What's actually going on?",
                "Okay… but for real — anything on your mind?",
                "Fair. Want company, or is something sitting there unspoken?");
        if (m is "ok" or "okay" or "k" or "yeah" or "yea" or "yep" or "lol" or "fine")
            return Pick("Got you. Want to keep chatting, or need a minute?", "Alright. I'm still here.", "Cool. Tell me more if you want.");
        return Pick("Yeah? Tell me more.", "I'm with you — what next?", "Okay, I'm listening.");
    }

    private static string Question(CompanionTurnContext ctx)
    {
        var m = (ctx.UserMessage ?? "").ToLowerInvariant();
        if (m.Contains("who are you") || m.Contains("what are you"))
            return "I'm NeuroSync — a wellbeing companion. I listen, remember what you allow, and help when you ask. Not a doctor.";
        return Pick(
            "Good question. Want the short version or the honest deep version?",
            "Let's dig into that — what's the part that matters most to you?",
            "I'm with you. What are you trying to figure out?");
    }

    private static string Emotional(CompanionTurnContext ctx)
    {
        var m = (ctx.UserMessage ?? "").ToLowerInvariant();
        if (m.Contains("miss her") || m.Contains("miss him") || m.Contains("miss them") || m.Contains("i miss"))
        {
            return ctx.Mode == CompanionInteractionMode.Listen || true
                ? Pick(
                    "Yeah… that can hit hard. Do you want to talk about them, or do you just want some company for a bit?",
                    "Ahh… one of those moments? What are you missing most — them, the memories, or just having someone there?",
                    "Missing someone can come back out of nowhere. I'm here if you want to say more.")
                : "That sounds heavy. Want to unpack it together?";
        }

        if (m.Contains("lonely") || m.Contains("alone") || m.Contains("no one"))
            return Pick(
                "Lonely nights are rough. You don't have to carry it quietly here — what's been the hardest part?",
                "I'm glad you said that. Want company, or do you want to talk about what's making it feel empty?");

        if (ctx.Mode == CompanionInteractionMode.ProblemSolving)
            return "Okay. Let's separate 'getting through right now' from 'the longer problem'. Which one first?";

        if (ctx.Mode == CompanionInteractionMode.Calm)
            return "Want to slow things down for a few minutes, or would talking through what's on your mind help more?";

        // Insufficient evidence → don't invent an emotion label
        if (ctx.Uncertainty is UncertaintyLevel.InsufficientEvidence or UncertaintyLevel.Uncertain
            or UncertaintyLevel.ConflictingSignals)
            return Pick(
                "I'm here. What's on your mind?",
                "Tell me what's sitting with you — no rush.",
                "I'm listening. Say as much or as little as you want.");

        return Pick(
            "That sounds rough. Do you want me to listen, help you work through it, or distract you for a bit?",
            "Yeah… I hear you. What's hitting you the most right now?",
            "Thanks for telling me. Want company, or want to unpack it?");
    }

    private static string General(CompanionTurnContext ctx)
    {
        if (ctx.Uncertainty is UncertaintyLevel.InsufficientEvidence)
            return Pick("What's on your mind?", "I'm here — talk to me.", "Hey. What's going on?");
        return Pick(
            "I'm with you. Tell me more.",
            "Okay — I'm listening.",
            "Yeah? What happened?");
    }

    private static string ShapeByMode(string draft, CompanionInteractionMode mode, UserIntent intent)
    {
        if (intent is UserIntent.Greeting or UserIntent.CasualConversation)
            return draft;

        return mode switch
        {
            CompanionInteractionMode.Focus when intent != UserIntent.EmotionalDisclosure =>
                draft.Contains("task", StringComparison.OrdinalIgnoreCase)
                    ? draft
                    : draft + " If you need Focus Mode, say the word.",
            _ => draft
        };
    }

    private static string Pick(params string[] options) =>
        options[Rng.Next(options.Length)];
}
