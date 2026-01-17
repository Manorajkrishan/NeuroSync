using Xunit;
using FluentAssertions;
using NeuroSync.Api.Services;
using NeuroSync.Core;
using NeuroSync.IoT;
using Microsoft.Extensions.Logging;
using Moq;

namespace NeuroSync.Api.Tests;

/// <summary>
/// Comprehensive tests for DecisionEngine
/// </summary>
public class DecisionEngineTests
{
    private readonly Mock<ILogger<DecisionEngine>> _loggerMock;
    private readonly IoTDeviceSimulator _iotSimulator;
    private readonly DecisionEngine _decisionEngine;
    private readonly Mock<ConversationMemory> _conversationMemoryMock;
    private readonly Mock<EmotionalIntelligence> _emotionalIntelligenceMock;

    public DecisionEngineTests()
    {
        _loggerMock = new Mock<ILogger<DecisionEngine>>();
        _iotSimulator = new IoTDeviceSimulator();
        _conversationMemoryMock = new Mock<ConversationMemory>();
        _emotionalIntelligenceMock = new Mock<EmotionalIntelligence>(Mock.Of<ILogger<EmotionalIntelligence>>());
        
        _decisionEngine = new DecisionEngine(
            _iotSimulator,
            null,
            _loggerMock.Object,
            _conversationMemoryMock.Object,
            _emotionalIntelligenceMock.Object
        );
    }

    [Theory]
    [InlineData(EmotionType.Happy)]
    [InlineData(EmotionType.Sad)]
    [InlineData(EmotionType.Angry)]
    [InlineData(EmotionType.Anxious)]
    [InlineData(EmotionType.Calm)]
    [InlineData(EmotionType.Excited)]
    [InlineData(EmotionType.Frustrated)]
    [InlineData(EmotionType.Neutral)]
    public async Task GetIoTActionsAsync_ShouldReturnActionsForAllEmotions(EmotionType emotion)
    {
        // Act
        var actions = await _decisionEngine.GetIoTActionsAsync(emotion);

        // Assert
        actions.Should().NotBeNull();
        actions.Should().NotBeEmpty();
        actions.Should().OnlyContain(a => !string.IsNullOrEmpty(a.DeviceId));
        actions.Should().OnlyContain(a => !string.IsNullOrEmpty(a.ActionType));
    }

    [Fact]
    public async Task GetIoTActionsAsync_WithAllEmotions_ShouldReturnValidActions()
    {
        // Arrange
        var emotions = Enum.GetValues<EmotionType>();
        var allActions = new List<IoTAction>();

        // Act
        foreach (var emotion in emotions)
        {
            var actions = await _decisionEngine.GetIoTActionsAsync(emotion);
            allActions.AddRange(actions);
        }

        // Assert
        allActions.Should().NotBeEmpty();
        allActions.Should().OnlyContain(a => a != null);
        allActions.Should().OnlyContain(a => !string.IsNullOrEmpty(a.DeviceId));
        allActions.Should().OnlyContain(a => !string.IsNullOrEmpty(a.ActionType));
    }

    [Fact]
    public void GenerateResponse_ShouldReturnValidResponse()
    {
        // Arrange
        var emotionResult = new EmotionResult
        {
            Emotion = EmotionType.Happy,
            Confidence = 0.95f,
            OriginalText = "I'm so happy!"
        };

        _emotionalIntelligenceMock
            .Setup(e => e.GenerateEmpatheticMessage(It.IsAny<EmotionType>(), It.IsAny<ConversationContext?>()))
            .Returns("I'm glad you're feeling happy!");

        // Act
        var response = _decisionEngine.GenerateResponse(emotionResult, "test-user", "I'm so happy!");

        // Assert
        response.Should().NotBeNull();
        response.Emotion.Should().Be(EmotionType.Happy);
        response.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateResponse_WithAllEmotions_ShouldGenerateValidResponses()
    {
        // Arrange
        var emotions = Enum.GetValues<EmotionType>();
        _emotionalIntelligenceMock
            .Setup(e => e.GenerateEmpatheticMessage(It.IsAny<EmotionType>(), It.IsAny<ConversationContext?>()))
            .Returns<EmotionType, ConversationContext?>((e, c) => $"Response for {e}");

        // Act & Assert
        foreach (var emotion in emotions)
        {
            var emotionResult = new EmotionResult
            {
                Emotion = emotion,
                Confidence = 0.8f,
                OriginalText = $"I'm feeling {emotion}"
            };

            var response = _decisionEngine.GenerateResponse(emotionResult, "test-user", $"I'm feeling {emotion}");
            response.Should().NotBeNull();
            response.Emotion.Should().Be(emotion);
            response.Message.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void GenerateResponse_ShouldHandleNullUserId()
    {
        // Arrange
        var emotionResult = new EmotionResult
        {
            Emotion = EmotionType.Happy,
            Confidence = 0.95f,
            OriginalText = "I'm happy!"
        };

        _emotionalIntelligenceMock
            .Setup(e => e.GenerateEmpatheticMessage(It.IsAny<EmotionType>(), It.IsAny<ConversationContext?>()))
            .Returns("Response");

        // Act
        var response = _decisionEngine.GenerateResponse(emotionResult, null, "I'm happy!");

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public void GenerateResponse_ShouldHandleNullUserMessage()
    {
        // Arrange
        var emotionResult = new EmotionResult
        {
            Emotion = EmotionType.Happy,
            Confidence = 0.95f,
            OriginalText = "I'm happy!"
        };

        _emotionalIntelligenceMock
            .Setup(e => e.GenerateEmpatheticMessage(It.IsAny<EmotionType>(), It.IsAny<ConversationContext?>()))
            .Returns("Response");

        // Act
        var response = _decisionEngine.GenerateResponse(emotionResult, "test-user", null);

        // Assert
        response.Should().NotBeNull();
    }
}
