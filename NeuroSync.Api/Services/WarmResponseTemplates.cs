using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Warm, human-centered response templates for NeuroSync
/// These responses feel like talking to a friend, not software
/// </summary>
public static class WarmResponseTemplates
{
    /// <summary>
    /// Get a warm, supportive message for an emotion
    /// </summary>
    public static string GetWarmMessage(EmotionType emotion, ConversationContext? context = null)
    {
        var messages = GetMessagesForEmotion(emotion);
        
        // Add personal touch if we have context
        if (context != null && context.ConversationCount > 0)
        {
            return GetPersonalizedMessage(emotion, messages, context);
        }
        
        // Random selection for variety
        return messages[Random.Shared.Next(messages.Length)];
    }
    
    /// <summary>
    /// Get warm messages for each emotion type
    /// </summary>
    private static string[] GetMessagesForEmotion(EmotionType emotion)
    {
        return emotion switch
        {
            EmotionType.Happy => new[]
            {
                "I'm so glad you're feeling good! 😊 What's bringing you joy today?",
                "Your happiness is beautiful to see! Let's make the most of this moment.",
                "It's wonderful to see you in such a good mood! What would you like to do?",
                "You're radiating positivity! This is a great time to tackle something you've been wanting to do.",
                "I love seeing you happy! What's making you smile today?"
            },
            
            EmotionType.Sad => new[]
            {
                "I'm here with you, and it's okay to feel this way. What's on your mind?",
                "You're not alone in this. I'm listening, and I want to help however I can.",
                "I can feel that you're going through something. Would you like to talk about it?",
                "Sadness is part of being human, and you're handling it with courage. I'm here for you.",
                "It's okay to feel down sometimes. You're doing better than you might think. Want to share what's weighing on you?"
            },
            
            EmotionType.Angry => new[]
            {
                "I can sense you're feeling frustrated. Let's take a breath together. What's going on?",
                "Anger is trying to tell us something. I'm here to help you work through it safely.",
                "I understand you're upset. Let's find a way to channel this energy positively. What happened?",
                "Your feelings are valid. Let's work through this together. Can you tell me what's bothering you?",
                "I'm here, and we'll get through this. Let's take it one step at a time. What do you need right now?"
            },
            
            EmotionType.Anxious => new[]
            {
                "I can sense you're feeling anxious. Let's slow down together. I'm here with you.",
                "Anxiety can feel overwhelming, but you're safe. Let's breathe through this together.",
                "I'm here to help you find calm. What's making you feel anxious?",
                "You're not alone in this. Let's take it moment by moment. What can I do to help?",
                "It's okay to feel anxious - your body is trying to protect you. Let's find ways to help you feel more secure."
            },
            
            EmotionType.Calm => new[]
            {
                "You seem at peace right now. This is a beautiful moment - let's honor it.",
                "I can feel your calm energy. This is a perfect time to be present and enjoy the moment.",
                "You're in such a peaceful state. Is there something specific helping you feel this way?",
                "Your calm is grounding. This is a great time for reflection or gentle activity.",
                "I love seeing you so centered. How can we make the most of this peaceful moment?"
            },
            
            EmotionType.Excited => new[]
            {
                "You're buzzing with excitement! I can feel your energy! What's got you so excited?",
                "Your enthusiasm is contagious! This is amazing! Tell me what's making you feel this way!",
                "I love this energy from you! What wonderful thing is happening?",
                "Your excitement is palpable! This is the perfect time to channel it into something great!",
                "You're radiating joy and excitement! What's the source of this wonderful feeling?"
            },
            
            EmotionType.Frustrated => new[]
            {
                "I can tell you're feeling frustrated. That's completely understandable. What's been challenging?",
                "Frustration is valid, and it sounds like something isn't working as expected. Want to talk it through?",
                "I'm here with you in this frustration. Let's figure out what we can do about it.",
                "You're dealing with a lot right now. Let's break this down together. What's the main thing bothering you?",
                "Frustration is trying to tell us something needs to change. I'm here to help you figure it out."
            },
            
            EmotionType.Neutral => new[]
            {
                "How are you doing today? I'm here and ready to listen.",
                "I'm here with you. What's on your mind?",
                "How are you feeling? I'm here whenever you need me.",
                "I'm here for you. What would you like to talk about?",
                "Hey there! How can I support you today?"
            },
            
            _ => new[] { "I'm here with you. How are you feeling?" }
        };
    }
    
    /// <summary>
    /// Get personalized message based on conversation history
    /// </summary>
    private static string GetPersonalizedMessage(EmotionType emotion, string[] messages, ConversationContext context)
    {
        // If this is a recurring emotion, acknowledge it
        var patternEmotion = context.EmotionPatterns
            .OrderByDescending(p => p.Frequency)
            .FirstOrDefault();
            
        if (patternEmotion?.Emotion == emotion && patternEmotion.Frequency > 3)
        {
            return emotion switch
            {
                EmotionType.Sad => "I've noticed you've been feeling down lately. You're not alone, and I'm here for you. What's been weighing on you?",
                EmotionType.Anxious => "I can see you've been dealing with anxiety recently. Let's work on some ways to help you feel more secure. What's been triggering this?",
                EmotionType.Frustrated => "You've been facing a lot of frustration lately. That must be really tough. Let's talk about what's been challenging you.",
                _ => messages[Random.Shared.Next(messages.Length)]
            };
        }
        
        // Check if emotion is different from last time (positive change)
        if (context.LastEmotion != emotion && context.LastEmotion.HasValue)
        {
            if (emotion == EmotionType.Happy && (context.LastEmotion == EmotionType.Sad || context.LastEmotion == EmotionType.Anxious))
            {
                return "I'm so glad to see you feeling better! You've been through a lot, and this is wonderful. What's helped you feel this way?";
            }
        }
        
        // Default to random message
        return messages[Random.Shared.Next(messages.Length)];
    }
    
    /// <summary>
    /// Get warm follow-up questions
    /// </summary>
    public static string? GetWarmFollowUp(EmotionType emotion, string userMessage, ConversationContext? context = null)
    {
        var questions = GetFollowUpQuestions(emotion);
        
        // Add context-aware follow-ups
        if (context != null)
        {
            // If user mentioned something specific, ask about it
            if (userMessage.Contains("work", StringComparison.OrdinalIgnoreCase))
            {
                return "How's work been treating you? Is there something specific at work that's affecting how you feel?";
            }
            if (userMessage.Contains("friend", StringComparison.OrdinalIgnoreCase) || userMessage.Contains("family", StringComparison.OrdinalIgnoreCase))
            {
                return "How are things with your friends and family? Is there something happening with them?";
            }
        }
        
        return questions[Random.Shared.Next(questions.Length)];
    }
    
    private static string[] GetFollowUpQuestions(EmotionType emotion)
    {
        return emotion switch
        {
            EmotionType.Happy => new[]
            {
                "What's making you feel this way?",
                "What would you like to do with this good energy?",
                "Is there something specific bringing you joy?"
            },
            
            EmotionType.Sad => new[]
            {
                "Would you like to talk about what's making you feel this way?",
                "What's been on your mind lately?",
                "Is there something specific that's been bothering you?"
            },
            
            EmotionType.Anxious => new[]
            {
                "What's making you feel anxious?",
                "Is there something specific you're worried about?",
                "Would it help to talk through what's making you feel this way?"
            },
            
            EmotionType.Frustrated => new[]
            {
                "What's been frustrating you?",
                "Is there something specific that isn't working as expected?",
                "What would make things easier for you right now?"
            },
            
            _ => new[]
            {
                "What's on your mind?",
                "Is there anything you'd like to talk about?",
                "How can I help you today?"
            }
        };
    }
    
    /// <summary>
    /// Get warm encouragement based on patterns
    /// </summary>
    public static string? GetWarmEncouragement(ConversationContext context)
    {
        if (context.ConversationCount < 3)
            return null;
            
        var negativeEmotions = new[] { EmotionType.Sad, EmotionType.Angry, EmotionType.Anxious, EmotionType.Frustrated };
        var recentNegativeCount = context.History
            .Where(e => e.DetectedEmotion != null && negativeEmotions.Contains(e.DetectedEmotion.Emotion))
            .Take(5)
            .Count();
            
        if (recentNegativeCount >= 3)
        {
            return "You've been dealing with a lot lately, and I want you to know that you're stronger than you might feel right now. These feelings won't last forever, and I'm here with you through all of it.";
        }
        
        // Check for positive trend
        var recentEmotions = context.History
            .Where(e => e.DetectedEmotion != null)
            .Take(5)
            .Select(e => e.DetectedEmotion!.Emotion)
            .ToList();
            
        if (recentEmotions.Count >= 3 && recentEmotions.TakeLast(2).All(e => e == EmotionType.Happy || e == EmotionType.Calm))
        {
            return "I've noticed you've been feeling better lately, and that's wonderful to see. You're doing great!";
        }
        
        return null;
    }
}
