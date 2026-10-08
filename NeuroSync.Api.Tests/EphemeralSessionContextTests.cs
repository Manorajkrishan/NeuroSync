using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NeuroSync.Api.Services;
using NeuroSync.Core;
using Xunit;

namespace NeuroSync.Api.Tests;

public class EphemeralSessionContextTests
{
    private readonly EphemeralSessionContextService _sessions = new();

    [Fact]
    public void AppendTurn_DifferentSessionIds_DoNotShareTurns()
    {
        _sessions.AppendTurn("user_a", "sess_one", "nalaiku interview", "Got it — interview tomorrow.");
        _sessions.AppendTurn("user_a", "sess_two", "bayama irukku", "Tell me more.");

        var one = _sessions.GetRecentTurns("user_a", "sess_one");
        var two = _sessions.GetRecentTurns("user_a", "sess_two");

        one.Should().Contain(t => t.Text.Contains("interview"));
        one.Should().NotContain(t => t.Text.Contains("bayama"));
        two.Should().Contain(t => t.Text.Contains("bayama"));
        two.Should().NotContain(t => t.Text.Contains("interview"));
    }

    [Fact]
    public void GetRecentTurns_UnknownSession_ReturnsEmpty()
    {
        _sessions.GetRecentTurns("user_a", "missing_session").Should().BeEmpty();
    }

    [Fact]
    public void Clear_RemovesSessionTurns()
    {
        _sessions.AppendTurn("user_a", "sess_clear", "hello", "hi");
        _sessions.Clear("user_a", "sess_clear");
        _sessions.GetRecentTurns("user_a", "sess_clear").Should().BeEmpty();
    }

    [Fact]
    public void DecisionEngine_WithMemoryOff_StillLoadsEphemeralRecentTurns()
    {
        var temp = Path.Combine(Path.GetTempPath(), "ns-mem-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            env.Setup(e => e.ContentRootPath).Returns(temp);
            var consent = new EthicalAIFrameworkService(
                Mock.Of<ILogger<EthicalAIFrameworkService>>(), env.Object);
            consent.HasConsent("mem_off", ConsentType.Memory).Should().BeFalse();

            var memory = CompanionTestFactory.CreateMemory();
            var ephemeral = new EphemeralSessionContextService();
            const string sessionId = "testsession01";
            ephemeral.AppendTurn("mem_off", sessionId, "nalaiku interview", "Good luck with the interview.");

            var recording = new RecordingCompanionResponseService(new ResponsePolicyService(), new TemplateCompanionProvider());
            var engine = new DecisionEngine(
                new NeuroSync.IoT.IoTDeviceSimulator(),
                null,
                Mock.Of<ILogger<DecisionEngine>>(),
                new SafetyGateService(Mock.Of<ILogger<SafetyGateService>>()),
                new IntentRouterService(),
                new CompanionModeService(),
                new ResponsePolicyService(),
                recording,
                memory,
                null,
                null,
                consent,
                null,
                null,
                ephemeral);

            var beforeCount = memory.GetOrCreateContext("mem_off").History.Count;
            var emotion = new EmotionResult(EmotionType.Anxious, 0.7f, "bayama irukku");
            engine.GenerateResponse(emotion, "mem_off", "bayama irukku", sessionId);

            recording.LastContext.Should().NotBeNull();
            recording.LastContext!.RecentTurns.Should().Contain(t =>
                t.Role == "user" && t.Text.Contains("interview", StringComparison.OrdinalIgnoreCase));

            memory.GetOrCreateContext("mem_off").History.Count.Should().Be(beforeCount);
            engine.GenerateResponse(emotion, "mem_off", "second", sessionId);
            memory.GetOrCreateContext("mem_off").History.Count.Should().Be(beforeCount);
        }
        finally
        {
            try { Directory.Delete(temp, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task CompanionEndpoint_TwoMessagesSameSession_ReturnsSessionId()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:RequireApiKey"] = "false",
                    ["Cors:AllowedOrigins:0"] = "http://localhost"
                });
            });
        });

        var client = factory.CreateClient();
        var first = await client.PostAsJsonAsync("/api/companion/message",
            new { text = "nalaiku interview", userId = "sess_flow" });
        first.EnsureSuccessStatusCode();
        using var doc1 = System.Text.Json.JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        var sessionId = doc1.RootElement.GetProperty("sessionId").GetString();
        sessionId.Should().NotBeNullOrWhiteSpace();

        var second = await client.PostAsJsonAsync("/api/companion/message",
            new { text = "bayama irukku", userId = "sess_flow", sessionId });
        second.EnsureSuccessStatusCode();
        using var doc2 = System.Text.Json.JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        doc2.RootElement.GetProperty("sessionId").GetString().Should().Be(sessionId);
    }

    private sealed class RecordingCompanionResponseService : ICompanionResponseService
    {
        private readonly ResponsePolicyService _policy;
        private readonly ICompanionProvider _inner;

        public CompanionContext? LastContext { get; private set; }

        public RecordingCompanionResponseService(ResponsePolicyService policy, ICompanionProvider inner)
        {
            _policy = policy;
            _inner = inner;
        }

        public async Task<CompanionReply> GenerateAsync(
            CompanionContext context,
            ResponsePolicyService.PolicyResult policy,
            CancellationToken cancellationToken = default)
        {
            LastContext = context;
            var reply = await _inner.GenerateAsync(context, cancellationToken).ConfigureAwait(false);
            reply.Message = _policy.Sanitize(reply.Message, policy);
            return reply;
        }
    }
}
