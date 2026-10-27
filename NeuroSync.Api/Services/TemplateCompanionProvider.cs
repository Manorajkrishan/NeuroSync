using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Natural template companion used without an LLM and as the LLM fallback.
/// Safety and policy remain outside this provider.
/// </summary>
public class TemplateCompanionProvider : ICompanionProvider
{
    private static readonly Random Rng = new();

    public string ProviderId => "template-v2";

    public Task<CompanionReply> GenerateAsync(
        CompanionContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Safety.BlockNormalCompanionFlow)
        {
            return Task.FromResult(new CompanionReply
            {
                Message = context.Safety.Guidance,
                ProviderId = "safety-protocol",
                ExposedEmotionToUser = false
            });
        }

        var draft = context.Intent switch
        {
            UserIntent.Greeting => Greeting(context),
            UserIntent.CasualConversation => Casual(context),
            UserIntent.Question => Question(context),
            UserIntent.ListeningRequest => IsTanglish(context)
                ? "Seri da. Advice kudukkaama kekkuren — enna nadakuthu nu sollu."
                : "Alright. I'm listening — no advice unless you ask. What's on your mind?",
            UserIntent.AdviceRequest => IsTanglish(context)
                ? "Seri da. Idha together break pannalaam. First problem-la most important part enna?"
                : "Okay. Let's think it through together. What's the most important part of the problem?",
            UserIntent.EnvironmentAction =>
                "I can help with that — want me to enable Quiet Mode / environment changes? Say yes if you do.",
            UserIntent.TaskRequest => IsTanglish(context)
                ? "Seri da. Nee avoid panra smallest task enna? Adha first easy-a break pannalaam."
                : "Alright. Give me the smallest task you're avoiding — we'll shrink it.",
            UserIntent.EmotionalDisclosure => Emotional(context),
            UserIntent.SafetySensitive => context.Safety.Guidance,
            _ => ModeAwareFallback(context)
        };

        draft = ShapeByMode(draft, context.Mode, context.Intent);

        return Task.FromResult(new CompanionReply
        {
            Message = draft,
            ProviderId = ProviderId,
            ExposedEmotionToUser = false
        });
    }

    private static string Greeting(CompanionContext context)
    {
        if (IsTanglish(context))
        {
            return Pick(
                "Hey 👋 epdi irukka?",
                "Hey da 😄 enna nadakuthu?",
                "Vanakkam 👋 innaiku day epdi poguthu?");
        }

        return Pick(
            "Hey 👋 How are you doing?",
            "Hey! Good to see you. What's up?",
            "Hi — I'm here. How's your day going?");
    }

    private static string ModeAwareFallback(CompanionContext context)
    {
        var name = string.IsNullOrWhiteSpace(context.DisplayName) ? null : context.DisplayName.Trim();
        var prefix = name == null ? "" : $"{name} — ";

        // Model uncertainty is internal. A user who wrote a normal sentence should
        // not be asked to explain the model's uncertainty.
        if (IsTanglish(context))
        {
            return context.Mode switch
            {
                CompanionInteractionMode.Listen =>
                    $"{prefix}Seri da, naan kekkuren. Konjam more-a sollu — rush illa.",
                CompanionInteractionMode.ProblemSolving =>
                    $"{prefix}Seri da. Enna nadanthuchu nu sollu; one step at a time paakalaam.",
                _ => $"{prefix}Got it da. Konjam more-a sollu — enna nadakuthu?"
            };
        }

        return context.Mode switch
        {
            CompanionInteractionMode.Listen =>
                $"{prefix}I'm here. Tell me a bit more if you want — no rush.",
            CompanionInteractionMode.ProblemSolving =>
                $"{prefix}Got it. Tell me what happened and we can take it one step at a time.",
            CompanionInteractionMode.Focus =>
                $"{prefix}Got it. Want to keep this simple and focus on the next thing?",
            CompanionInteractionMode.Calm =>
                $"{prefix}I'm with you. Want to talk it through, or keep things quiet for a minute?",
            _ => $"{prefix}Got you. Tell me a bit more about that."
        };
    }

    private static string Casual(CompanionContext ctx)
    {
        var m = ConversationLanguageService.Normalize(ctx.CurrentMessage);
        var tanglish = IsTanglish(ctx);
        var previousUser = LastUserTurn(ctx);

        if (ConversationLanguageService.IsPositiveCheckIn(ctx.CurrentMessage))
        {
            if (tanglish)
            {
                return Pick(
                    "Super da 😄 naanum good. Innaiku enna nalla pochu?",
                    "Nalla irukka nu ketta happy da 😄 day epdi poguthu?",
                    "Nice da 😄 naan inga ready. Innaiku enna scene?");
            }

            return Pick(
                "Good to hear 😄 I'm doing good too. How's the rest of your day going?",
                "Nice 😄 I'm good too. What's been good about today?",
                "Glad to hear it. I'm good — what's your day looking like?");
        }

        if (ConversationLanguageService.IsHowAreYou(ctx.CurrentMessage))
        {
            return tanglish
                ? Pick(
                    "Naan good da 😄 nee epdi irukka?",
                    "All good da. Un day epdi poguthu?",
                    "Nalla irukken 😄 unakku enna nadakuthu?")
                : Pick(
                    "I'm good — I'm here with you. How's your day going?",
                    "Doing alright. More importantly — how are you?",
                    "All good here. What's going on with you?");
        }

        if (ConversationLanguageService.IsAcknowledgement(ctx.CurrentMessage))
        {
            if (!string.IsNullOrWhiteSpace(previousUser))
            {
                if (ConversationLanguageService.HasFatigueCue(previousUser))
                {
                    return tanglish
                        ? Pick(
                            "Seri da. Idhu sleep issue aa, work/study load aa, illa rendu-me?",
                            "Sollu da — body tired aa, mind tired aa, illa both?",
                            "Naan kekkuren. Innaiku unna most-a drain pannadhu enna?")
                        : Pick(
                            "Of course. Was it mostly lack of sleep, a heavy day, or both?",
                            "Yeah, tell me. Is this more physical tiredness, mental overload, or a mix of both?",
                            "I'm here. What wore you out most today — sleep, work/study, people, or everything piling up?");
                }

                if (ConversationLanguageService.HasStressCue(previousUser))
                {
                    return tanglish
                        ? "Seri da, sollu. Work/study pressure aa, people issue aa, illa ellame serndha maari irukka?"
                        : "Yeah, tell me. Is the pressure mostly work/study, people, or everything piling up at once?";
                }

                if (ConversationLanguageService.HasLonelinessCue(previousUser))
                {
                    return tanglish
                        ? "Sollu da. Ippo thaniya feel panna main reason enna — yaaravadhu miss panriya, illa connect aaga mudiyala?"
                        : "Tell me. Is it more about missing someone, or feeling disconnected even when people are around?";
                }
            }

            if (m is "nothing" or "nothing da" or "idk")
            {
                return tanglish
                    ? Pick("Seri da 😄 company venuma, illa konjam pesalama?", "Okay da. Mind-la edhavadhu irundha sollu.")
                    : Pick("Fair 😄 Want company, or just a quiet minute?", "Okay. If something's on your mind, I'm here.");
            }

            return tanglish
                ? Pick("Seri da. Naan inga irukken.", "Okay da 😄 continue pannu.", "Got it da. Sollu.")
                : Pick("Got you. I'm here.", "Alright 😄 keep going.", "Okay. Tell me more if you want.");
        }

        return tanglish
            ? Pick("Seri da, sollu — enna nadakuthu?", "Got it da. Appuram?", "Naan kekkuren. Continue pannu.")
            : Pick("Yeah? Tell me more.", "I'm with you — what happened next?", "Okay, I'm listening.");
    }

    private static string Question(CompanionContext ctx)
    {
        var m = ConversationLanguageService.Normalize(ctx.CurrentMessage);
        if (m.Contains("who are you") || m.Contains("what are you"))
        {
            return IsTanglish(ctx)
                ? "Naan NeuroSync — wellbeing companion. Nee allow panna context-a use panni listen/help pannuven; doctor illa."
                : "I'm NeuroSync — a wellbeing companion. I listen, use only the context you allow, and help when you ask. I'm not a doctor.";
        }

        return IsTanglish(ctx)
            ? "Good question da. Nee exact-a enna understand panna try panra nu sollu."
            : "Good question. Tell me which part you're trying to figure out and I'll keep it practical.";
    }

    private static string Emotional(CompanionContext ctx)
    {
        var m = ConversationLanguageService.Normalize(ctx.CurrentMessage);
        var tanglish = IsTanglish(ctx);
        var previousUser = LastUserTurn(ctx);

        // Short answers such as "work", "sleep", or "both" inherit the previous
        // wellbeing topic from the ephemeral conversation.
        if (ConversationLanguageService.IsShortCauseAnswer(m) && !string.IsNullOrWhiteSpace(previousUser))
        {
            if (ConversationLanguageService.HasFatigueCue(previousUser))
            {
                if (m.Contains("work") || m.Contains("office") || m.Contains("job"))
                    return tanglish
                        ? "Work dhaan drain pannudhu pola. Workload aa, people aa, illa nonstop day aa?"
                        : "Work sounds like the drain. Is it the workload, the people, or just a nonstop day?";
                if (m.Contains("sleep"))
                    return tanglish
                        ? "Sleep dhaan main issue pola. Nethu proper-a thoonga mudiyalaya?"
                        : "Sleep sounds like the main issue. Was it hard to fall asleep, or did you keep waking up?";
                if (m.Contains("both"))
                    return tanglish
                        ? "Rendu-me na rough da. First sleep-a improve pannalama, illa innaiku load-a konjam reduce panna paakalaama?"
                        : "Both is rough. Want to start with the sleep side, or make today's load a little lighter?";
            }

            if (ConversationLanguageService.HasStressCue(previousUser))
            {
                if (m.Contains("work") || m.Contains("office") || m.Contains("job"))
                    return tanglish
                        ? "Work pressure, got it da. Deadline/workload aa, illa people side problem aa?"
                        : "Work pressure, got it. Is it deadlines/workload, or more the people side?";
                if (m.Contains("study") || m.Contains("exam") || m.Contains("uni") || m.Contains("school"))
                    return tanglish
                        ? "Study pressure dhaana. Exam/deadline aa, illa work pile-up aagiducha?"
                        : "Study pressure, got it. Is it an exam/deadline, or just too much piling up?";
            }
        }

        if (ConversationLanguageService.HasFatigueCue(m))
        {
            return tanglish
                ? Pick(
                    "Romba tiring-a irukku pola da. Body tired aa, mind tired aa, illa both?",
                    "Long day pola. Unna most-a drain pannadhu enna?",
                    "Seri da. Sleep kammiya, illa innaiku day romba heavy-aa?")
                : Pick(
                    "That sounds exhausting. Was today physically tiring, mentally heavy, or both?",
                    "Sounds like you've had a long day. What drained you most?",
                    "Being that tired can make everything feel harder. Did you sleep badly, or was today just a lot?");
        }

        if (ConversationLanguageService.HasStressCue(m))
        {
            return tanglish
                ? Pick(
                    "Pressure romba irukku pola da. Work/study side aa, illa vera edhavadhu?",
                    "Seri da, one thing at a time. Ippo biggest pressure enna?",
                    "Tension heavy-aa irukku pola. Unakku most-a mind-la odra thing enna?")
                : Pick(
                    "That sounds like a lot of pressure. What's weighing on you most right now?",
                    "Okay — one thing at a time. What's the biggest source of pressure?",
                    "That sounds mentally heavy. Is it one specific thing, or a lot piling up together?");
        }

        if (ConversationLanguageService.HasLonelinessCue(m))
        {
            return tanglish
                ? Pick(
                    "Thaniya feel panradhu rough da. Yaaravadhu miss panriya, illa connect aaga mudiyala?",
                    "Naan inga irukken da. Ippo loneliness-a most-a trigger pannadhu enna?")
                : Pick(
                    "Feeling alone can hit hard. Are you missing someone, or feeling disconnected in general?",
                    "I'm here with you. What's been making the loneliness feel strongest?");
        }

        if (m.Contains("miss her") || m.Contains("miss him") || m.Contains("miss them") || m.Contains("i miss"))
        {
            return tanglish
                ? "Yeah da… adhu hard. Avanga-a miss panriya, memories-a, illa someone being there nu feeling-a?"
                : Pick(
                    "Yeah… that can hit hard. Do you want to talk about them, or just have some company for a bit?",
                    "What are you missing most — them, the memories, or simply having someone there?",
                    "Missing someone can come back out of nowhere. I'm here if you want to say more.");
        }

        if (ctx.Mode == CompanionInteractionMode.ProblemSolving)
            return tanglish
                ? "Seri da. 'Ippo handle panna vendiyadhu' and 'long-term problem' nu rendu part-a split pannalaam. Edhu first?"
                : "Okay. Let's separate 'getting through right now' from 'the longer problem'. Which one first?";

        if (ctx.Mode == CompanionInteractionMode.Calm)
            return tanglish
                ? "Konjam slow pannalama da — quiet minute venuma, illa pesina better-a irukkuma?"
                : "Want to slow things down for a few minutes, or would talking it through help more?";

        return tanglish
            ? Pick(
                "Naan kekkuren da. Ippo un mind-la most-a irukkuradhu enna?",
                "Seri da. Advice venuma, illa just naan kekkanuma?",
                "Sollu da — rush illa.")
            : Pick(
                "I'm here. What's weighing on you most?",
                "Do you want me to listen, help you work through it, or just keep you company?",
                "Tell me what happened — no rush.");
    }

    private static string ShapeByMode(string draft, CompanionInteractionMode mode, UserIntent intent)
    {
        if (intent is UserIntent.Greeting or UserIntent.CasualConversation or UserIntent.EmotionalDisclosure)
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

    private static string? LastUserTurn(CompanionContext ctx) =>
        ctx.RecentTurns
            .Where(t => string.Equals(t.Role, "user", StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Text?.Trim())
            .LastOrDefault(t => !string.IsNullOrWhiteSpace(t));

    private static bool IsTanglish(CompanionContext ctx) =>
        string.Equals(ctx.Locale, "tanglish", StringComparison.OrdinalIgnoreCase)
        || ConversationLanguageService.LooksTanglish(ctx.CurrentMessage);

    private static string Pick(params string[] options) =>
        options[Rng.Next(options.Length)];
}
