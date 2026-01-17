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
        var messageLower = userMessage.ToLower();
        
        // Academic/Exam situations
        if (messageLower.Contains("exam") || messageLower.Contains("test") || messageLower.Contains("failed") || 
            messageLower.Contains("miss") || messageLower.Contains("fail") || messageLower.Contains("grade"))
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
        
        // Missing someone (MUST CHECK BEFORE relationship situations)
        if (messageLower.Contains("miss") && (messageLower.Contains("mother") || messageLower.Contains("mom") || messageLower.Contains("father") || 
            messageLower.Contains("dad") || messageLower.Contains("parent") || messageLower.Contains("family") || 
            messageLower.Contains("friend") || messageLower.Contains("loved") || messageLower.Contains("someone")))
        {
            if (messageLower.Contains("mother") || messageLower.Contains("mom"))
            {
                return emotion switch
                {
                    EmotionType.Sad => "I'm so sorry you're missing your mother. That must be really hard. How long has it been?",
                    _ => "Missing your mother is really tough. I'm here for you. Do you want to talk about her?"
                };
            }
            if (messageLower.Contains("father") || messageLower.Contains("dad"))
            {
                return emotion switch
                {
                    EmotionType.Sad => "I'm so sorry you're missing your father. That must be really difficult. How are you coping?",
                    _ => "Missing your father is really tough. I'm here to listen. Want to share some memories?"
                };
            }
            return emotion switch
            {
                EmotionType.Sad => "I can hear how much you miss them. That pain is real, and it's okay to feel it. I'm here with you. Tell me about them?",
                _ => "Missing someone you love is really hard. I'm here to listen. What are you missing most about them?"
            };
        }
        
        // Relationship situations
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
        
        // Loneliness/needing someone
        if (messageLower.Contains("alone") || messageLower.Contains("lonely") || messageLower.Contains("need someone") ||
            messageLower.Contains("no one") || messageLower.Contains("talk"))
        {
            return "I hear you need someone to talk to. I'm here, and I'm listening. You're not alone in this. What's on your mind?";
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
                    "I'm here with you. It's okay to feel this way, and your feelings are completely valid.",
                    "I can sense you're going through something difficult. You don't have to face this alone.",
                    "Feeling sad is part of being human. I'm here to listen and support you through this.",
                    "It sounds like you're having a tough time. Would you like to talk about what's on your mind?",
                    "I understand this is hard for you. Remember, tough times don't last, but tough people do.",
                    "Your feelings matter. Let's work through this together, one step at a time.",
                    "I hear you're not feeling good. I'm here to listen. What's going on?",
                    "I can tell you're struggling. You're not alone in this. What's been weighing on you?",
                    "I'm sorry you're going through this. I'm here for you. Want to talk about it?",
                    "It sounds like you're having a really hard time. I'm here to support you. What's on your mind?",
                    "I can feel that you're hurting. I'm here to listen and help however I can.",
                    "You don't have to go through this alone. I'm here. What's been bothering you?"
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
                // More varied and conversational responses for neutral state
                messages.AddRange(new[]
                {
                    "I'm here whenever you need me. What's on your mind?",
                    "I'm listening. Feel free to share what you're thinking about.",
                    "I'm here for you. Is there anything you'd like to talk about?",
                    "How can I help you today?",
                    "What would you like to explore or discuss?",
                    "I'm here to support you. What's going on in your world?",
                    "Feel free to share anything that's on your mind.",
                    "I'm ready to chat whenever you are. What's up?",
                    "How's your day going?",
                    "What's something you'd like to talk about?",
                    "I'm here. What's on your mind today?",
                    "Feel like sharing what you're thinking about?",
                    "I'm listening. What would you like to discuss?",
                    "How can I be helpful to you right now?",
                    "What's something you'd like to explore together?"
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
        // High confidence in negative emotions with concerning patterns
        if (confidence > 0.9f)
        {
            var concerningEmotions = new[] { EmotionType.Sad, EmotionType.Angry };
            if (concerningEmotions.Contains(emotion))
            {
                // Check for concerning keywords
                if (context?.History.LastOrDefault()?.UserMessage != null)
                {
                    var message = context.History.Last().UserMessage.ToLower();
                    var crisisKeywords = new[] { "hurt", "harm", "end", "give up", "can't go on", "suicide", "kill myself" };
                    if (crisisKeywords.Any(keyword => message.Contains(keyword)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}

