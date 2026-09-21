using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace NeuroSync.Api.Tests;

public class CompanionEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CompanionEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task CompanionMessage_Hi_ReturnsNaturalMessageWithoutEmotionClaim()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/companion/message", new { text = "hi", userId = "comp_hi" });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.TryGetProperty("message", out var msg).Should().BeTrue();
        msg.GetString().Should().NotBeNullOrWhiteSpace();
        msg.GetString()!.ToLowerInvariant().Should().NotContain("sense you're feeling");
        msg.GetString().Should().NotMatchRegex(@"\d{1,3}%");
        root.TryGetProperty("developerInsights", out _).Should().BeTrue();
        // Emotion is not the product — no top-level emotion dump required
        root.TryGetProperty("mode", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CompanionMessage_IMissHer_IsSupportive()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/companion/message", new { text = "i miss her", userId = "comp_miss" });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var message = doc.RootElement.GetProperty("message").GetString()!.ToLowerInvariant();
        message.Should().NotContain("i sense you're feeling");
        (message.Contains("miss") || message.Contains("listen") || message.Contains("company")
         || message.Contains("hard") || message.Contains("here") || message.Contains("hit"))
            .Should().BeTrue($"unexpected: {message}");
    }

    [Fact]
    public async Task CompanionMessage_Lights_IoTOff_DoesNotExecute()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/companion/message",
            new { text = "turn on the lights", userId = "comp_iot_off" });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("iotExecuted").GetBoolean().Should().BeFalse();
        root.GetProperty("iotBlocked").GetBoolean().Should().BeTrue();
        var message = root.GetProperty("message").GetString()!.ToLowerInvariant();
        (message.Contains("enable") || message.Contains("privacy") || message.Contains("consent") || message.Contains("want"))
            .Should().BeTrue($"expected ask-permission message, got: {message}");
    }

    [Fact]
    public async Task CompanionMessage_InvalidUserId_BadRequest()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/companion/message",
            new { text = "hi", userId = "../evil" });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
