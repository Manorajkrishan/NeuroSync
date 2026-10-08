using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NeuroSync.Api.Services;
using NeuroSync.Core;
using Xunit;

namespace NeuroSync.Api.Tests;

public class LlmCompanionProviderTests
{
    [Fact]
    public async Task GenerateAsync_WhenDisabled_FallsBackToTemplate()
    {
        var template = new TemplateCompanionProvider();
        var provider = CreateProvider(template, enabled: false);

        var reply = await provider.GenerateAsync(SampleContext(UserIntent.Greeting, "hi"));

        reply.ProviderId.Should().Be(template.ProviderId);
        reply.Message.Should().NotBeNullOrWhiteSpace();
        reply.ExposedEmotionToUser.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAsync_WhenMissingApiKey_FallsBackToTemplate()
    {
        var template = new TemplateCompanionProvider();
        var provider = CreateProvider(template, enabled: true, apiKey: "");

        var reply = await provider.GenerateAsync(SampleContext(UserIntent.Greeting, "hi"));

        reply.ProviderId.Should().Be(template.ProviderId);
    }

    [Fact]
    public async Task GenerateAsync_WhenHttpFails_FallsBackToTemplate()
    {
        var template = new TemplateCompanionProvider();
        var provider = CreateProvider(
            template,
            enabled: true,
            apiKey: "sk-test",
            handler: new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var reply = await provider.GenerateAsync(SampleContext(UserIntent.CasualConversation, "how are you"));

        reply.ProviderId.Should().Be(template.ProviderId);
        reply.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GenerateAsync_WhenLlmSucceeds_ReturnsLlmContent()
    {
        var template = new TemplateCompanionProvider();
        var json = """{"choices":[{"message":{"content":"Hello from the model."}}]}""";
        var provider = CreateProvider(
            template,
            enabled: true,
            apiKey: "sk-test",
            handler: new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }));

        var reply = await provider.GenerateAsync(SampleContext(UserIntent.Greeting, "hi"));

        reply.ProviderId.Should().Be("llm-v1");
        reply.Message.Should().Be("Hello from the model.");
    }

    [Fact]
    public async Task GenerateAsync_WhenSafetyBlocked_SkipsLlmAndReturnsGuidance()
    {
        var called = false;
        var template = new TemplateCompanionProvider();
        var provider = CreateProvider(
            template,
            enabled: true,
            apiKey: "sk-test",
            handler: new StubHandler(_ =>
            {
                called = true;
                return new HttpResponseMessage(HttpStatusCode.OK);
            }));

        var ctx = SampleContext(UserIntent.SafetySensitive, "help");
        ctx.Safety = new SafetyAssessment
        {
            Level = SafetyLevel.ImmediateDanger,
            BlockNormalCompanionFlow = true,
            Guidance = "Please reach out to emergency support."
        };

        var reply = await provider.GenerateAsync(ctx);

        called.Should().BeFalse();
        reply.ProviderId.Should().Be("safety-protocol");
        reply.Message.Should().Contain("emergency");
    }

    [Fact]
    public async Task CompanionResponseService_UsesProviderAsync()
    {
        var policy = new ResponsePolicyService();
        var template = new TemplateCompanionProvider();
        var service = new CompanionResponseService(policy, template);
        var ctx = SampleContext(UserIntent.Greeting, "hi");
        var p = policy.Evaluate(ctx);

        var reply = await service.GenerateAsync(ctx, p);

        reply.Message.Should().NotBeNullOrWhiteSpace();
        reply.ProviderId.Should().Be(template.ProviderId);
    }

    private static CompanionContext SampleContext(UserIntent intent, string message) =>
        new()
        {
            CurrentMessage = message,
            Intent = intent,
            Mode = CompanionInteractionMode.Talk,
            Emotion = new EmotionResult(EmotionType.Neutral, 0.5f, message),
            Safety = new SafetyAssessment(),
            Uncertainty = UncertaintyLevel.InsufficientEvidence,
            EmotionSignals = new Dictionary<string, float> { ["Neutral"] = 0.5f }
        };

    private static LlmCompanionProvider CreateProvider(
        TemplateCompanionProvider template,
        bool enabled,
        string apiKey = "sk-test",
        HttpMessageHandler? handler = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Companion:Llm:Enabled"] = enabled ? "true" : "false",
                ["Companion:Llm:ApiKey"] = apiKey,
                ["Companion:Llm:BaseUrl"] = "https://example.test/v1",
                ["Companion:Llm:Model"] = "gpt-test",
                ["Companion:Llm:TimeoutSeconds"] = "5"
            })
            .Build();

        handler ??= new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"choices":[{"message":{"content":"ok"}}]}""", Encoding.UTF8, "application/json")
        });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler));

        return new LlmCompanionProvider(
            template,
            factory.Object,
            config,
            Mock.Of<ILogger<LlmCompanionProvider>>());
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
            _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(_responder(request));
    }
}
