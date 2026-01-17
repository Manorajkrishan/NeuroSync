using Xunit;
using FluentAssertions;
using NeuroSync.Api.Services;
using NeuroSync.Core;
using Microsoft.Extensions.Logging;
using Moq;
using NeuroSync.ML;

namespace NeuroSync.Api.Tests;

/// <summary>
/// Comprehensive tests for EmotionDetectionService
/// </summary>
public class EmotionDetectionServiceTests
{
    private readonly Mock<ILogger<EmotionDetectionService>> _loggerMock;
    private readonly EmotionDetectionService _service;
    private readonly List<EmotionTestCase> _testCases;

    public EmotionDetectionServiceTests()
    {
        _loggerMock = new Mock<ILogger<EmotionDetectionService>>();
        var testModel = TestHelper.GetTestModel();
        var cache = new PredictionCache(); // Real instance, not mocked
        _service = new EmotionDetectionService(testModel, _loggerMock.Object, cache);
        _testCases = GenerateEmotionTestCases();
    }

    [Theory]
    [MemberData(nameof(EmotionTestData))]
    public void DetectEmotion_ShouldReturnExpectedEmotion(string input, EmotionType expectedEmotion, float minConfidence)
    {
        // Act
        var result = _service.DetectEmotion(input);

        // Assert
        result.Should().NotBeNull();
        result.Emotion.Should().Be(expectedEmotion);
        result.Confidence.Should().BeGreaterThanOrEqualTo(minConfidence);
        result.OriginalText.Should().Be(input);
    }

    [Fact]
    public void DetectEmotion_With1000TestCases_ShouldPerformWell()
    {
        // Arrange
        var results = new List<EmotionResult>();
        var startTime = DateTime.UtcNow;

        // Act
        foreach (var testCase in _testCases)
        {
            var result = _service.DetectEmotion(testCase.Text);
            results.Add(result);
        }

        var endTime = DateTime.UtcNow;
        var duration = (endTime - startTime).TotalMilliseconds;

        // Assert
        results.Should().HaveCount(1000);
        results.Should().OnlyContain(r => r != null);
        results.Should().OnlyContain(r => r.Confidence >= 0.0f && r.Confidence <= 1.0f);
        
        // Performance check: Should complete 1000 tests in reasonable time (< 10 seconds)
        duration.Should().BeLessThan(10000, $"1000 emotion detections took {duration}ms, should be less than 10000ms");
    }

    [Fact]
    public void DetectEmotion_ShouldHandleEmptyString()
    {
        // Act
        var result = _service.DetectEmotion("");

        // Assert
        result.Should().NotBeNull();
        result.OriginalText.Should().Be("");
    }

    [Fact]
    public void DetectEmotion_ShouldHandleNullString()
    {
        // Act
        var result = _service.DetectEmotion(null!);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void DetectEmotion_ShouldHandleVeryLongString()
    {
        // Arrange
        var longText = new string('a', 10000) + " I am very happy and excited about this wonderful day!";

        // Act
        var result = _service.DetectEmotion(longText);

        // Assert
        result.Should().NotBeNull();
        result.Confidence.Should().BeGreaterThanOrEqualTo(0.0f);
    }

    [Fact]
    public void DetectEmotion_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var specialText = "I'm feeling 😊 happy!!! What about you? @#$%^&*()";

        // Act
        var result = _service.DetectEmotion(specialText);

        // Assert
        result.Should().NotBeNull();
        result.Confidence.Should().BeGreaterThanOrEqualTo(0.0f);
    }

    [Fact]
    public void DetectEmotion_ShouldHandleMixedLanguages()
    {
        // Arrange
        var mixedText = "I'm feeling triste (sad in Spanish) and happy at the same time";

        // Act
        var result = _service.DetectEmotion(mixedText);

        // Assert
        result.Should().NotBeNull();
        result.Confidence.Should().BeGreaterThanOrEqualTo(0.0f);
    }

    public static IEnumerable<object[]> EmotionTestData()
    {
        var testCases = new List<object[]>
        {
            new object[] { "I am so happy!", EmotionType.Happy, 0.5f },
            new object[] { "I feel sad", EmotionType.Sad, 0.5f },
            new object[] { "I'm angry", EmotionType.Angry, 0.5f },
            new object[] { "I'm anxious", EmotionType.Anxious, 0.5f },
            new object[] { "I'm calm", EmotionType.Calm, 0.5f },
            new object[] { "I'm excited!", EmotionType.Excited, 0.5f },
            new object[] { "I'm frustrated", EmotionType.Frustrated, 0.5f },
        };
        
        return testCases;
    }

    private List<EmotionTestCase> GenerateEmotionTestCases()
    {
        var testCases = new List<EmotionTestCase>();
        var random = new Random(42); // Fixed seed for reproducibility

        // Happy emotions (200 cases)
        var happyPhrases = new[]
        {
            "I'm so happy!", "I'm feeling great!", "This is wonderful!", "I'm delighted!",
            "I'm overjoyed!", "I'm thrilled!", "I'm ecstatic!", "I'm elated!",
            "I'm cheerful!", "I'm glad!", "I'm pleased!", "I'm content!",
            "I'm satisfied!", "I'm upbeat!", "I'm optimistic!", "I'm positive!",
            "I'm joyful!", "I'm merry!", "I'm jubilant!", "I'm blissful!"
        };
        for (int i = 0; i < 200; i++)
        {
            testCases.Add(new EmotionTestCase
            {
                Text = happyPhrases[random.Next(happyPhrases.Length)] + " " + GenerateVariation(random),
                ExpectedEmotion = EmotionType.Happy
            });
        }

        // Sad emotions (200 cases)
        var sadPhrases = new[]
        {
            "I'm sad", "I'm feeling down", "I'm depressed", "I'm miserable",
            "I'm unhappy", "I'm gloomy", "I'm blue", "I'm dejected",
            "I'm despondent", "I'm melancholy", "I'm sorrowful", "I'm tearful",
            "I'm heartbroken", "I'm devastated", "I'm crushed", "I'm disappointed",
            "I'm discouraged", "I'm disheartened", "I'm forlorn", "I'm desolate"
        };
        for (int i = 0; i < 200; i++)
        {
            testCases.Add(new EmotionTestCase
            {
                Text = sadPhrases[random.Next(sadPhrases.Length)] + " " + GenerateVariation(random),
                ExpectedEmotion = EmotionType.Sad
            });
        }

        // Angry emotions (150 cases)
        var angryPhrases = new[]
        {
            "I'm angry", "I'm furious", "I'm enraged", "I'm mad",
            "I'm livid", "I'm irate", "I'm incensed", "I'm outraged",
            "I'm fuming", "I'm seething", "I'm annoyed", "I'm irritated",
            "I'm frustrated", "I'm aggravated", "I'm vexed", "I'm provoked"
        };
        for (int i = 0; i < 150; i++)
        {
            testCases.Add(new EmotionTestCase
            {
                Text = angryPhrases[random.Next(angryPhrases.Length)] + " " + GenerateVariation(random),
                ExpectedEmotion = EmotionType.Angry
            });
        }

        // Anxious emotions (150 cases)
        var anxiousPhrases = new[]
        {
            "I'm anxious", "I'm worried", "I'm nervous", "I'm stressed",
            "I'm tense", "I'm uneasy", "I'm restless", "I'm apprehensive",
            "I'm concerned", "I'm troubled", "I'm agitated", "I'm jittery",
            "I'm panicky", "I'm on edge", "I'm overwhelmed", "I'm distraught"
        };
        for (int i = 0; i < 150; i++)
        {
            testCases.Add(new EmotionTestCase
            {
                Text = anxiousPhrases[random.Next(anxiousPhrases.Length)] + " " + GenerateVariation(random),
                ExpectedEmotion = EmotionType.Anxious
            });
        }

        // Calm emotions (100 cases)
        var calmPhrases = new[]
        {
            "I'm calm", "I'm peaceful", "I'm relaxed", "I'm serene",
            "I'm tranquil", "I'm composed", "I'm collected", "I'm at ease",
            "I'm comfortable", "I'm content", "I'm balanced", "I'm centered"
        };
        for (int i = 0; i < 100; i++)
        {
            testCases.Add(new EmotionTestCase
            {
                Text = calmPhrases[random.Next(calmPhrases.Length)] + " " + GenerateVariation(random),
                ExpectedEmotion = EmotionType.Calm
            });
        }

        // Excited emotions (100 cases)
        var excitedPhrases = new[]
        {
            "I'm excited", "I'm pumped", "I'm enthusiastic", "I'm eager",
            "I'm energized", "I'm fired up", "I'm motivated", "I'm stoked",
            "I'm hyped", "I'm exhilarated", "I'm animated", "I'm vivacious"
        };
        for (int i = 0; i < 100; i++)
        {
            testCases.Add(new EmotionTestCase
            {
                Text = excitedPhrases[random.Next(excitedPhrases.Length)] + " " + GenerateVariation(random),
                ExpectedEmotion = EmotionType.Excited
            });
        }

        // Frustrated emotions (100 cases)
        var frustratedPhrases = new[]
        {
            "I'm frustrated", "I'm annoyed", "I'm irritated", "I'm bothered",
            "I'm aggravated", "I'm vexed", "I'm exasperated", "I'm fed up",
            "I'm impatient", "I'm testy", "I'm cross", "I'm peeved"
        };
        for (int i = 0; i < 100; i++)
        {
            testCases.Add(new EmotionTestCase
            {
                Text = frustratedPhrases[random.Next(frustratedPhrases.Length)] + " " + GenerateVariation(random),
                ExpectedEmotion = EmotionType.Frustrated
            });
        }

        return testCases;
    }

    private string GenerateVariation(Random random)
    {
        var variations = new[]
        {
            "right now", "today", "lately", "recently",
            "a lot", "so much", "very much", "extremely",
            "and I don't know why", "but it's okay", "which is unusual", ""
        };
        return variations[random.Next(variations.Length)];
    }
}

public class EmotionTestCase
{
    public string Text { get; set; } = string.Empty;
    public EmotionType ExpectedEmotion { get; set; }
}
