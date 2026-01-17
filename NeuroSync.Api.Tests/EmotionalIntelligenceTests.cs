using Xunit;
using FluentAssertions;
using NeuroSync.Api.Services;
using NeuroSync.Core;
using Microsoft.Extensions.Logging;
using Moq;

namespace NeuroSync.Api.Tests;

/// <summary>
/// Comprehensive tests for EmotionalIntelligence
/// </summary>
public class EmotionalIntelligenceTests
{
    private readonly Mock<ILogger<EmotionalIntelligence>> _loggerMock;
    private readonly EmotionalIntelligence _emotionalIntelligence;

    public EmotionalIntelligenceTests()
    {
        _loggerMock = new Mock<ILogger<EmotionalIntelligence>>();
        _emotionalIntelligence = new EmotionalIntelligence(_loggerMock.Object);
    }

    [Theory]
    [InlineData(EmotionType.Happy)]
    [InlineData(EmotionType.Sad)]
    [InlineData(EmotionType.Angry)]
    [InlineData(EmotionType.Anxious)]
    [InlineData(EmotionType.Calm)]
    [InlineData(EmotionType.Excited)]
    [InlineData(EmotionType.Frustrated)]
    public void GenerateEmpatheticMessage_ShouldReturnNonEmptyMessage(EmotionType emotion)
    {
        // Act
        var message = _emotionalIntelligence.GenerateEmpatheticMessage(emotion);

        // Assert
        message.Should().NotBeNullOrEmpty();
        message.Length.Should().BeGreaterThan(10);
    }

    [Fact]
    public void GenerateEmpatheticMessage_ShouldReturnDifferentMessages()
    {
        // Arrange
        var messages = new HashSet<string>();
        const int iterations = 100;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var message = _emotionalIntelligence.GenerateEmpatheticMessage(EmotionType.Happy);
            messages.Add(message);
        }

        // Assert - Should have some variety (not all identical)
        messages.Count.Should().BeGreaterThan(1, "Messages should have some variety");
    }

    [Fact]
    public void GenerateFollowUpQuestion_ShouldReturnValidQuestion()
    {
        // Act
        var question = _emotionalIntelligence.GenerateFollowUpQuestion(EmotionType.Sad, "I'm feeling down", null);

        // Assert
        question.Should().NotBeNullOrEmpty();
        question.Should().ContainAny("?", "What", "How", "Would");
    }

    [Fact]
    public void GenerateFollowUpQuestion_WithAllEmotions_ShouldReturnQuestions()
    {
        // Arrange
        var emotions = new[] { EmotionType.Happy, EmotionType.Sad, EmotionType.Angry, EmotionType.Anxious };

        // Act & Assert
        foreach (var emotion in emotions)
        {
            var question = _emotionalIntelligence.GenerateFollowUpQuestion(emotion, $"I'm feeling {emotion}", null);
            question.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void GenerateEncouragement_WithMinimalContext_ShouldReturnNull()
    {
        // Arrange
        var context = new ConversationContext
        {
            ConversationCount = 1,
            History = new List<ConversationEntry>()
        };

        // Act
        var encouragement = _emotionalIntelligence.GenerateEncouragement(context);

        // Assert
        encouragement.Should().BeNull();
    }

    [Fact]
    public void GenerateEncouragement_WithNegativePattern_ShouldReturnEncouragement()
    {
        // Arrange
        var context = new ConversationContext
        {
            ConversationCount = 5,
            History = new List<ConversationEntry>
            {
                new ConversationEntry
                {
                    DetectedEmotion = new EmotionResult { Emotion = EmotionType.Sad, Confidence = 0.8f }
                },
                new ConversationEntry
                {
                    DetectedEmotion = new EmotionResult { Emotion = EmotionType.Sad, Confidence = 0.9f }
                },
                new ConversationEntry
                {
                    DetectedEmotion = new EmotionResult { Emotion = EmotionType.Anxious, Confidence = 0.85f }
                },
                new ConversationEntry
                {
                    DetectedEmotion = new EmotionResult { Emotion = EmotionType.Sad, Confidence = 0.75f }
                }
            }
        };

        // Act
        var encouragement = _emotionalIntelligence.GenerateEncouragement(context);

        // Assert
        encouragement.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void NeedsImmediateSupport_WithCrisisKeywords_ShouldReturnTrue()
    {
        // Arrange
        var context = new ConversationContext
        {
            History = new List<ConversationEntry>
            {
                new ConversationEntry
                {
                    UserMessage = "I want to hurt myself",
                    DetectedEmotion = new EmotionResult { Emotion = EmotionType.Sad, Confidence = 0.95f }
                }
            }
        };

        // Act
        var needsSupport = _emotionalIntelligence.NeedsImmediateSupport(EmotionType.Sad, 0.95f, context);

        // Assert
        needsSupport.Should().BeTrue();
    }

    [Fact]
    public void NeedsImmediateSupport_WithoutCrisisKeywords_ShouldReturnFalse()
    {
        // Arrange
        var context = new ConversationContext
        {
            History = new List<ConversationEntry>
            {
                new ConversationEntry
                {
                    UserMessage = "I'm feeling sad today",
                    DetectedEmotion = new EmotionResult { Emotion = EmotionType.Sad, Confidence = 0.8f }
                }
            }
        };

        // Act
        var needsSupport = _emotionalIntelligence.NeedsImmediateSupport(EmotionType.Sad, 0.8f, context);

        // Assert
        needsSupport.Should().BeFalse();
    }
}
