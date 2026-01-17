using Xunit;
using FluentAssertions;
using NeuroSync.Api.Services;
using NeuroSync.Core;
using NeuroSync.IoT;
using Microsoft.Extensions.Logging;
using Moq;

namespace NeuroSync.Api.Tests;

/// <summary>
/// Comprehensive system tests with 1000 test cases
/// Tests the entire system end-to-end
/// </summary>
public class ComprehensiveSystemTests
{
    private readonly EmotionDetectionService _emotionDetectionService;
    private readonly DecisionEngine _decisionEngine;
    private readonly EmotionalIntelligence _emotionalIntelligence;
    private readonly IoTDeviceSimulator _iotSimulator;
    private readonly List<SystemTestCase> _systemTestCases;

    public ComprehensiveSystemTests()
    {
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var emotionLogger = loggerFactory.CreateLogger<EmotionDetectionService>();
        var decisionLogger = loggerFactory.CreateLogger<DecisionEngine>();
        var emotionalLogger = loggerFactory.CreateLogger<EmotionalIntelligence>();
        var conversationLogger = loggerFactory.CreateLogger<ConversationMemory>();
        
        var cache = new PredictionCache();
        var testModel = TestHelper.GetTestModel();
        _emotionDetectionService = new EmotionDetectionService(testModel, emotionLogger, cache);
        _iotSimulator = new IoTDeviceSimulator();
        _emotionalIntelligence = new EmotionalIntelligence(emotionalLogger);
        
        var conversationMemory = new ConversationMemory(conversationLogger);
        _decisionEngine = new DecisionEngine(
            _iotSimulator,
            null,
            decisionLogger,
            conversationMemory,
            _emotionalIntelligence
        );

        _systemTestCases = GenerateSystemTestCases();
    }

    [Fact]
    public void ComprehensiveSystemTest_With1000TestCases_ShouldPass()
    {
        // Arrange
        var results = new List<SystemTestResult>();
        var startTime = DateTime.UtcNow;
        int successCount = 0;
        int failureCount = 0;

        // Act
        foreach (var testCase in _systemTestCases)
        {
            try
            {
                // Step 1: Detect emotion
                var emotionResult = _emotionDetectionService.DetectEmotion(testCase.Input);

                // Step 2: Generate response
                var response = _decisionEngine.GenerateResponse(
                    emotionResult,
                    testCase.UserId,
                    testCase.Input
                );

                // Step 3: Get IoT actions
                var iotActions = _decisionEngine.GetIoTActionsAsync(emotionResult.Emotion).Result;

                // Step 4: Generate follow-up question
                var followUp = _emotionalIntelligence.GenerateFollowUpQuestion(
                    emotionResult.Emotion,
                    testCase.Input,
                    null
                );

                var result = new SystemTestResult
                {
                    TestCase = testCase,
                    EmotionResult = emotionResult,
                    Response = response,
                    IoTActions = iotActions,
                    FollowUpQuestion = followUp,
                    Success = true
                };

                results.Add(result);
                successCount++;
            }
            catch (Exception ex)
            {
                results.Add(new SystemTestResult
                {
                    TestCase = testCase,
                    Success = false,
                    Error = ex.Message
                });
                failureCount++;
            }
        }

        var endTime = DateTime.UtcNow;
        var duration = (endTime - startTime).TotalMilliseconds;

        // Assert
        results.Should().HaveCount(1000);
        successCount.Should().BeGreaterThan(950, $"Expected > 95% success rate, got {successCount}/1000");
        failureCount.Should().BeLessThan(50, $"Expected < 5% failure rate, got {failureCount}/1000");
        
        // Performance check
        duration.Should().BeLessThan(30000, $"1000 system tests took {duration}ms, should be < 30000ms");
        
        // Validate results
        var successfulResults = results.Where(r => r.Success).ToList();
        successfulResults.Should().OnlyContain(r => r.EmotionResult != null);
        successfulResults.Should().OnlyContain(r => r.Response != null);
        successfulResults.Should().OnlyContain(r => r.IoTActions != null && r.IoTActions.Any());
        successfulResults.Should().OnlyContain(r => r.EmotionResult!.Confidence >= 0.0f && r.EmotionResult!.Confidence <= 1.0f);
    }

    [Fact]
    public void SystemPerformanceTest_With1000Iterations_ShouldCompleteInReasonableTime()
    {
        // Arrange
        const int iterations = 1000;
        var testInput = "I'm feeling happy today!";
        var startTime = DateTime.UtcNow;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var emotionResult = _emotionDetectionService.DetectEmotion(testInput);
            var response = _decisionEngine.GenerateResponse(emotionResult, "test-user", testInput);
            var iotActions = _decisionEngine.GetIoTActionsAsync(emotionResult.Emotion).Result;
        }

        var endTime = DateTime.UtcNow;
        var duration = (endTime - startTime).TotalMilliseconds;
        var avgDuration = duration / iterations;

        // Assert
        duration.Should().BeLessThan(15000, $"1000 iterations took {duration}ms");
        avgDuration.Should().BeLessThan(15, $"Average time per iteration: {avgDuration}ms");
    }

    [Fact]
    public void SystemAccuracyTest_WithKnownEmotions_ShouldDetectCorrectly()
    {
        // Arrange
        var knownTestCases = new[]
        {
            new { Input = "I'm so happy!", ExpectedEmotion = EmotionType.Happy },
            new { Input = "I feel sad", ExpectedEmotion = EmotionType.Sad },
            new { Input = "I'm angry", ExpectedEmotion = EmotionType.Angry },
            new { Input = "I'm anxious", ExpectedEmotion = EmotionType.Anxious },
            new { Input = "I'm calm", ExpectedEmotion = EmotionType.Calm },
            new { Input = "I'm excited!", ExpectedEmotion = EmotionType.Excited },
            new { Input = "I'm frustrated", ExpectedEmotion = EmotionType.Frustrated }
        };

        // Act & Assert
        foreach (var testCase in knownTestCases)
        {
            var emotionResult = _emotionDetectionService.DetectEmotion(testCase.Input);
            emotionResult.Emotion.Should().Be(testCase.ExpectedEmotion, 
                $"Input: '{testCase.Input}' should detect {testCase.ExpectedEmotion}");
            emotionResult.Confidence.Should().BeGreaterThan(0.5f, 
                $"Confidence should be > 0.5 for '{testCase.Input}'");
        }
    }

    private List<SystemTestCase> GenerateSystemTestCases()
    {
        var testCases = new List<SystemTestCase>();
        var random = new Random(42); // Fixed seed for reproducibility

        // Generate 1000 diverse test cases
        var emotionTemplates = new Dictionary<EmotionType, string[]>
        {
            { EmotionType.Happy, new[] { "I'm happy", "I'm glad", "I'm delighted", "I'm thrilled", "I'm ecstatic" } },
            { EmotionType.Sad, new[] { "I'm sad", "I'm down", "I'm depressed", "I'm miserable", "I'm heartbroken" } },
            { EmotionType.Angry, new[] { "I'm angry", "I'm mad", "I'm furious", "I'm enraged", "I'm livid" } },
            { EmotionType.Anxious, new[] { "I'm anxious", "I'm worried", "I'm nervous", "I'm stressed", "I'm tense" } },
            { EmotionType.Calm, new[] { "I'm calm", "I'm peaceful", "I'm relaxed", "I'm serene", "I'm tranquil" } },
            { EmotionType.Excited, new[] { "I'm excited", "I'm pumped", "I'm enthusiastic", "I'm eager", "I'm energized" } },
            { EmotionType.Frustrated, new[] { "I'm frustrated", "I'm annoyed", "I'm irritated", "I'm bothered", "I'm aggravated" } },
            { EmotionType.Neutral, new[] { "I'm okay", "I'm fine", "I'm alright", "I'm normal", "Nothing special" } }
        };

        var variations = new[]
        {
            "", " right now", " today", " lately", " recently",
            " and I don't know why", " but it's okay", " which is unusual",
            " a lot", " so much", " very much", " extremely"
        };

        for (int i = 0; i < 1000; i++)
        {
            var emotion = (EmotionType)(i % Enum.GetValues<EmotionType>().Length);
            var templates = emotionTemplates.ContainsKey(emotion) 
                ? emotionTemplates[emotion] 
                : emotionTemplates[EmotionType.Neutral];
            
            var template = templates[random.Next(templates.Length)];
            var variation = variations[random.Next(variations.Length)];
            var input = template + variation;

            testCases.Add(new SystemTestCase
            {
                Input = input,
                UserId = $"user-{i % 100}",
                ExpectedEmotion = emotion
            });
        }

        return testCases;
    }
}

public class SystemTestCase
{
    public string Input { get; set; } = string.Empty;
    public string UserId { get; set; } = "default";
    public EmotionType ExpectedEmotion { get; set; }
}

public class SystemTestResult
{
    public SystemTestCase TestCase { get; set; } = null!;
    public EmotionResult? EmotionResult { get; set; }
    public AdaptiveResponse? Response { get; set; }
    public List<IoTAction>? IoTActions { get; set; }
    public string? FollowUpQuestion { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}
