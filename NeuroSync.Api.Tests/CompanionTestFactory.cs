using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NeuroSync.Api.Services;
using NeuroSync.IoT;

namespace NeuroSync.Api.Tests;

public static class CompanionTestFactory
{
    public static DecisionEngine CreateEngine(ConversationMemory? memory = null)
    {
        memory ??= CreateMemory();
        var ei = new EmotionalIntelligence(Mock.Of<ILogger<EmotionalIntelligence>>());
        var policy = new ResponsePolicyService();
        return new DecisionEngine(
            new IoTDeviceSimulator(),
            null,
            Mock.Of<ILogger<DecisionEngine>>(),
            new SafetyGateService(Mock.Of<ILogger<SafetyGateService>>()),
            new IntentRouterService(),
            new CompanionModeService(),
            policy,
            new CompanionResponseService(policy),
            memory,
            null,
            new EmotionalBaselineService(memory, Mock.Of<ILogger<EmotionalBaselineService>>()),
            null,
            ei,
            new TemplateCompanionProvider());
    }

    public static ConversationMemory CreateMemory()
    {
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Throws(new InvalidOperationException("no db in unit test"));
        return new ConversationMemory(Mock.Of<ILogger<ConversationMemory>>(), scopeFactory.Object);
    }
}
