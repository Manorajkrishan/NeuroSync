using NeuroSync.ML;

namespace NeuroSync.ML;

/// <summary>
/// Generates sample training data for emotion classification.
/// </summary>
public static class TrainingDataGenerator
{
    /// <summary>
    /// Generates expanded sample training data for emotion classification.
    /// This is a basic dataset for demonstration. For better accuracy, use a real dataset.
    /// </summary>
    public static List<EmotionData> GenerateSampleData()
    {
        return new List<EmotionData>
        {
            // Happy (expanded)
            new EmotionData { Text = "I'm happy!", Label = "happy" },
            new EmotionData { Text = "I am happy.", Label = "happy" },
            new EmotionData { Text = "I feel happy.", Label = "happy" },
            new EmotionData { Text = "I'm so happy today!", Label = "happy" },
            new EmotionData { Text = "This is amazing! I love it!", Label = "happy" },
            new EmotionData { Text = "I feel great!", Label = "happy" },
            new EmotionData { Text = "What a wonderful day!", Label = "happy" },
            new EmotionData { Text = "I'm thrilled about this!", Label = "happy" },
            new EmotionData { Text = "This makes me so happy!", Label = "happy" },
            new EmotionData { Text = "I'm feeling joyful!", Label = "happy" },
            new EmotionData { Text = "Perfect! Just what I wanted!", Label = "happy" },
            new EmotionData { Text = "I'm delighted!", Label = "happy" },
            new EmotionData { Text = "This is fantastic!", Label = "happy" },
            new EmotionData { Text = "I'm in a great mood!", Label = "happy" },
            new EmotionData { Text = "This is wonderful news!", Label = "happy" },
            new EmotionData { Text = "I'm so pleased!", Label = "happy" },
            new EmotionData { Text = "This brings me joy!", Label = "happy" },
            new EmotionData { Text = "I'm feeling fantastic!", Label = "happy" },
            
            // Sad (expanded and optimized - many simple variations)
            new EmotionData { Text = "I feel sad.", Label = "sad" },
            new EmotionData { Text = "I feel so sad.", Label = "sad" },
            new EmotionData { Text = "I'm sad.", Label = "sad" },
            new EmotionData { Text = "I am sad.", Label = "sad" },
            new EmotionData { Text = "I feel sad right now.", Label = "sad" },
            new EmotionData { Text = "I feel so sad right now.", Label = "sad" },
            new EmotionData { Text = "I'm feeling sad.", Label = "sad" },
            new EmotionData { Text = "I am feeling sad.", Label = "sad" },
            new EmotionData { Text = "I feel really sad.", Label = "sad" },
            new EmotionData { Text = "I'm really sad.", Label = "sad" },
            new EmotionData { Text = "I am really sad.", Label = "sad" },
            new EmotionData { Text = "This is really depressing.", Label = "sad" },
            new EmotionData { Text = "I'm feeling down today.", Label = "sad" },
            new EmotionData { Text = "I feel down.", Label = "sad" },
            new EmotionData { Text = "I'm down.", Label = "sad" },
            new EmotionData { Text = "Everything feels so hopeless.", Label = "sad" },
            new EmotionData { Text = "I'm really upset about this.", Label = "sad" },
            new EmotionData { Text = "I'm upset.", Label = "sad" },
            new EmotionData { Text = "This makes me feel miserable.", Label = "sad" },
            new EmotionData { Text = "I'm feeling blue.", Label = "sad" },
            new EmotionData { Text = "I feel terrible.", Label = "sad" },
            new EmotionData { Text = "This is heartbreaking.", Label = "sad" },
            new EmotionData { Text = "I'm feeling very low.", Label = "sad" },
            new EmotionData { Text = "This is so disappointing.", Label = "sad" },
            new EmotionData { Text = "I feel empty inside.", Label = "sad" },
            new EmotionData { Text = "I'm feeling gloomy.", Label = "sad" },
            new EmotionData { Text = "I am going through a breakup.", Label = "sad" },
            new EmotionData { Text = "I'm going through a breakup.", Label = "sad" },
            new EmotionData { Text = "Going through a breakup.", Label = "sad" },
            new EmotionData { Text = "I can't control myself.", Label = "sad" },
            new EmotionData { Text = "I'm going through a breakup and I can't control myself.", Label = "sad" },
            new EmotionData { Text = "My relationship ended and I feel lost.", Label = "sad" },
            new EmotionData { Text = "I'm heartbroken about the breakup.", Label = "sad" },
            new EmotionData { Text = "I can't stop crying after the breakup.", Label = "sad" },
            new EmotionData { Text = "I feel like I'm losing control.", Label = "sad" },
            new EmotionData { Text = "I'm struggling to cope with this breakup.", Label = "sad" },
            new EmotionData { Text = "I feel devastated and out of control.", Label = "sad" },
            new EmotionData { Text = "The breakup is destroying me.", Label = "sad" },
            new EmotionData { Text = "I can't handle this breakup.", Label = "sad" },
            new EmotionData { Text = "I'm overwhelmed with sadness.", Label = "sad" },
            new EmotionData { Text = "I feel bad.", Label = "sad" },
            new EmotionData { Text = "I'm feeling bad.", Label = "sad" },
            new EmotionData { Text = "This makes me sad.", Label = "sad" },
            new EmotionData { Text = "I feel unhappy.", Label = "sad" },
            new EmotionData { Text = "I'm unhappy.", Label = "sad" },
            new EmotionData { Text = "I feel depressed.", Label = "sad" },
            new EmotionData { Text = "I'm depressed.", Label = "sad" },
            new EmotionData { Text = "I miss my love.", Label = "sad" },
            new EmotionData { Text = "I miss my loved one.", Label = "sad" },
            new EmotionData { Text = "I miss someone.", Label = "sad" },
            new EmotionData { Text = "I miss them.", Label = "sad" },
            new EmotionData { Text = "I miss her.", Label = "sad" },
            new EmotionData { Text = "I miss him.", Label = "sad" },
            new EmotionData { Text = "I'm missing someone special.", Label = "sad" },
            new EmotionData { Text = "I miss my partner.", Label = "sad" },
            new EmotionData { Text = "I miss my girlfriend.", Label = "sad" },
            new EmotionData { Text = "I miss my boyfriend.", Label = "sad" },
            new EmotionData { Text = "I miss my family.", Label = "sad" },
            new EmotionData { Text = "I miss my mom.", Label = "sad" },
            new EmotionData { Text = "I miss my dad.", Label = "sad" },
            new EmotionData { Text = "I miss my friend.", Label = "sad" },
            new EmotionData { Text = "I'm missing my love.", Label = "sad" },
            new EmotionData { Text = "Missing my loved one.", Label = "sad" },
            
            // Angry (expanded)
            new EmotionData { Text = "I'm really angry about this!", Label = "angry" },
            new EmotionData { Text = "This makes me furious!", Label = "angry" },
            new EmotionData { Text = "I'm so mad right now!", Label = "angry" },
            new EmotionData { Text = "This is infuriating!", Label = "angry" },
            new EmotionData { Text = "I can't stand this!", Label = "angry" },
            new EmotionData { Text = "This is so annoying!", Label = "angry" },
            new EmotionData { Text = "I'm livid!", Label = "angry" },
            new EmotionData { Text = "This makes me rage!", Label = "angry" },
            new EmotionData { Text = "I'm absolutely furious!", Label = "angry" },
            new EmotionData { Text = "This is unacceptable!", Label = "angry" },
            new EmotionData { Text = "I'm so irritated!", Label = "angry" },
            new EmotionData { Text = "This is outrageous!", Label = "angry" },
            
            // Anxious (expanded)
            new EmotionData { Text = "I'm feeling really anxious.", Label = "anxious" },
            new EmotionData { Text = "This makes me worried.", Label = "anxious" },
            new EmotionData { Text = "I'm stressed about this.", Label = "anxious" },
            new EmotionData { Text = "I'm really nervous.", Label = "anxious" },
            new EmotionData { Text = "This is making me anxious.", Label = "anxious" },
            new EmotionData { Text = "I feel worried about this.", Label = "anxious" },
            new EmotionData { Text = "I'm feeling tense.", Label = "anxious" },
            new EmotionData { Text = "This is stressing me out.", Label = "anxious" },
            new EmotionData { Text = "I'm panicking.", Label = "anxious" },
            new EmotionData { Text = "I feel overwhelmed.", Label = "anxious" },
            new EmotionData { Text = "I'm really concerned.", Label = "anxious" },
            new EmotionData { Text = "This is making me nervous.", Label = "anxious" },
            
            // Calm (expanded - ensure clear distinction from sad)
            new EmotionData { Text = "I feel calm.", Label = "calm" },
            new EmotionData { Text = "I'm calm.", Label = "calm" },
            new EmotionData { Text = "I am calm.", Label = "calm" },
            new EmotionData { Text = "I feel calm and peaceful.", Label = "calm" },
            new EmotionData { Text = "Everything is so serene.", Label = "calm" },
            new EmotionData { Text = "I'm feeling relaxed.", Label = "calm" },
            new EmotionData { Text = "I feel relaxed.", Label = "calm" },
            new EmotionData { Text = "This is very calming.", Label = "calm" },
            new EmotionData { Text = "I feel at peace.", Label = "calm" },
            new EmotionData { Text = "I'm feeling tranquil.", Label = "calm" },
            new EmotionData { Text = "This is so peaceful.", Label = "calm" },
            new EmotionData { Text = "I feel centered.", Label = "calm" },
            new EmotionData { Text = "I'm in a calm state.", Label = "calm" },
            new EmotionData { Text = "I feel balanced.", Label = "calm" },
            new EmotionData { Text = "I feel good.", Label = "calm" },
            new EmotionData { Text = "I'm feeling good.", Label = "calm" },
            new EmotionData { Text = "Everything is fine.", Label = "calm" },
            new EmotionData { Text = "I feel okay.", Label = "calm" },
            new EmotionData { Text = "I'm okay.", Label = "calm" },
            
            // Excited (expanded)
            new EmotionData { Text = "I'm so excited about this!", Label = "excited" },
            new EmotionData { Text = "This is thrilling!", Label = "excited" },
            new EmotionData { Text = "I can't wait for this!", Label = "excited" },
            new EmotionData { Text = "This is so exciting!", Label = "excited" },
            new EmotionData { Text = "I'm pumped up!", Label = "excited" },
            new EmotionData { Text = "I'm ecstatic!", Label = "excited" },
            new EmotionData { Text = "This is exhilarating!", Label = "excited" },
            new EmotionData { Text = "I'm so hyped!", Label = "excited" },
            new EmotionData { Text = "This is amazing!", Label = "excited" },
            new EmotionData { Text = "I'm buzzing with excitement!", Label = "excited" },
            
            // Frustrated (expanded)
            new EmotionData { Text = "I'm frustrated with this.", Label = "frustrated" },
            new EmotionData { Text = "This is so frustrating!", Label = "frustrated" },
            new EmotionData { Text = "I can't figure this out!", Label = "frustrated" },
            new EmotionData { Text = "This is really annoying me.", Label = "frustrated" },
            new EmotionData { Text = "I'm getting frustrated.", Label = "frustrated" },
            new EmotionData { Text = "This is driving me crazy!", Label = "frustrated" },
            new EmotionData { Text = "I'm stuck and frustrated.", Label = "frustrated" },
            new EmotionData { Text = "This is so irritating!", Label = "frustrated" },
            new EmotionData { Text = "I'm feeling blocked.", Label = "frustrated" },
            new EmotionData { Text = "This is really getting to me.", Label = "frustrated" },
            
            // Neutral (expanded - including greetings and casual conversation)
            new EmotionData { Text = "I see.", Label = "neutral" },
            new EmotionData { Text = "Okay, that's fine.", Label = "neutral" },
            new EmotionData { Text = "I understand.", Label = "neutral" },
            new EmotionData { Text = "That makes sense.", Label = "neutral" },
            new EmotionData { Text = "Alright then.", Label = "neutral" },
            new EmotionData { Text = "I see what you mean.", Label = "neutral" },
            new EmotionData { Text = "That's okay.", Label = "neutral" },
            new EmotionData { Text = "I get it.", Label = "neutral" },
            new EmotionData { Text = "Understood.", Label = "neutral" },
            new EmotionData { Text = "That's reasonable.", Label = "neutral" },
            new EmotionData { Text = "I acknowledge that.", Label = "neutral" },
            
            // Greetings (should be neutral, not sad!)
            new EmotionData { Text = "Hello.", Label = "neutral" },
            new EmotionData { Text = "Hello there.", Label = "neutral" },
            new EmotionData { Text = "Hello dear.", Label = "neutral" },
            new EmotionData { Text = "Hi.", Label = "neutral" },
            new EmotionData { Text = "Hi there.", Label = "neutral" },
            new EmotionData { Text = "Hi dear.", Label = "neutral" },
            new EmotionData { Text = "Hey.", Label = "neutral" },
            new EmotionData { Text = "Hey there.", Label = "neutral" },
            new EmotionData { Text = "Hey dear.", Label = "neutral" },
            new EmotionData { Text = "Good morning.", Label = "neutral" },
            new EmotionData { Text = "Good afternoon.", Label = "neutral" },
            new EmotionData { Text = "Good evening.", Label = "neutral" },
            new EmotionData { Text = "How are you?", Label = "neutral" },
            new EmotionData { Text = "How are you doing?", Label = "neutral" },
            new EmotionData { Text = "How's it going?", Label = "neutral" },
            new EmotionData { Text = "What's up?", Label = "neutral" },
            new EmotionData { Text = "Nice to meet you.", Label = "neutral" },
            new EmotionData { Text = "Nice to see you.", Label = "neutral" },
            new EmotionData { Text = "How do you do?", Label = "neutral" },
            
            // Casual conversation starters
            new EmotionData { Text = "What's happening?", Label = "neutral" },
            new EmotionData { Text = "What's going on?", Label = "neutral" },
            new EmotionData { Text = "Tell me about it.", Label = "neutral" },
            new EmotionData { Text = "I'm listening.", Label = "neutral" },
            new EmotionData { Text = "Go ahead.", Label = "neutral" },
            new EmotionData { Text = "Sure thing.", Label = "neutral" },
            new EmotionData { Text = "No problem.", Label = "neutral" },
            new EmotionData { Text = "That's fine.", Label = "neutral" },
            new EmotionData { Text = "Sounds good.", Label = "neutral" },
            new EmotionData { Text = "Okay.", Label = "neutral" },
            new EmotionData { Text = "Alright.", Label = "neutral" },
            new EmotionData { Text = "I'm here.", Label = "neutral" },
            new EmotionData { Text = "I'm ready.", Label = "neutral" },
            new EmotionData { Text = "Let's talk.", Label = "neutral" },
            new EmotionData { Text = "What can I help with?", Label = "neutral" }
        };
    }

    /// <summary>
    /// Generates comprehensive training data with ~1000+ examples covering all emotions and real-world scenarios.
    /// Includes modern language, current events, and real-time emotional expressions.
    /// </summary>
    public static List<EmotionData> GenerateComprehensiveData()
    {
        var data = new List<EmotionData>();
        
        // Start with base examples
        data.AddRange(GenerateSampleData());
        
        // Generate variations programmatically for each emotion
        data.AddRange(GenerateHappyVariations(150));
        data.AddRange(GenerateSadVariations(200));
        data.AddRange(GenerateAngryVariations(120));
        data.AddRange(GenerateAnxiousVariations(130));
        data.AddRange(GenerateCalmVariations(100));
        data.AddRange(GenerateExcitedVariations(100));
        data.AddRange(GenerateFrustratedVariations(100));
        data.AddRange(GenerateNeutralVariations(100));
        
        // Add real-world, modern scenarios
        data.AddRange(GenerateRealWorldScenarios(200));
        
        return data;
    }

    /// <summary>
    /// Generates real-world, modern scenarios with current language and situations.
    /// </summary>
    private static List<EmotionData> GenerateRealWorldScenarios(int count)
    {
        var data = new List<EmotionData>();
        
        // Modern work/school stress scenarios
        var workStress = new[]
        {
            new EmotionData { Text = "I have so much work to do, I'm overwhelmed.", Label = "anxious" },
            new EmotionData { Text = "My boss is driving me crazy today.", Label = "frustrated" },
            new EmotionData { Text = "Deadline is tomorrow and I'm not ready.", Label = "anxious" },
            new EmotionData { Text = "I got the promotion! I'm so excited!", Label = "happy" },
            new EmotionData { Text = "Work is really stressing me out.", Label = "anxious" },
            new EmotionData { Text = "I'm so tired of this job.", Label = "frustrated" },
            new EmotionData { Text = "I aced my exam today!", Label = "happy" },
            new EmotionData { Text = "Failed my test, I feel terrible.", Label = "sad" },
            new EmotionData { Text = "I'm burned out from work.", Label = "frustrated" },
            new EmotionData { Text = "Got a raise! This is amazing!", Label = "happy" }
        };
        data.AddRange(workStress);
        
        // Social media and modern communication
        var socialMedia = new[]
        {
            new EmotionData { Text = "No one liked my post, I feel ignored.", Label = "sad" },
            new EmotionData { Text = "My post went viral! I'm so excited!", Label = "excited" },
            new EmotionData { Text = "Someone was mean to me online.", Label = "sad" },
            new EmotionData { Text = "I got so many messages today!", Label = "happy" },
            new EmotionData { Text = "I'm addicted to my phone and it's stressing me out.", Label = "anxious" },
            new EmotionData { Text = "I deleted social media and feel so much better.", Label = "calm" },
            new EmotionData { Text = "Everyone is posting about their success and I feel left behind.", Label = "sad" },
            new EmotionData { Text = "I'm taking a break from social media.", Label = "calm" }
        };
        data.AddRange(socialMedia);
        
        // Modern relationships and dating
        var relationships = new[]
        {
            new EmotionData { Text = "We broke up and I'm devastated.", Label = "sad" },
            new EmotionData { Text = "I'm going on a date tonight, I'm so nervous!", Label = "anxious" },
            new EmotionData { Text = "I found my soulmate!", Label = "happy" },
            new EmotionData { Text = "They ghosted me and I feel terrible.", Label = "sad" },
            new EmotionData { Text = "I'm in a long-distance relationship and I miss them so much.", Label = "sad" },
            new EmotionData { Text = "We're getting married! I'm overjoyed!", Label = "excited" },
            new EmotionData { Text = "I'm single and actually happy about it.", Label = "happy" },
            new EmotionData { Text = "Dating apps are so frustrating.", Label = "frustrated" },
            new EmotionData { Text = "I'm falling in love and it's amazing.", Label = "happy" },
            new EmotionData { Text = "They cheated on me and I'm heartbroken.", Label = "sad" }
        };
        data.AddRange(relationships);
        
        // Health and wellness
        var health = new[]
        {
            new EmotionData { Text = "I'm worried about my health.", Label = "anxious" },
            new EmotionData { Text = "I started working out and feel great!", Label = "happy" },
            new EmotionData { Text = "I can't sleep and I'm exhausted.", Label = "frustrated" },
            new EmotionData { Text = "I meditated today and feel so calm.", Label = "calm" },
            new EmotionData { Text = "I'm stressed about my weight.", Label = "anxious" },
            new EmotionData { Text = "I feel healthy and energized today!", Label = "happy" },
            new EmotionData { Text = "I'm having panic attacks.", Label = "anxious" },
            new EmotionData { Text = "I took a mental health day and feel refreshed.", Label = "calm" }
        };
        data.AddRange(health);
        
        // Financial stress (very real-world)
        var financial = new[]
        {
            new EmotionData { Text = "I'm worried about money.", Label = "anxious" },
            new EmotionData { Text = "I can't pay my bills and I'm stressed.", Label = "anxious" },
            new EmotionData { Text = "I got a bonus! I'm so relieved!", Label = "happy" },
            new EmotionData { Text = "I'm in debt and it's overwhelming.", Label = "anxious" },
            new EmotionData { Text = "I saved enough for my goal! I'm thrilled!", Label = "excited" },
            new EmotionData { Text = "Money problems are keeping me up at night.", Label = "anxious" }
        };
        data.AddRange(financial);
        
        // Family and friends
        var family = new[]
        {
            new EmotionData { Text = "I miss my family so much.", Label = "sad" },
            new EmotionData { Text = "I'm visiting my parents and I'm so happy!", Label = "happy" },
            new EmotionData { Text = "My friend betrayed me and I'm hurt.", Label = "sad" },
            new EmotionData { Text = "I made new friends and I'm excited!", Label = "excited" },
            new EmotionData { Text = "I'm lonely and it's hard.", Label = "sad" },
            new EmotionData { Text = "Family dinner was amazing today!", Label = "happy" },
            new EmotionData { Text = "I'm having a fight with my best friend.", Label = "sad" },
            new EmotionData { Text = "My friend surprised me and I'm so touched!", Label = "happy" }
        };
        data.AddRange(family);
        
        // Modern life challenges
        var modernLife = new[]
        {
            new EmotionData { Text = "I'm stuck in traffic and I'm frustrated.", Label = "frustrated" },
            new EmotionData { Text = "I'm working from home and I love it!", Label = "happy" },
            new EmotionData { Text = "I'm isolated and feeling down.", Label = "sad" },
            new EmotionData { Text = "I'm learning a new skill and I'm excited!", Label = "excited" },
            new EmotionData { Text = "I'm comparing myself to others and it's making me sad.", Label = "sad" },
            new EmotionData { Text = "I'm practicing self-care and feel balanced.", Label = "calm" },
            new EmotionData { Text = "I'm overwhelmed by all my responsibilities.", Label = "anxious" },
            new EmotionData { Text = "I set boundaries and feel so much better.", Label = "calm" },
            new EmotionData { Text = "I'm trying to balance everything and it's exhausting.", Label = "frustrated" },
            new EmotionData { Text = "I'm taking things one day at a time.", Label = "calm" }
        };
        data.AddRange(modernLife);
        
        // Current events and world situations
        var currentEvents = new[]
        {
            new EmotionData { Text = "I'm worried about the future.", Label = "anxious" },
            new EmotionData { Text = "The news is making me anxious.", Label = "anxious" },
            new EmotionData { Text = "I'm grateful for what I have today.", Label = "happy" },
            new EmotionData { Text = "Everything feels uncertain and it's scary.", Label = "anxious" },
            new EmotionData { Text = "I'm focusing on the present moment.", Label = "calm" },
            new EmotionData { Text = "I'm hopeful about tomorrow.", Label = "happy" }
        };
        data.AddRange(currentEvents);
        
        // Modern slang and casual expressions
        var modernSlang = new[]
        {
            new EmotionData { Text = "I'm vibing today!", Label = "happy" },
            new EmotionData { Text = "This is giving me anxiety.", Label = "anxious" },
            new EmotionData { Text = "I'm not okay right now.", Label = "sad" },
            new EmotionData { Text = "This is lowkey stressing me out.", Label = "anxious" },
            new EmotionData { Text = "I'm so hyped about this!", Label = "excited" },
            new EmotionData { Text = "I'm done with this.", Label = "frustrated" },
            new EmotionData { Text = "This hits different today.", Label = "happy" },
            new EmotionData { Text = "I'm having a rough day.", Label = "sad" },
            new EmotionData { Text = "I'm living my best life!", Label = "happy" },
            new EmotionData { Text = "I'm just trying to get through the day.", Label = "frustrated" }
        };
        data.AddRange(modernSlang);
        
        // Technology and digital life
        var technology = new[]
        {
            new EmotionData { Text = "My computer crashed and I lost my work, I'm so frustrated!", Label = "frustrated" },
            new EmotionData { Text = "I got a new phone and I'm so excited!", Label = "excited" },
            new EmotionData { Text = "I'm spending too much time online and I feel empty.", Label = "sad" },
            new EmotionData { Text = "I unplugged for the weekend and feel refreshed.", Label = "calm" },
            new EmotionData { Text = "I'm addicted to scrolling and it's making me anxious.", Label = "anxious" }
        };
        data.AddRange(technology);
        
        // Achievements and milestones
        var achievements = new[]
        {
            new EmotionData { Text = "I graduated and I'm so proud!", Label = "happy" },
            new EmotionData { Text = "I got rejected and I'm disappointed.", Label = "sad" },
            new EmotionData { Text = "I accomplished my goal and I'm thrilled!", Label = "excited" },
            new EmotionData { Text = "I didn't get what I wanted and I'm sad.", Label = "sad" },
            new EmotionData { Text = "I'm celebrating a milestone today!", Label = "happy" }
        };
        data.AddRange(achievements);
        
        // Random real-world scenarios to fill remaining count
        var random = new Random(42);
        var remaining = count - data.Count;
        if (remaining > 0)
        {
            var allScenarios = new List<EmotionData>();
            allScenarios.AddRange(workStress);
            allScenarios.AddRange(socialMedia);
            allScenarios.AddRange(relationships);
            allScenarios.AddRange(health);
            allScenarios.AddRange(financial);
            allScenarios.AddRange(family);
            allScenarios.AddRange(modernLife);
            allScenarios.AddRange(currentEvents);
            allScenarios.AddRange(modernSlang);
            allScenarios.AddRange(technology);
            allScenarios.AddRange(achievements);
            
            for (int i = 0; i < remaining && allScenarios.Count > 0; i++)
            {
                var scenario = allScenarios[random.Next(allScenarios.Count)];
                data.Add(scenario);
            }
        }
        
        return data;
    }

    private static List<EmotionData> GenerateHappyVariations(int count)
    {
        var data = new List<EmotionData>();
        var templates = new[]
        {
            "I'm {0}!", "I feel {0}!", "I am {0}!", "I'm so {0}!", "I'm really {0}!",
            "This is {0}!", "That's {0}!", "How {0}!", "So {0}!", "Very {0}!",
            "I'm feeling {0} today!", "I feel {0} right now!", "I'm {0} about this!",
            "This makes me {0}!", "I'm {0} because of this!", "I'm {0} to hear that!"
        };
        
        var words = new[] { "happy", "joyful", "delighted", "thrilled", "ecstatic", "elated", 
                           "cheerful", "glad", "pleased", "content", "blissful", "jubilant",
                           "excited", "overjoyed", "euphoric", "radiant", "upbeat", "positive" };
        
        var random = new Random(42); // Fixed seed for reproducibility
        for (int i = 0; i < count; i++)
        {
            var template = templates[random.Next(templates.Length)];
            var word = words[random.Next(words.Length)];
            data.Add(new EmotionData { Text = string.Format(template, word), Label = "happy" });
        }
        
        return data;
    }

    private static List<EmotionData> GenerateSadVariations(int count)
    {
        var data = new List<EmotionData>();
        var templates = new[]
        {
            "I'm {0}.", "I feel {0}.", "I am {0}.", "I'm so {0}.", "I'm really {0}.",
            "This is {0}.", "That's {0}.", "I'm feeling {0}.", "I feel {0} right now.",
            "This makes me {0}.", "I'm {0} about this.", "I'm {0} because of this.",
            "I miss {0}.", "I'm missing {0}.", "Missing {0}.", "I miss my {0}.",
            "I'm going through {0}.", "I'm dealing with {0}.", "I'm struggling with {0}."
        };
        
        var words = new[] { "sad", "down", "upset", "depressed", "miserable", "heartbroken",
                           "devastated", "disappointed", "unhappy", "gloomy", "melancholy",
                           "blue", "low", "empty", "hopeless", "despair", "grief", "sorrow" };
        
        var missWords = new[] { "my love", "my loved one", "someone", "them", "her", "him",
                               "my partner", "my family", "my friend", "my mom", "my dad" };
        
        var random = new Random(42);
        for (int i = 0; i < count; i++)
        {
            var template = templates[random.Next(templates.Length)];
            if (template.Contains("{0}") && template.Contains("miss"))
            {
                var word = missWords[random.Next(missWords.Length)];
                data.Add(new EmotionData { Text = string.Format(template, word), Label = "sad" });
            }
            else
            {
                var word = words[random.Next(words.Length)];
                data.Add(new EmotionData { Text = string.Format(template, word), Label = "sad" });
            }
        }
        
        return data;
    }

    private static List<EmotionData> GenerateAngryVariations(int count)
    {
        var data = new List<EmotionData>();
        var templates = new[]
        {
            "I'm {0}!", "I feel {0}!", "I am {0}!", "I'm so {0}!", "I'm really {0}!",
            "This is {0}!", "That's {0}!", "I'm {0} about this!", "This makes me {0}!",
            "I'm {0} right now!", "I'm absolutely {0}!", "I'm {0} at this!"
        };
        
        var words = new[] { "angry", "mad", "furious", "livid", "irritated", "annoyed",
                           "frustrated", "enraged", "outraged", "infuriated", "aggravated" };
        
        var random = new Random(42);
        for (int i = 0; i < count; i++)
        {
            var template = templates[random.Next(templates.Length)];
            var word = words[random.Next(words.Length)];
            data.Add(new EmotionData { Text = string.Format(template, word), Label = "angry" });
        }
        
        return data;
    }

    private static List<EmotionData> GenerateAnxiousVariations(int count)
    {
        var data = new List<EmotionData>();
        var templates = new[]
        {
            "I'm {0}.", "I feel {0}.", "I am {0}.", "I'm so {0}.", "I'm really {0}.",
            "This is making me {0}.", "I'm feeling {0}.", "I feel {0} about this.",
            "I'm {0} right now.", "This makes me {0}.", "I'm getting {0}."
        };
        
        var words = new[] { "anxious", "worried", "nervous", "stressed", "tense", "panicked",
                           "overwhelmed", "concerned", "uneasy", "apprehensive", "fearful" };
        
        var random = new Random(42);
        for (int i = 0; i < count; i++)
        {
            var template = templates[random.Next(templates.Length)];
            var word = words[random.Next(words.Length)];
            data.Add(new EmotionData { Text = string.Format(template, word), Label = "anxious" });
        }
        
        return data;
    }

    private static List<EmotionData> GenerateCalmVariations(int count)
    {
        var data = new List<EmotionData>();
        var templates = new[]
        {
            "I'm {0}.", "I feel {0}.", "I am {0}.", "I'm so {0}.", "I feel {0} and {1}.",
            "This is {0}.", "I'm feeling {0}.", "I feel {0} right now.", "Everything is {0}."
        };
        
        var words = new[] { "calm", "relaxed", "peaceful", "serene", "tranquil", "centered",
                           "balanced", "at ease", "comfortable", "content", "still" };
        
        var random = new Random(42);
        for (int i = 0; i < count; i++)
        {
            var template = templates[random.Next(templates.Length)];
            if (template.Contains("{1}"))
            {
                var word1 = words[random.Next(words.Length)];
                var word2 = words[random.Next(words.Length)];
                data.Add(new EmotionData { Text = string.Format(template, word1, word2), Label = "calm" });
            }
            else
            {
                var word = words[random.Next(words.Length)];
                data.Add(new EmotionData { Text = string.Format(template, word), Label = "calm" });
            }
        }
        
        return data;
    }

    private static List<EmotionData> GenerateExcitedVariations(int count)
    {
        var data = new List<EmotionData>();
        var templates = new[]
        {
            "I'm {0}!", "I feel {0}!", "I am {0}!", "I'm so {0}!", "I'm really {0}!",
            "This is {0}!", "I'm {0} about this!", "This makes me {0}!", "I can't wait!"
        };
        
        var words = new[] { "excited", "thrilled", "pumped", "hyped", "ecstatic", "eager",
                           "enthusiastic", "energized", "animated", "jubilant", "elated" };
        
        var random = new Random(42);
        for (int i = 0; i < count; i++)
        {
            var template = templates[random.Next(templates.Length)];
            var word = words[random.Next(words.Length)];
            data.Add(new EmotionData { Text = string.Format(template, word), Label = "excited" });
        }
        
        return data;
    }

    private static List<EmotionData> GenerateFrustratedVariations(int count)
    {
        var data = new List<EmotionData>();
        var templates = new[]
        {
            "I'm {0}.", "I feel {0}.", "I am {0}.", "I'm so {0}!", "This is {0}!",
            "I'm {0} with this.", "This makes me {0}.", "I'm getting {0}.", "I can't {0}!"
        };
        
        var words = new[] { "frustrated", "annoyed", "irritated", "stuck", "blocked",
                           "impatient", "exasperated", "aggravated", "bothered" };
        
        var random = new Random(42);
        for (int i = 0; i < count; i++)
        {
            var template = templates[random.Next(templates.Length)];
            var word = words[random.Next(words.Length)];
            data.Add(new EmotionData { Text = string.Format(template, word), Label = "frustrated" });
        }
        
        return data;
    }

    private static List<EmotionData> GenerateNeutralVariations(int count)
    {
        var data = new List<EmotionData>();
        var templates = new[]
        {
            "{0}.", "I {0}.", "That's {0}.", "I see.", "Okay.", "Alright.", "I understand.",
            "That makes sense.", "I get it.", "Understood.", "Sure.", "Fine.", "No problem."
        };
        
        var words = new[] { "see", "understand", "get it", "acknowledge", "note" };
        
        var greetings = new[] { "Hello", "Hi", "Hey", "Good morning", "Good afternoon", 
                               "Good evening", "How are you", "What's up", "Nice to meet you" };
        
        var random = new Random(42);
        for (int i = 0; i < count; i++)
        {
            if (i % 3 == 0 && greetings.Length > 0)
            {
                data.Add(new EmotionData { Text = greetings[random.Next(greetings.Length)], Label = "neutral" });
            }
            else
            {
                var template = templates[random.Next(templates.Length)];
                if (template.Contains("{0}"))
                {
                    var word = words[random.Next(words.Length)];
                    data.Add(new EmotionData { Text = string.Format(template, word), Label = "neutral" });
                }
                else
                {
                    data.Add(new EmotionData { Text = template, Label = "neutral" });
                }
            }
        }
        
        return data;
    }
}

