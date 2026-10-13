using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeuroSync.Api.Services;
using Xunit;

namespace NeuroSync.Api.Tests;

public class V1IndexHtmlSessionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public V1IndexHtmlSessionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
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
    }

    [Fact]
    public void IndexHtml_StoresCompanionSessionInSessionStorage_NotLocalStorage()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "NeuroSync.Api", "wwwroot", "index.html");
        path = Path.GetFullPath(path);
        File.Exists(path).Should().BeTrue($"expected V1 UI at {path}");

        var html = File.ReadAllText(path);
        html.Should().Contain("sessionStorage");
        html.Should().Contain("ns_companion_session");
        html.Should().Contain("payload.sessionId");
        html.Should().Contain("getCompanionSessionId");
        html.Should().Contain("saveCompanionSessionId");

        var localStorageSessionPattern = new[] { "localStorage.setItem('ns_companion_session'", "localStorage.getItem('ns_companion_session'" };
        foreach (var forbidden in localStorageSessionPattern)
            html.Should().NotContain(forbidden, "companion session id must not use localStorage");
    }

    [Fact]
    public async Task CompanionEndpoint_SameSessionId_SharesEphemeralTurnsWithMemoryOff()
    {
        const string userId = "v1_sess_ctx";
        var client = _factory.CreateClient();
        var ephemeral = _factory.Services.GetRequiredService<EphemeralSessionContextService>();

        var first = await client.PostAsJsonAsync("/api/companion/message",
            new { text = "nalaiku interview", userId });
        first.EnsureSuccessStatusCode();
        using var doc1 = System.Text.Json.JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        var sessionId = doc1.RootElement.GetProperty("sessionId").GetString();
        sessionId.Should().NotBeNullOrWhiteSpace();

        var afterFirst = ephemeral.GetRecentTurns(userId, sessionId!);
        afterFirst.Should().Contain(t => t.Role == "user" && t.Text.Contains("interview", StringComparison.OrdinalIgnoreCase));

        var second = await client.PostAsJsonAsync("/api/companion/message",
            new { text = "bayama irukku", userId, sessionId });
        second.EnsureSuccessStatusCode();

        var afterSecond = ephemeral.GetRecentTurns(userId, sessionId!);
        afterSecond.Should().Contain(t => t.Role == "user" && t.Text.Contains("interview", StringComparison.OrdinalIgnoreCase));
        afterSecond.Should().Contain(t => t.Role == "user" && t.Text.Contains("bayama", StringComparison.OrdinalIgnoreCase));
        afterSecond.Count(t => t.Role == "user").Should().BeGreaterOrEqualTo(2);
    }
}
