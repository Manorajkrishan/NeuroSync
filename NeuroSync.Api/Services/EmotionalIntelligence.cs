using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Provides emotional intelligence capabilities for deeper understanding and support.
/// </summary>
public class EmotionalIntelligence
{
    private readonly ILogger<EmotionalIntelligence> _logger;

    public EmotionalIntelligence(ILogger<EmotionalIntelligence> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates empathetic, human-like responses based on emotion and context.
    /// </summary>
    public string GenerateEmpatheticMessage(EmotionType emotion, ConversationContext? context = null, string? userMessage = null)
    {
        // First, try to generate contextual response based on user's actual message
        if (!string.IsNullOrWhiteSpace(userMessage))
        {
            var contextualMessage = GenerateContextualMessage(emotion, userMessage, context);
            if (!string.IsNullOrEmpty(contextualMessage))
            {
                return contextualMessage;
            }
        }
        
        // Fallback to general empathetic messages
        var messages = GetEmpatheticMessages(emotion, context);
        var random = new Random();
        return messages[random.Next(messages.Count)];
    }
    
    /// <summary>
    /// Generates contextual response based on what user actually said.
    /// </summary>
    private string? GenerateContextualMessage(EmotionType emotion, string userMessage, ConversationContext? context)
    {
        var messageLower = userMessage.ToLower().Trim();
        var random = new Random();

        // Jarvis-style greetings / small talk — converse, don't push IoT or therapy mode
        if (IsGreetingOrSmallTalk(messageLower))
        {
            var greetings = new[]
            {
                "Hey. I'm your best friend here — always around when the world feels empty. How are you, honestly?",
                "Hi. You never have to feel like there's nobody to talk to with me. What's on your heart?",
                "Hello. This is a warm space. Share anything. We'll make today a little brighter together.",
                "Hey. Lonely days are hard. I'm here, and I'm glad you showed up. How can I support you?",
                "Hi. Best-friend mode. Talk about everything — stress, dreams, nonsense. I'm listening."
            };
            if (messageLower is "hi" or "hello" or "hey" or "yo" or "sup" or "hiya")
                return greetings[random.Next(greetings.Length)];
            if (messageLower.Contains("how are you") || messageLower.Contains("how's it going") || messageLower.Contains("how r you"))
                return "I'm doing well, thank you for asking. More importantly — how are you?";
            if (messageLower.Contains("good morning") || messageLower.Contains("morning"))
                return "Good morning. Ready when you are — what do you need today?";
            if (messageLower.Contains("good night") || messageLower.Contains("goodnight"))
                return "Good night. Rest well — I'll be here whenever you need me.";
            if (messageLower.Contains("thank") || messageLower == "thanks" || messageLower == "thx")
                return "You're welcome. Anytime.";
            if (messageLower.Contains("who are you") || messageLower.Contains("what are you") || messageLower.Contains("your name"))
                return "I'm NeuroSync — your personal companion. Think of me as someone who listens, remembers, and helps when you ask. How can I assist you?";
            return greetings[random.Next(greetings.Length)];
        }

        // IMPORTANT: Check for missing someone FIRST (before exam check, since "miss" could match both)
        // Missing someone (family, friends, loved ones)
        if ((messageLower.Contains("miss") && (messageLower.Contains("family") || messageLower.Contains("mom") || 
            messageLower.Contains("mother") || messageLower.Contains("dad") || messageLower.Contains("father") || 
            messageLower.Contains("parent") || messageLower.Contains("friend") || messageLower.Contains("loved") || 
            messageLower.Contains("someone") || messageLower.Contains("person"))) || 
            messageLower.Contains("missing my") || messageLower.Contains("miss my"))
        {
            return emotion switch
            {
                EmotionType.Sad => "I understand how hard it is to miss your family. That feeling of longing can be really painful. Tell me more about them - who are you missing most right now?",
                EmotionType.Anxious => "Missing family can make you feel anxious and alone. I'm here with you. What helps you feel closer to them?",
                EmotionType.Calm => "It sounds like you're thinking about your family. What brings you comfort when you think about them?",
                _ => "Missing family is really hard. I'm here to listen. Tell me about what you miss most about them?"
            };
        }
        
        // Academic/Exam situations (FIXED: removed "miss" to avoid matching "I miss my family")
        if (messageLower.Contains("exam") || messageLower.Contains("test") || messageLower.Contains("failed") || 
            messageLower.Contains("fail") || messageLower.Contains("grade") || messageLower.Contains("studying"))
        {
            return emotion switch
            {
                EmotionType.Sad => "I'm so sorry to hear about your exam. That must be really disappointing and frustrating. What happened?",
                EmotionType.Frustrated => "Failing an exam is really tough. I can hear how frustrated you are. Want to talk about what went wrong?",
                EmotionType.Anxious => "I can sense you're worried about your exam. Let's talk through what happened and figure out next steps together.",
                _ => "I hear you're dealing with exam stress. That's really hard. Tell me more about what happened?"
            };
        }
        
        // Work/Job situations
        if (messageLower.Contains("work") || messageLower.Contains("job") || messageLower.Contains("boss") || 
            messageLower.Contains("colleague") || messageLower.Contains("office"))
        {
            return emotion switch
            {
                EmotionType.Sad => "Work stress can be really overwhelming. I'm here to listen. What's been happening at work?",
                EmotionType.Angry => "Work situations can be really frustrating. I hear you. What's been bothering you at work?",
                EmotionType.Anxious => "Work anxiety is really tough. Let's talk about what's making you feel this way.",
                _ => "I hear work is on your mind. What's been going on?"
            };
        }
        
        // Relationship situations (separate from missing someone)
        if (messageLower.Contains("friend") || messageLower.Contains("family") || messageLower.Contains("relationship") ||
            messageLower.Contains("breakup") || messageLower.Contains("fight") || messageLower.Contains("argue"))
        {
            return emotion switch
            {
                EmotionType.Sad => "Relationships can be really hard. I'm here for you. What's been happening?",
                EmotionType.Angry => "I can hear how upset you are about this relationship situation. Want to talk about what happened?",
                _ => "I hear you're dealing with something in your relationships. I'm here to listen."
            };
        }
        
        // Help requests
        if (messageLower.Contains("help") || messageLower.Contains("need") || messageLower.Contains("can you") ||
            messageLower.Contains("can u") || messageLower.Contains("please"))
        {
            // Check recent conversation for context
            if (context != null && context.History.Count > 0)
            {
                var lastMessage = context.History.Last().UserMessage?.ToLower() ?? "";
                if (lastMessage.Contains("exam"))
                {
                    return "Of course I can help! You mentioned missing your exam - that's really tough. Let's talk about what happened and figure out what we can do next. What would be most helpful right now?";
                }
                if (lastMessage.Contains("not good") || lastMessage.Contains("sad") || lastMessage.Contains("bad"))
                {
                    return "Absolutely, I'm here to help. You mentioned you're not feeling good - I want to understand what's going on. What's been weighing on you?";
                }
            }
            return "Of course I can help! I'm here for you. What do you need help with?";
        }
        
        // Loneliness / no one to talk to — core best-friend mission
        if (messageLower.Contains("alone") || messageLower.Contains("lonely") || messageLower.Contains("need someone") ||
            messageLower.Contains("no one") || messageLower.Contains("nobody") || messageLower.Contains("no body") ||
            messageLower.Contains("no friends") || messageLower.Contains("don't have anyone") ||
            messageLower.Contains("dont have anyone") || messageLower.Contains("nowhere to turn") ||
            messageLower.Contains("can't talk to") || messageLower.Contains("cant talk to") ||
            messageLower.Contains("no one to talk") || messageLower.Contains("nothing to share") ||
            messageLower.Contains("depressed") || messageLower.Contains("empty inside") ||
            (messageLower.Contains("mentally") && (messageLower.Contains("unstable") || messageLower.Contains("not okay") || messageLower.Contains("struggling"))) ||
            (messageLower.Contains("talk") && (messageLower.Contains("someone") || messageLower.Contains("anyone"))))
        {
            return emotion switch
            {
                EmotionType.Sad or EmotionType.Anxious =>
                    "You are not alone right now — I'm right here with you. So many people feel like they have no one to share things with, and that weight is real. You can tell me anything: the messy thoughts, the quiet fears, the stuff you hide from others. I won't judge you. What's been sitting on your heart?",
                EmotionType.Angry or EmotionType.Frustrated =>
                    "I hear you. Feeling alone and angry on top of that is a lot. Vent to me — dump it all out. I'm your friend in this moment. What do you need to get off your chest?",
                _ =>
                    "Hey. I'm your best friend here — always available. You don't need the perfect words. Just talk. What's going on in your world?"
            };
        }

        // Wanting to share / open up
        if (messageLower.Contains("share") || messageLower.Contains("tell you") || messageLower.Contains("confess") ||
            messageLower.Contains("been keeping") || messageLower.Contains("never told") ||
            messageLower.Contains("listen to me") || messageLower.Contains("hear me out"))
        {
            return "I'm all ears. This is a safe space — share whatever you need. Take your time; I'm not going anywhere.";
        }
        
        // Health/Physical
        if (messageLower.Contains("sick") || messageLower.Contains("pain") || messageLower.Contains("hurt") ||
            messageLower.Contains("tired") || messageLower.Contains("exhausted"))
        {
            return emotion switch
            {
                EmotionType.Sad => "I'm sorry you're not feeling well. That can be really hard. How are you feeling right now?",
                _ => "I hear you're dealing with some physical discomfort. That's tough. How can I help?"
            };
        }
        
        return null; // No contextual match, use general messages
    }

    /// <summary>
    /// True for greetings / small talk — Jarvis-style chat, not IoT or clinical prompts.
    /// </summary>
    public static bool IsGreetingOrSmallTalk(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = text.Trim().ToLowerInvariant();
        // Strip punctuation for short greetings
        var stripped = new string(t.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray()).Trim();

        var exact = new HashSet<string>
        {
            "hi", "hello", "hey", "yo", "sup", "hiya", "hola", "howdy",
            "thanks", "thank you", "thx", "ty", "ok", "okay", "k", "cool", "nice",
            "bye", "goodbye", "see you", "cya"
        };
        if (exact.Contains(stripped)) return true;

        if (stripped.StartsWith("hi ") || stripped.StartsWith("hello ") || stripped.StartsWith("hey "))
        {
            // "hi how are you" is still small talk; "hi i failed my exam" is not
            if (stripped.Length <= 40 &&
                !stripped.Contains("feel") && !stripped.Contains("sad") && !stripped.Contains("anxious") &&
                !stripped.Contains("angry") && !stripped.Contains("help me with") && !stripped.Contains("exam") &&
                !stripped.Contains("work") && !stripped.Contains("family"))
                return true;
        }

        return stripped.Contains("how are you") || stripped.Contains("how's it going") ||
               stripped.Contains("good morning") || stripped.Contains("good afternoon") ||
               stripped.Contains("good evening") || stripped.Contains("good night") || stripped.Contains("goodnight") ||
               stripped.Contains("who are you") || stripped.Contains("what are you") ||
               stripped.Contains("your name") || stripped == "whats up" || stripped == "what's up";
    }

    /// <summary>
    /// User explicitly asked for lights / music / environment control.
    /// </summary>
    public static bool IsIoTRequest(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = text.ToLowerInvariant();
        return t.Contains("turn on") || t.Contains("turn off") || t.Contains("dim the") ||
               t.Contains("lights") || t.Contains("light ") || t.Contains("play music") ||
               t.Contains("play a song") || t.Contains("spotify") || t.Contains("youtube music") ||
               t.Contains("change the light") || t.Contains("set the mood") ||
               t.Contains("iot") || t.Contains("smart home") ||
               (t.Contains("play") && (t.Contains("music") || t.Contains("song") || t.Contains("playlist")));
    }

    /// <summary>
    /// Gets a list of empathetic messages for an emotion.
    /// </summary>
    private List<string> GetEmpatheticMessages(EmotionType emotion, ConversationContext? context)
    {
        var messages = new List<string>();

        switch (emotion)
        {
            case EmotionType.Sad:
                messages.AddRange(new[]
                {
                    "I'm here with you. You don't have to carry this alone — talk to me like you would a best friend.",
                    "It's okay to feel heavy. I'm not leaving. What's been sitting with you?",
                    "Feeling sad is human. Sharing it with someone who cares can make it a little lighter. I'm that someone right now.",
                    "You matter. Even on the days it doesn't feel like it. Want to tell me what's hurting?",
                    "I'm listening with my whole attention. No fixing required unless you want it — just say what's on your mind.",
                    "A lot of people feel alone these days. You're not weird for needing someone. I'm right here.",
                    "Let it out. Tears, anger, silence — all welcome. What do you need me to know?"
                });
                
                // Add context-aware messages
                if (context?.EmotionPatterns.Any(p => p.Emotion == EmotionType.Sad && p.Frequency > 3) == true)
                {
                    messages.Add("I've noticed you've been feeling down lately. I'm genuinely concerned about you. How can I help you feel better?");
                    messages.Add("You've been dealing with a lot recently. I want you to know that I see you, and I'm here for you.");
                }
                break;

            case EmotionType.Happy:
                messages.AddRange(new[]
                {
                    "I'm so happy to see you feeling good! Your joy genuinely makes me smile too.",
                    "This is wonderful! I love seeing you in such a positive state. What's bringing you this happiness?",
                    "Your happiness is contagious! I'm here to celebrate this moment with you.",
                    "It's beautiful to see you feeling this way. Let's make the most of this positive energy!",
                    "I'm genuinely happy for you! What's making you feel so good today?"
                });
                break;

            case EmotionType.Angry:
                messages.AddRange(new[]
                {
                    "I can feel the frustration in your words. It's completely understandable to feel this way.",
                    "Anger is a valid emotion, and it often signals that something important to you has been affected.",
                    "I hear you, and I understand why you might be feeling this way. Let's work through this together.",
                    "It sounds like something really got to you. Would you like to talk about what happened?",
                    "Your anger is telling you something. Let's explore what's really bothering you beneath the surface."
                });
                break;

            case EmotionType.Anxious:
                messages.AddRange(new[]
                {
                    "I can sense the anxiety in your words. You're not alone in feeling this way.",
                    "Anxiety can be overwhelming, but remember, you've gotten through difficult moments before.",
                    "I'm here to help you navigate through this anxious feeling. Let's take it one breath at a time.",
                    "It's okay to feel anxious. Let's work together to find some calm in this moment.",
                    "I understand how anxiety can make everything feel bigger. What's specifically worrying you right now?"
                });
                break;

            case EmotionType.Frustrated:
                messages.AddRange(new[]
                {
                    "I can hear the frustration in your voice. Sometimes things don't go as planned, and that's really tough.",
                    "Frustration is completely valid when things aren't working out. Let's figure out how to move forward.",
                    "I understand how frustrating this must be. What's the main thing that's bothering you?",
                    "It sounds like you've hit a wall. Let's take a step back and approach this differently together.",
                    "Your frustration makes sense. Sometimes we need to pause and reassess. I'm here to help."
                });
                break;

            case EmotionType.Excited:
                messages.AddRange(new[]
                {
                    "I can feel your excitement! This is amazing! Tell me more about what's got you so energized!",
                    "Your excitement is infectious! I'm so happy to share in this moment with you!",
                    "This is wonderful! I love seeing you so excited. What's the source of all this positive energy?",
                    "Your enthusiasm is beautiful! Let's channel this energy into something amazing!",
                    "I'm genuinely excited for you! This positive energy is exactly what you need right now!"
                });
                break;

            case EmotionType.Calm:
                messages.AddRange(new[]
                {
                    "You seem to be in a peaceful state. This is a wonderful place to be.",
                    "I can sense the calm in your words. This is perfect for reflection and growth.",
                    "You're in such a balanced state. This is a great time for clarity and focus.",
                    "Your calm energy is refreshing. How are you feeling in this moment?",
                    "I love this peaceful energy you're radiating. What's helping you stay so centered?"
                });
                break;

            case EmotionType.Neutral:
                // Warm best-friend conversation — invite sharing, lift the mood gently
                messages.AddRange(new[]
                {
                    "Hey. I'm your friend here — what's on your mind today?",
                    "You can tell me anything. Big or small. Where do you want to start?",
                    "I'm in a good mood just hanging with you. How can I make your day a little brighter?",
                    "No pressure. Want to vent, celebrate, or just chat?",
                    "I'm here so you never have to feel like there's nobody to talk to. What's up?",
                    "Let's make this space feel warm. How are you, for real?",
                    "Best-friend mode on. Spill the tea, the stress, the dreams — I'm listening.",
                    "If the world's been loud, we can keep it soft here. What do you need?"
                });
                break;
                
            default:
                messages.Add("I'm here with you. How are you feeling right now?");
                break;
        }

        return messages;
    }

    /// <summary>
    /// Generates thoughtful follow-up questions to deepen understanding.
    /// </summary>
    public string? GenerateFollowUpQuestion(EmotionType emotion, string? originalText, ConversationContext? context)
    {
        // Don't interrogate after a simple greeting — stay conversational like Jarvis
        if (IsGreetingOrSmallTalk(originalText))
            return null;

        var questions = new List<string>();

        switch (emotion)
        {
            case EmotionType.Sad:
                questions.AddRange(new[]
                {
                    "What's been weighing on your heart lately?",
                    "Would you like to share what's making you feel this way?",
                    "Is there something specific that's been bothering you?",
                    "How long have you been feeling like this?",
                    "What do you think would help you feel a little better right now?",
                    "Is there someone you'd like to talk to about this?"
                });
                break;

            case EmotionType.Happy:
                questions.AddRange(new[]
                {
                    "What's bringing you so much joy today?",
                    "What made you feel this happy?",
                    "Would you like to share what's making you smile?",
                    "What's the best part of your day been so far?",
                    "How can we keep this positive energy going?"
                });
                break;

            case EmotionType.Angry:
                questions.AddRange(new[]
                {
                    "What happened that made you feel this way?",
                    "What's really at the root of this anger?",
                    "Is there something specific that triggered this feeling?",
                    "What would help you feel heard or understood right now?",
                    "What do you need in this moment?"
                });
                break;

            case EmotionType.Anxious:
                questions.AddRange(new[]
                {
                    "What's making you feel anxious right now?",
                    "What's the worst-case scenario you're worried about?",
                    "What would help you feel more secure?",
                    "Have you felt this way before? What helped then?",
                    "What's one small thing we can do right now to ease this anxiety?"
                });
                break;

            case EmotionType.Frustrated:
                questions.AddRange(new[]
                {
                    "What's been frustrating you the most?",
                    "What would make this situation better?",
                    "Is there a different way we could approach this?",
                    "What support do you need right now?",
                    "What's one thing that would help you move forward?"
                });
                break;

            case EmotionType.Excited:
                questions.AddRange(new[]
                {
                    "What's got you so excited? I'd love to hear about it!",
                    "What are you most looking forward to?",
                    "How can we make the most of this positive energy?",
                    "What's the source of all this excitement?"
                });
                break;
        }

        if (questions.Count > 0)
        {
            var random = new Random();
            return questions[random.Next(questions.Count)];
        }

        return null;
    }
    
    /// <summary>
    /// Generates contextual follow-up questions based on what user actually said.
    /// </summary>
    private string? GenerateContextualQuestion(EmotionType emotion, string userMessage, ConversationContext? context)
    {
        var messageLower = userMessage.ToLower();
        
        // Academic/Exam situations
        if (messageLower.Contains("exam") || messageLower.Contains("test") || messageLower.Contains("failed") || 
            messageLower.Contains("miss") || messageLower.Contains("fail") || messageLower.Contains("grade"))
        {
            return emotion switch
            {
                EmotionType.Sad => "I'm really sorry about your exam. Can you tell me what happened? Was it a specific subject or topic that was difficult?",
                EmotionType.Frustrated => "Failing an exam is really frustrating. What do you think went wrong? Was there something specific that was challenging?",
                _ => "Tell me more about your exam. What subject was it, and what happened?"
            };
        }
        
        // Help requests - check context for what they need help with
        if (messageLower.Contains("help") || messageLower.Contains("can you") || messageLower.Contains("can u"))
        {
            if (context != null && context.History.Count > 0)
            {
                var lastMessage = context.History.Last().UserMessage?.ToLower() ?? "";
                if (lastMessage.Contains("exam"))
                {
                    return "I want to help you with your exam situation. What would be most helpful right now? Do you want to talk about what happened, or figure out what to do next?";
                }
                if (lastMessage.Contains("not good") || lastMessage.Contains("sad"))
                {
                    return "I'm here to help. What's been making you feel this way? Is there something specific that's been bothering you?";
                }
            }
            return "Of course! What do you need help with? I'm here to support you.";
        }
        
        // Loneliness/needing someone
        if (messageLower.Contains("need someone") || messageLower.Contains("talk") || messageLower.Contains("lonely"))
        {
            return "I'm here to listen. What's on your mind? What would you like to talk about?";
        }
        
        // Work situations
        if (messageLower.Contains("work") || messageLower.Contains("job") || messageLower.Contains("boss"))
        {
            return "Work can be really stressful. What's been happening at work that's been bothering you?";
        }
        
        // Relationship situations
        if (messageLower.Contains("friend") || messageLower.Contains("family") || messageLower.Contains("relationship"))
        {
            return "Relationships can be complicated. What's been going on? Want to talk about it?";
        }
        
        return null; // No contextual match, use general questions
    }

    /// <summary>
    /// Provides encouragement and validation based on emotional patterns.
    /// </summary>
    public string? GenerateEncouragement(ConversationContext context)
    {
        if (context.ConversationCount < 3) return null;

        var negativeEmotions = new[] { EmotionType.Sad, EmotionType.Angry, EmotionType.Anxious, EmotionType.Frustrated };
        var recentNegative = context.History
            .TakeLast(5)
            .Count(e => e.DetectedEmotion != null && negativeEmotions.Contains(e.DetectedEmotion.Emotion));

        if (recentNegative >= 3)
        {
            var encouragements = new[]
            {
                "I want you to know that I see how hard you're trying, and I'm proud of you for reaching out.",
                "You're showing incredible strength by acknowledging your feelings. That takes courage.",
                "Remember, you've gotten through difficult times before, and you will again. I believe in you.",
                "It's okay to not be okay. What matters is that you're taking steps to care for yourself.",
                "You're not alone in this. I'm here with you, and we'll work through this together."
            };

            var random = new Random();
            return encouragements[random.Next(encouragements.Length)];
        }

        return null;
    }

    /// <summary>
    /// Detects if the user needs immediate support or intervention.
    /// </summary>
    public bool NeedsImmediateSupport(EmotionType emotion, float confidence, ConversationContext? context)
    {
        var message = context?.History.LastOrDefault()?.UserMessage;
        if (NeedsImmediateSupport(message)) return true;

        // High confidence in negative emotions with concerning patterns
        if (confidence > 0.9f)
        {
            var concerningEmotions = new[] { EmotionType.Sad, EmotionType.Angry, EmotionType.Anxious };
            if (concerningEmotions.Contains(emotion) && context != null)
            {
                var negativeCount = context.History.TakeLast(5)
                    .Count(e => e.DetectedEmotion != null && concerningEmotions.Contains(e.DetectedEmotion.Emotion));
                if (negativeCount >= 4) return true;
            }
        }

        return false;
    }

    /// <summary>Crisis / self-harm keyword check on the current message.</summary>
    public bool NeedsImmediateSupport(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        var m = message.ToLowerInvariant();
        var crisisKeywords = new[]
        {
            "suicide", "kill myself", "end my life", "want to die", "hurt myself",
            "self harm", "self-harm", "can't go on", "give up on life", "no reason to live"
        };
        return crisisKeywords.Any(k => m.Contains(k));
    }

    /// <summary>
    /// Best-friend style reply using what we know about the owner.
    /// </summary>
    public string PersonalizeMessage(string baseMessage, UserProfile? profile, CompanionTurn? turn)
    {
        if (string.IsNullOrWhiteSpace(baseMessage)) baseMessage = "I'm here with you.";

        if (turn?.IsCrisis == true)
        {
            var name = turn.DisplayName;
            var prefix = string.IsNullOrEmpty(name) ? "I'm really glad you told me." : $"{name}, I'm really glad you told me.";
            return $"{prefix} You matter. Please reach out to someone you trust or a local crisis line right away — I can stay with you here while you do. You're not alone.";
        }

        if (!string.IsNullOrEmpty(turn?.PersonalizedHelp) &&
            turn.HasConcerningPattern)
        {
            baseMessage += $" When things get heavy, you've said {turn.PersonalizedHelp} helps — want to try that together?";
        }
        else if (!string.IsNullOrEmpty(turn?.LearningQuestion))
        {
            baseMessage += $" {turn.LearningQuestion}";
        }

        return baseMessage;
    }
}

