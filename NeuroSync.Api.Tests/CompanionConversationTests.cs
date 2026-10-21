using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeuroSync.Api.Services;
using NeuroSync.Core;
using Xunit;
namespace NeuroSync.Api.Tests;

/// <summary>
/// Conversation-level companion tests — NeuroSync must feel like a companion, not an emotion dashboard.
/// </summary>
public class CompanionConversationTests
{
    private readonly DecisionEngine _engine = CompanionTestFactory.CreateEngine();
    private readonly IntentRouterService _intents = new();

    [Fact]
    public void TEST_COMP_001_Hi_ShouldGreetWithoutEmotionClaim()
    {
        var emotion = new EmotionResult(EmotionType.Neutral, 0.98f, "hi");
        var response = _engine.GenerateResponse(emotion, "c1", "hi");

        response.Parameters["intent"].ToString().Should().Be(nameof(UserIntent.Greeting));
        response.Message.Should().NotBeNullOrWhiteSpace();
        response.Message.ToLowerInvariant().Should().NotContain("sense you're feeling");
        response.Message.Should().NotMatchRegex(@"\d{1,3}%");
        response.Parameters["exposedEmotionToUser"].Should().Be(false);
    }

    [Fact]
    public void TEST_COMP_002_HowAreYou_ShouldBeSocialNotCalmDiagnosis()
    {
        var emotion = new EmotionResult(EmotionType.Calm, 0.99f, "how are you");
        var response = _engine.GenerateResponse(emotion, "c2", "how are you");

        response.Parameters["intent"].ToString().Should().Be(nameof(UserIntent.CasualConversation));
        response.Message.ToLowerInvariant().Should().NotContain("i sense");
        response.Message.Should().NotContain("99%");
    }

    [Fact]
    public void TEST_COMP_003_IMissHer_ShouldListenEmpathically()
    {
        var emotion = new EmotionResult(EmotionType.Sad, 0.9f, "i miss her")
        {
            Uncertainty = UncertaintyLevel.HighConfidence
        };
        var response = _engine.GenerateResponse(emotion, "c3", "i miss her");

        response.Parameters["intent"].ToString().Should().Be(nameof(UserIntent.EmotionalDisclosure));
        response.Message.ToLowerInvariant().Should().NotContain("i sense you're feeling sad");
        response.Message.Should().NotContain("90%");
        var lower = response.Message.ToLowerInvariant();
        (lower.Contains("miss") || lower.Contains("listen") || lower.Contains("talk") || lower.Contains("company")
         || lower.Contains("hard") || lower.Contains("here") || lower.Contains("hit") || lower.Contains("night"))
            .Should().BeTrue($"unexpected companion reply: {response.Message}");
    }

    [Fact]
    public void TEST_COMP_004_LightsRequest_ShouldAskPermission()
    {
        var emotion = new EmotionResult(EmotionType.Neutral, 0.5f, "turn my lights down");
        var response = _engine.GenerateResponse(emotion, "c4", "turn my lights down");

        response.Parameters["intent"].ToString().Should().Be(nameof(UserIntent.EnvironmentAction));
        DecisionEngine.ShouldTriggerIoT("turn my lights down").Should().BeTrue();
        // Without IoTConsent, DecisionEngine marks blocked (consent null in factory = treated as no IoT)
        // Companion asks permission rather than claiming execution
        response.Message.ToLowerInvariant().Should().MatchRegex("quiet|enable|privacy|want me|lights|environment|yes");
    }

    [Fact]
    public void TEST_COMP_005_IoTConsentOff_MarksBlockedNotExecute()
    {
        var temp = Path.Combine(Path.GetTempPath(), "ns-iot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            env.Setup(e => e.ContentRootPath).Returns(temp);
            var consent = new EthicalAIFrameworkService(
                Mock.Of<ILogger<EthicalAIFrameworkService>>(), env.Object);
            // Default IoTConsent is false
            consent.HasConsent("iot_off_user", ConsentType.IoT).Should().BeFalse();

            var engine = new DecisionEngine(
                new NeuroSync.IoT.IoTDeviceSimulator(),
                null,
                Mock.Of<ILogger<DecisionEngine>>(),
                new SafetyGateService(Mock.Of<ILogger<SafetyGateService>>()),
                new IntentRouterService(),
                new CompanionModeService(),
                new ResponsePolicyService(),
                new CompanionResponseService(new ResponsePolicyService(), new TemplateCompanionProvider()),
                CompanionTestFactory.CreateMemory(),
                null,
                null,
                consent);

            var emotion = new EmotionResult(EmotionType.Neutral, 0.5f, "turn on the lights");
            var response = engine.GenerateResponse(emotion, "iot_off_user", "turn on the lights");

            response.Parameters.Should().ContainKey("iotBlocked");
            response.Parameters["iotBlocked"].ToString().Should().Contain("IoTConsent");
            // Trace must not claim IoT execute when consent off
            var trace = response.Parameters["decisionTrace"]?.ToString() ?? "";
            trace.Should().NotContain("IoTOnRequest");
        }
        finally
        {
            try { Directory.Delete(temp, true); } catch { /* ignore */ }
        }
    }

    [Theory]
    [InlineData("hi", UserIntent.Greeting)]
    [InlineData("how are you", UserIntent.CasualConversation)]
    [InlineData("i miss her", UserIntent.EmotionalDisclosure)]
    [InlineData("just listen to me", UserIntent.ListeningRequest)]
    [InlineData("what should i do about this", UserIntent.AdviceRequest)]
    [InlineData("i am so tired today", UserIntent.EmotionalDisclosure)]
    [InlineData("I feel completely drained", UserIntent.EmotionalDisclosure)]
    [InlineData("yes pls", UserIntent.CasualConversation)]
    public void IntentRouter_ShouldClassifyCoreIntents(string text, UserIntent expected)
    {
        _intents.Detect(text).Should().Be(expected);
    }

    [Fact]
    public void TiredStatement_ShouldRespondToFatigue_NotClassifierUncertainty()
    {
        var emotion = new EmotionResult(EmotionType.Neutral, 0.2f, "i am so tired today")
        {
            Uncertainty = UncertaintyLevel.InsufficientEvidence
        };

        var response = _engine.GenerateResponse(emotion, "fatigue-user", "i am so tired today");

        response.Parameters["intent"].ToString().Should().Be(nameof(UserIntent.EmotionalDisclosure));
        var lower = response.Message.ToLowerInvariant();
        lower.Should().NotContain("not sure i have enough");
        lower.Should().NotContain("clarify how you're feeling");
        (lower.Contains("tired") || lower.Contains("exhaust") || lower.Contains("drain") ||
         lower.Contains("long day") || lower.Contains("sleep") || lower.Contains("wore you out"))
            .Should().BeTrue($"unexpected fatigue reply: {response.Message}");
    }

    [Fact]
    public async Task YesPlease_AfterTiredTurn_ShouldContinueTheThread()
    {
        var provider = new TemplateCompanionProvider();
        var context = new CompanionContext
        {
            CurrentMessage = "yes pls",
            Intent = UserIntent.CasualConversation,
            Mode = CompanionInteractionMode.Talk,
            Uncertainty = UncertaintyLevel.InsufficientEvidence,
            RecentTurns = new List<CompanionConversationTurn>
            {
                new() { Role = "user", Text = "i am so tired today", Timestamp = DateTime.UtcNow.AddSeconds(-2) },
                new() { Role = "assistant", Text = "Sounds like a long day. Want to talk about it?", Timestamp = DateTime.UtcNow.AddSeconds(-1) }
            }
        };

        var reply = await provider.GenerateAsync(context);
        var lower = reply.Message.ToLowerInvariant();

        lower.Should().NotContain("not sure i have enough");
        (lower.Contains("tired") || lower.Contains("drain") || lower.Contains("sleep") ||
         lower.Contains("physical") || lower.Contains("mental") || lower.Contains("wore you out"))
            .Should().BeTrue($"unexpected continuation reply: {reply.Message}");
    }

    [Fact]
    public void ResponsePolicy_Sanitize_RemovesConfidenceLeakage()
    {
        var policySvc = new ResponsePolicyService();
        var ctx = new CompanionContext
        {
            CurrentMessage = "hi",
            Intent = UserIntent.Greeting,
            Uncertainty = UncertaintyLevel.InsufficientEvidence
        };
        var p = policySvc.Evaluate(ctx);
        var cleaned = policySvc.Sanitize("I sense you're feeling sad (98% confidence). Want to talk?", p);
        cleaned.ToLowerInvariant().Should().NotContain("sense you're feeling");
        cleaned.Should().NotContain("98%");
    }
}
