using Xunit;
using FluentAssertions;
using NeuroSync.Api.Services;
using NeuroSync.Core;
using NeuroSync.IoT;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace NeuroSync.Api.Tests;

/// <summary>
/// Tests for DecisionEngine
/// </summary>
public class DecisionEngineTests
{
    private readonly DecisionEngine _decisionEngine;
    private readonly EmotionalIntelligence _emotionalIntelligence;

    public DecisionEngineTests()
    {
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Throws(new InvalidOperationException("no db in tests"));
        var conversationMemory = new ConversationMemory(
            Mock.Of<ILogger<ConversationMemory>>(),
            scopeFactory.Object);
        _emotionalIntelligence = new EmotionalIntelligence(Mock.Of<ILogger<EmotionalIntelligence>>());

        _decisionEngine = new DecisionEngine(
            new IoTDeviceSimulator(),
            null,
            Mock.Of<ILogger<DecisionEngine>>(),
            conversationMemory,
            _emotionalIntelligence
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
        var actions = await _decisionEngine.GetIoTActionsAsync(emotion);

        actions.Should().NotBeNull();
        actions.Should().NotBeEmpty();
        actions.Should().OnlyContain(a => !string.IsNullOrEmpty(a.DeviceId));
        actions.Should().OnlyContain(a => !string.IsNullOrEmpty(a.ActionType));
    }

    [Fact]
    public async Task GetIoTActionsAsync_WithAllEmotions_ShouldReturnValidActions()
    {
        foreach (var emotion in Enum.GetValues<EmotionType>())
        {
            var actions = await _decisionEngine.GetIoTActionsAsync(emotion);
            actions.Should().NotBeNull();
            actions.Should().NotBeEmpty();
        }
    }

    [Fact]
    public void GenerateResponse_ShouldReturnValidResponse()
    {
        var emotionResult = new EmotionResult
        {
            Emotion = EmotionType.Happy,
            Confidence = 0.95f,
            OriginalText = "I'm so happy!"
        };

        var response = _decisionEngine.GenerateResponse(emotionResult, "test-user", "I'm so happy!");

        response.Should().NotBeNull();
        response.Emotion.Should().Be(EmotionType.Happy);
        response.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateResponse_WithAllEmotions_ShouldGenerateValidResponses()
    {
        foreach (var emotion in Enum.GetValues<EmotionType>())
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
        var emotionResult = new EmotionResult
        {
            Emotion = EmotionType.Happy,
            Confidence = 0.95f,
            OriginalText = "I'm happy!"
        };

        var response = _decisionEngine.GenerateResponse(emotionResult, null, "I'm happy!");
        response.Should().NotBeNull();
    }

    [Fact]
    public void GenerateResponse_ShouldHandleNullUserMessage()
    {
        var emotionResult = new EmotionResult
        {
            Emotion = EmotionType.Happy,
            Confidence = 0.95f,
            OriginalText = "I'm happy!"
        };

        var response = _decisionEngine.GenerateResponse(emotionResult, "test-user", null);
        response.Should().NotBeNull();
    }

    [Fact]
    public void GenerateResponse_Hi_ShouldUseConverseAction()
    {
        var emotionResult = new EmotionResult(EmotionType.Neutral, 0.9f, "hi");
        var response = _decisionEngine.GenerateResponse(emotionResult, "u1", "hi");
        response.Action.Should().Be("converse");
    }
}
