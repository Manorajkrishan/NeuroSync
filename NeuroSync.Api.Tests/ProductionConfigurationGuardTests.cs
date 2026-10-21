using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using NeuroSync.Api.Hosting;
using Xunit;

namespace NeuroSync.Api.Tests;

public class ProductionConfigurationGuardTests
{
    [Fact]
    public void Validate_WhenProductionLlmEnabledWithoutRedis_FailsClosed()
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:RequireAuthentication"] = "true",
                ["Auth:Jwt:SigningKey"] = "a-unique-production-signing-key-1234567890",
                ["Cors:AllowedOrigins:0"] = "https://neurosync.example",
                ["Database:Provider"] = "Postgres",
                ["ConnectionStrings:DefaultConnection"] = "Host=db;Database=neurosync;Username=neurosync;Password=strong-password-123",
                ["Beta:InviteOnly"] = "false",
                ["Companion:Llm:Enabled"] = "true",
                ["Companion:Llm:ApiKey"] = "server-side-test-key",
                ["Redis:ConnectionString"] = ""
            })
            .Build();

        var act = () => ProductionConfigurationGuard.Validate(environment.Object, config);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*requires Redis:ConnectionString*");
    }
}
