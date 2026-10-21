using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NeuroSync.Api.Services;
using Xunit;

namespace NeuroSync.Api.Tests;

public class EthicalAIFrameworkServiceTests
{
    [Fact]
    public void ProductionConsent_DoesNotCreateLegacyFilesystemCopy()
    {
        var root = Path.Combine(Path.GetTempPath(), "neurosync-consent-" + Guid.NewGuid().ToString("N"));
        try
        {
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);
            environment.SetupGet(e => e.ContentRootPath).Returns(root);

            var service = new EthicalAIFrameworkService(
                Mock.Of<ILogger<EthicalAIFrameworkService>>(),
                environment.Object);

            var consent = service.GetOrCreateDefault("production-user");
            consent.MemoryConsent = true;
            service.SetConsent("production-user", consent);

            Directory.Exists(Path.Combine(root, "UserConsents")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
