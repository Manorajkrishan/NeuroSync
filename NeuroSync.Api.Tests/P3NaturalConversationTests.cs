using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeuroSync.Api.Services;
using NeuroSync.Core;
using NeuroSync.IoT;
using Xunit;

namespace NeuroSync.Api.Tests;

public class P3NaturalConversationTests
{
    private readonly IntentRouterService _router = new();

    [Theory]
    [InlineData("i am good and u", UserIntent.CasualConversation)]
    [InlineData("I'm good, and you?", UserIntent.CasualConversation)]
    [InlineData("nee epdi irukka", UserIntent.CasualConversation)]
    [InlineData("vanakkam", UserIntent.Greeting)]
    [InlineData("romba tired da", UserIntent.EmotionalDisclosure)]
    [InlineData("work pressure romba irukku", UserIntent.EmotionalDisclosure)]
    [InlineData("thaniya feel panren", UserIntent.EmotionalDisclosure)]
    [InlineData("manasu seri illa", UserIntent.EmotionalDisclosure)]
    public void Router_ShouldUnderstandNaturalEnglishAndTanglish(string text, UserIntent expected)
    {
        _router.Detect(text).Should().Be(expected);
    }

    [Fact]
    public void Router_ShortCauseAnswer_ShouldContinueRecentWellbeingThread()
    {
        var recent = new List<CompanionConversationTurn>
        {
            new() { Role = "user", Text = "i am so tired today" },
            new() { Role = "assistant", Text = "What drained you most?" }
        };

        _router.Detect("work", recentTurns: recent)
            .Should().Be(UserIntent.EmotionalDisclosure);
    }

    [Fact]
    public async Task PositiveCheckIn_ShouldAnswerNaturally()
    {
        var provider = new TemplateCompanionProvider();
        var reply = await provider.GenerateAsync(new CompanionContext
        {
            CurrentMessage = "i am good and u",
            Intent = UserIntent.CasualConversation,
            Locale = "en",
            Mode = CompanionInteractionMode.Talk
        });

        var lower = reply.Message.ToLowerInvariant();
        lower.Should().NotContain("not sure");
        lower.Should().NotContain("clarify");
        (lower.Contains("good") || lower.Contains("glad") || lower.Contains("nice"))
            .Should().BeTrue($"unexpected positive check-in reply: {reply.Message}");
    }

    [Fact]
    public async Task TanglishLocale_ShouldUseTanglishStyleReply()
    {
        var provider = new TemplateCompanionProvider();
        var reply = await provider.GenerateAsync(new CompanionContext
        {
            CurrentMessage = "romba tired da",
            Intent = UserIntent.EmotionalDisclosure,
            Locale = "tanglish",
            Mode = CompanionInteractionMode.Listen,
            Uncertainty = UncertaintyLevel.Uncertain
        });

        var lower = reply.Message.ToLowerInvariant();
        (lower.Contains("da") || lower.Contains("enna") || lower.Contains("romba") ||
         lower.Contains("sleep") || lower.Contains("mind"))
            .Should().BeTrue($"expected a Tanglish-friendly reply, got: {reply.Message}");
        lower.Should().NotContain("clarify how you're feeling");
        lower.Should().NotContain("not sure i have enough");
    }

    [Fact]
    public async Task CauseAnswer_AfterFatigue_ShouldContinueSpecificThread()
    {
        var provider = new TemplateCompanionProvider();
        var reply = await provider.GenerateAsync(new CompanionContext
        {
            CurrentMessage = "work",
            Intent = UserIntent.EmotionalDisclosure,
            Locale = "en",
            Mode = CompanionInteractionMode.Listen,
            RecentTurns = new List<CompanionConversationTurn>
            {
                new() { Role = "user", Text = "i am so tired today" },
                new() { Role = "assistant", Text = "What drained you most?" }
            }
        });

        var lower = reply.Message.ToLowerInvariant();
        lower.Should().Contain("work");
        (lower.Contains("workload") || lower.Contains("people") || lower.Contains("nonstop"))
            .Should().BeTrue($"unexpected continuation reply: {reply.Message}");
    }

    [Fact]
    public async Task UnknownUncertainMessage_ShouldNotExposeModelUncertainty()
    {
        var provider = new TemplateCompanionProvider();
        var reply = await provider.GenerateAsync(new CompanionContext
        {
            CurrentMessage = "today was weird",
            Intent = UserIntent.Unknown,
            Locale = "en",
            Mode = CompanionInteractionMode.Talk,
            Uncertainty = UncertaintyLevel.InsufficientEvidence
        });

        var lower = reply.Message.ToLowerInvariant();
        lower.Should().NotContain("not sure i have enough");
        lower.Should().NotContain("reading this wrong");
        lower.Should().NotContain("clarify how you're feeling");
        lower.Should().MatchRegex("tell|got|what|more");
    }

    [Fact]
    public void DecisionEngine_ShouldUseEphemeralSessionForShortFollowUp()
    {
        var ephemeral = new EphemeralSessionContextService();
        var policy = new ResponsePolicyService();
        var template = new TemplateCompanionProvider();
        var responder = new CompanionResponseService(policy, template);
        var memory = CompanionTestFactory.CreateMemory();

        var engine = new DecisionEngine(
            new IoTDeviceSimulator(),
            null,
            Mock.Of<ILogger<DecisionEngine>>(),
            new SafetyGateService(Mock.Of<ILogger<SafetyGateService>>()),
            new IntentRouterService(),
            new CompanionModeService(),
            policy,
            responder,
            memory,
            null,
            new EmotionalBaselineService(memory, Mock.Of<ILogger<EmotionalBaselineService>>()),
            null,
            new EmotionalIntelligence(Mock.Of<ILogger<EmotionalIntelligence>>()),
            template,
            ephemeral);

        const string userId = "p3-user";
        const string sessionId = "p3-session";
        ephemeral.AppendTurn(userId, sessionId, "i am so tired today", "What drained you most?");

        var emotion = new EmotionResult(EmotionType.Neutral, 0.2f, "work")
        {
            Uncertainty = UncertaintyLevel.InsufficientEvidence
        };

        var response = engine.GenerateResponse(emotion, userId, "work", sessionId);

        response.Parameters["intent"].ToString().Should().Be(nameof(UserIntent.EmotionalDisclosure));
        var lower = response.Message.ToLowerInvariant();
        lower.Should().Contain("work");
        lower.Should().NotContain("not sure i have enough");
    }
}
