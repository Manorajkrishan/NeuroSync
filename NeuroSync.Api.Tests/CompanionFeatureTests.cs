using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NeuroSync.Api.Services;
using NeuroSync.Core;
using Xunit;

namespace NeuroSync.Api.Tests;

/// <summary>
/// Test cases for best-friend companion: emotion understanding, greetings/IoT,
/// loneliness support, device sync, and facial wellbeing.
/// </summary>
public class CompanionFeatureTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly Mock<IWebHostEnvironment> _env;

    public CompanionFeatureTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "neurosync-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        Directory.CreateDirectory(Path.Combine(_tempRoot, "UserProfiles"));
        Directory.CreateDirectory(Path.Combine(_tempRoot, "ConnectedDevices"));

        _env = new Mock<IWebHostEnvironment>();
        _env.Setup(e => e.ContentRootPath).Returns(_tempRoot);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }
        catch { /* ignore cleanup */ }
    }

    // ---------- Emotion understanding ----------

    [Theory]
    [InlineData("i feel really sad about my exam", EmotionType.Sad, "school")]
    [InlineData("work is stressing me out", EmotionType.Anxious, "work")]
    [InlineData("i'm so excited about cricket", EmotionType.Excited, null)]
    [InlineData("hi", EmotionType.Neutral, null)]
    [InlineData("my name is Krishan", EmotionType.Neutral, null)]
    public void EmotionUnderstanding_ShouldDetectEmotionAndCause(string text, EmotionType expected, string? causeContains)
    {
        var svc = new EmotionUnderstandingService(Mock.Of<ILogger<EmotionUnderstandingService>>());
        var ml = new EmotionResult(EmotionType.Happy, 0.4f, text); // deliberately wrong ML guess

        var result = svc.Understand(ml, text);

        result.Emotion.Should().Be(expected);
        result.UnderstoodAs.Should().NotBeNullOrWhiteSpace();
        result.Intensity.Should().BeOneOf("mild", "moderate", "intense");
        if (causeContains != null)
            result.LikelyCause.Should().ContainEquivalentOf(causeContains);
    }

    [Fact]
    public void EmotionUnderstanding_IntenseWords_ShouldMarkIntense()
    {
        var svc = new EmotionUnderstandingService(Mock.Of<ILogger<EmotionUnderstandingService>>());
        var result = svc.Understand(new EmotionResult(EmotionType.Neutral, 0.5f), "i feel extremely sad and overwhelmed");

        result.Emotion.Should().Be(EmotionType.Sad);
        result.Intensity.Should().Be("intense");
    }

    // ---------- Greeting / IoT gate ----------

    [Theory]
    [InlineData("hi", true)]
    [InlineData("hello", true)]
    [InlineData("hey", true)]
    [InlineData("how are you", true)]
    [InlineData("good morning", true)]
    [InlineData("i feel sad today", false)]
    [InlineData("play music please", false)]
    public void IsGreetingOrSmallTalk_ShouldClassify(string text, bool expected)
    {
        EmotionalIntelligence.IsGreetingOrSmallTalk(text).Should().Be(expected);
    }

    [Theory]
    [InlineData("play music", true)]
    [InlineData("turn on the lights", true)]
    [InlineData("hi", false)]
    [InlineData("i feel lonely", false)]
    public void ShouldTriggerIoT_OnlyWhenAsked(string text, bool expected)
    {
        DecisionEngine.ShouldTriggerIoT(text).Should().Be(expected);
        EmotionalIntelligence.IsIoTRequest(text).Should().Be(expected);
    }

    // ---------- Loneliness / best friend ----------

    [Theory]
    [InlineData("I feel so lonely")]
    [InlineData("I have no one to talk to")]
    [InlineData("nobody cares about me")]
    [InlineData("I don't have anyone")]
    public void EmotionalIntelligence_Loneliness_ShouldInviteSharing(string text)
    {
        var ei = new EmotionalIntelligence(Mock.Of<ILogger<EmotionalIntelligence>>());
        var msg = ei.GenerateEmpatheticMessage(EmotionType.Sad, null, text);

        msg.Should().NotBeNullOrWhiteSpace();
        msg.ToLowerInvariant().Should().Match(m =>
            m.Contains("alone") || m.Contains("here") || m.Contains("listen") || m.Contains("share") || m.Contains("friend"));
    }

    [Fact]
    public void Greeting_ShouldNotAskTherapyFollowUp()
    {
        var ei = new EmotionalIntelligence(Mock.Of<ILogger<EmotionalIntelligence>>());
        var q = ei.GenerateFollowUpQuestion(EmotionType.Neutral, "hi", null);
        q.Should().BeNull();
    }

    [Fact]
    public void CrisisKeywords_ShouldFlagImmediateSupport()
    {
        var ei = new EmotionalIntelligence(Mock.Of<ILogger<EmotionalIntelligence>>());
        ei.NeedsImmediateSupport("I want to kill myself").Should().BeTrue();
        ei.NeedsImmediateSupport("hi how are you").Should().BeFalse();
    }

    // ---------- Device sync ----------

    [Fact]
    public void DeviceSync_Pairing_ShouldShareOwnerAcrossDevices()
    {
        var profiles = new UserProfileService(Mock.Of<ILogger<UserProfileService>>(), _env.Object);
        profiles.LearnFromConversation("owner_test", "my name is Alex", EmotionType.Neutral);

        var memory = CreateMemory();
        var sync = new DeviceSyncService(profiles, memory, _env.Object, Mock.Of<ILogger<DeviceSyncService>>());

        sync.RegisterDevice("owner_test", new DeviceRegisterRequest
        {
            DeviceId = "pc-1",
            DeviceName = "Windows PC",
            Platform = "windows"
        });

        var code = sync.CreatePairingCode("owner_test");
        code.PairingCode.Should().HaveLength(6);

        var joined = sync.JoinWithPairingCode(code.PairingCode, new DeviceRegisterRequest
        {
            DeviceId = "phone-1",
            DeviceName = "iPhone",
            Platform = "ios"
        });

        joined.Should().NotBeNull();
        joined!.UserId.Should().Be("owner_test");
        joined.Devices.Should().HaveCount(2);
        joined.PreferredName.Should().BeEquivalentTo("Alex");
        joined.Message.Should().ContainEquivalentOf("synced");
    }

    [Fact]
    public void DeviceSync_InvalidCode_ShouldReturnNull()
    {
        var profiles = new UserProfileService(Mock.Of<ILogger<UserProfileService>>(), _env.Object);
        var sync = new DeviceSyncService(profiles, CreateMemory(), _env.Object, Mock.Of<ILogger<DeviceSyncService>>());

        var result = sync.JoinWithPairingCode("000000", new DeviceRegisterRequest { DeviceId = "x", Platform = "ios" });
        result.Should().BeNull();
    }

    // ---------- Facial wellbeing ----------

    [Fact]
    public void FacialWellbeing_TiredEyes_ShouldGiveHumanHealthReply()
    {
        var profiles = new UserProfileService(Mock.Of<ILogger<UserProfileService>>(), _env.Object);
        profiles.LearnFromConversation("face_user", "my name is Sam", EmotionType.Neutral);

        var facial = new FacialWellbeingService(profiles, Mock.Of<ILogger<FacialWellbeingService>>());
        var response = facial.BuildReaction("face_user", new FacialEmotionRequest
        {
            Emotion = "Sad",
            Confidence = 0.9f,
            EyeContactScore = 0.2f,
            FaceMotionScore = 0.1f,
            GazeState = "eyes_closed_or_down",
            Engagement = "disengaged",
            CueNotes = "Eyes look heavy"
        }, EmotionType.Sad);

        response.Message.Should().NotBeNullOrWhiteSpace();
        response.Action.Should().Be("wellbeing_checkin");
        response.Parameters.Should().ContainKey("healthFocus");
        response.Parameters!["mode"].Should().Be("real_person_wellbeing");
        var message = response.Message!.ToLowerInvariant();
        message.Should().Match(m =>
            m.Contains("tired") || m.Contains("drain") || m.Contains("eyes") || m.Contains("sleep") || m.Contains("here") || m.Contains("sam"));
    }

    [Fact]
    public void DecisionEngine_Greeting_ShouldConverseWithoutForcingIoTPath()
    {
        var ei = new EmotionalIntelligence(Mock.Of<ILogger<EmotionalIntelligence>>());
        var memory = CreateMemory();
        var engine = new DecisionEngine(
            new NeuroSync.IoT.IoTDeviceSimulator(),
            null,
            Mock.Of<ILogger<DecisionEngine>>(),
            memory,
            ei,
            null,
            new SafetyGateService(ei, Mock.Of<ILogger<SafetyGateService>>()),
            new CompanionModeService(),
            new EmotionalBaselineService(memory, Mock.Of<ILogger<EmotionalBaselineService>>()));

        var result = new EmotionResult(EmotionType.Neutral, 0.9f, "hi")
        {
            UnderstoodAs = "I understand you're feeling okay / neutral."
        };
        var response = engine.GenerateResponse(result, "greet_user", "hi");

        response.Action.Should().Be("converse");
        response.Message.Should().NotBeNullOrWhiteSpace();
        response.Parameters.Should().ContainKey("disclaimer");
        DecisionEngine.ShouldTriggerIoT("hi").Should().BeFalse();
    }

    [Fact]
    public void SafetyGate_CrisisLanguage_ShouldBlockNormalFlow()
    {
        var ei = new EmotionalIntelligence(Mock.Of<ILogger<EmotionalIntelligence>>());
        var gate = new SafetyGateService(ei, Mock.Of<ILogger<SafetyGateService>>());
        var assessment = gate.Assess("I want to end my life");
        assessment.Level.Should().Be(SafetyLevel.ImmediateDanger);
        assessment.BlockNormalCompanionFlow.Should().BeTrue();
    }

    [Fact]
    public void CompanionMode_QuietRequest_ShouldResolveCalm()
    {
        var modes = new CompanionModeService();
        modes.Resolve("please quiet mode", EmotionType.Anxious, SafetyLevel.Normal)
            .Should().Be(CompanionInteractionMode.Calm);
        modes.Resolve("just listen please", EmotionType.Sad, SafetyLevel.Normal)
            .Should().Be(CompanionInteractionMode.Listen);
    }

    [Fact]
    public void Understanding_TiredOfEverything_ShouldAddMultiSignals()
    {
        var understanding = new EmotionUnderstandingService(Mock.Of<ILogger<EmotionUnderstandingService>>());
        var ml = new EmotionResult(EmotionType.Neutral, 0.4f, "I'm tired of everything");
        var result = understanding.Understand(ml, "I'm tired of everything");
        result.SignalEstimates.Should().ContainKey("Fatigue");
        result.Disclaimer.Should().Contain("does not diagnose");
    }

    private ConversationMemory CreateMemory()
    {
        var scopeFactory = new Mock<IServiceScopeFactory>();
        // LoadContextFromDb will fail gracefully without a real DbContext scope
        scopeFactory.Setup(f => f.CreateScope()).Throws(new InvalidOperationException("no db in unit test"));
        return new ConversationMemory(Mock.Of<ILogger<ConversationMemory>>(), scopeFactory.Object);
    }
}
