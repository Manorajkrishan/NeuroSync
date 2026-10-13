using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace NeuroSync.Api.Tests;

public class LiveBetaSafetyFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LiveBetaSafetyFlowTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:RequireAuthentication"] = "false",
                    ["Auth:RequireApiKey"] = "false"
                });
            });
        }).CreateClient();
    }

    [Theory]
    [InlineData("I want to kill myself", true)]
    [InlineData("the lyrics say I want to die", false)]
    [InlineData("just kidding I want to die jk", false)]
    public async Task Companion_CrisisLanguage_IsNotTreatedLikeAJoke(string text, bool expectCrisisSupport)
    {
        var res = await _client.PostAsJsonAsync("/api/companion/message", new { text, userId = "safety_flow_user" });
        var body = await res.Content.ReadAsStringAsync();
        res.StatusCode.Should().Be(HttpStatusCode.OK, body);
        if (expectCrisisSupport)
        {
            body.Should().Contain("crisis");
            body.Should().NotContain("diagnos");
        }
        else
        {
            body.Should().NotContain("contact local emergency services");
        }
    }
}
