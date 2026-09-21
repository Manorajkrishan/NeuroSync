using Xunit;
using FluentAssertions;
using NeuroSync.Api.Services;
using NeuroSync.Core;

namespace NeuroSync.Api.Tests;

public class DecisionEngineTests
{
    private readonly DecisionEngine _decisionEngine = CompanionTestFactory.CreateEngine();

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
        response.Message.Should().NotBeNullOrEmpty();
        response.Message.Should().NotContain("I sense you're feeling");
        response.Parameters.Should().ContainKey("intent");
        response.Parameters.Should().ContainKey("developerInsights");
    }

    [Fact]
    public void GenerateResponse_Hi_ShouldUseConverseAction()
    {
        var emotionResult = new EmotionResult(EmotionType.Neutral, 0.9f, "hi");
        var response = _decisionEngine.GenerateResponse(emotionResult, "u1", "hi");
        response.Action.Should().Be("converse");
        response.Parameters["intent"].ToString().Should().Be(UserIntent.Greeting.ToString());
        response.Message.Should().NotContain("%");
    }

    [Fact]
    public void GenerateResponse_ShouldHandleNullUserId()
    {
        var emotionResult = new EmotionResult { Emotion = EmotionType.Happy, Confidence = 0.9f, OriginalText = "hi" };
        _decisionEngine.GenerateResponse(emotionResult, null, "hi").Should().NotBeNull();
    }

    [Fact]
    public void GenerateResponse_ShouldHandleNullUserMessage()
    {
        var emotionResult = new EmotionResult { Emotion = EmotionType.Happy, Confidence = 0.9f, OriginalText = "ok" };
        _decisionEngine.GenerateResponse(emotionResult, "u", null).Should().NotBeNull();
    }
}
