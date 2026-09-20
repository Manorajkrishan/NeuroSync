using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace NeuroSync.Api.Tests;

public class AuthAndPrivacyBlockerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthAndPrivacyBlockerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:RequireApiKey"] = "true",
                    ["Auth:ApiKey"] = "test-secret-key-12345",
                    ["Cors:AllowedOrigins:0"] = "http://localhost"
                });
            });
        });
    }

    [Fact]
    public async Task Health_ShouldRemainAnonymous()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/health");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Detect_WithoutApiKey_ShouldUnauthorized()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/emotion/detect", new { text = "hi", userId = "t1" });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Detect_WithValidApiKey_ShouldSucceed()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-secret-key-12345");
        var res = await client.PostAsJsonAsync("/api/emotion/detect", new { text = "I feel sad", userId = "t1" });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await res.Content.ReadAsStringAsync();
        json.Should().Contain("adaptiveResponse");
        json.Should().Contain("decisionTrace");
    }

    [Fact]
    public async Task Facial_WithoutConsent_ShouldForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-secret-key-12345");
        var res = await client.PostAsJsonAsync("/api/emotion/facial", new
        {
            emotion = "Happy",
            confidence = 0.9,
            userId = "no-face-consent-user"
        });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
