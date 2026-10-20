using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeuroSync.Api.Data;
using NeuroSync.Api.Services;
using Xunit;

namespace NeuroSync.Api.Tests;

public class LiveBetaAuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LiveBetaAuthTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:RequireAuthentication"] = "true",
                    ["Auth:RequireApiKey"] = "false",
                    ["Auth:Jwt:SigningKey"] = "test-signing-key-0123456789abcdef",
                    ["Auth:Jwt:Issuer"] = "NeuroSync.Tests",
                    ["Auth:Jwt:Audience"] = "NeuroSync.Tests",
                    ["Beta:InviteOnly"] = "false",
                    ["Database:Provider"] = "InMemory",
                    ["Cors:AllowedOrigins:0"] = "http://localhost"
                });
            });
        });
    }

    [Fact]
    public async Task Health_StaysAnonymous_AndSendsSecurityHeaders()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/health");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        res.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
    }

    [Fact]
    public async Task Config_ReportsMemoryAndLearningOffByDefault()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/auth/config");
        var json = await res.Content.ReadAsStringAsync();
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        json.Should().Contain("\"memoryDefault\":false");
        json.Should().Contain("\"learningDataSharingDefault\":false");
        json.Should().Contain("not therapy");
    }

    [Fact]
    public async Task Companion_WithoutToken_IsUnauthorized()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/companion/message", new { text = "hi", userId = "someone_else" });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SignedInUser_CannotReadOrMessageAnotherAccount()
    {
        var client = _factory.CreateClient();
        var session = await RegisterAsync(client, "owner");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var own = await client.PostAsJsonAsync("/api/companion/message", new { text = "nalaiku interview", userId = session.UserId });
        var ownBody = await own.Content.ReadAsStringAsync();
        own.StatusCode.Should().Be(HttpStatusCode.OK, ownBody);

        var spoof = await client.PostAsJsonAsync("/api/companion/message", new { text = "hi", userId = "someone_else" });
        spoof.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var export = await client.GetAsync("/api/privacy/export/someone_else");
        export.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteAccount_RemovesUserConsentMemoryAndHistory()
    {
        var client = _factory.CreateClient();
        var session = await RegisterAsync(client, "delete");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var consent = await client.PutAsJsonAsync($"/api/privacy/consent/{session.UserId}", new
        {
            userId = session.UserId,
            memoryConsent = true,
            emotionHistoryConsent = true,
            dataSharingConsent = false
        });
        consent.StatusCode.Should().Be(HttpStatusCode.OK);

        var chat = await client.PostAsJsonAsync("/api/companion/message", new
        {
            text = "please remember that I like evening walks",
            userId = session.UserId
        });
        chat.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NeuroSyncDbContext>();
            (await db.ConversationHistories.CountAsync(c => c.UserId == session.UserId)).Should().BeGreaterThan(0);
        }

        var deleted = await client.DeleteAsync("/api/account");
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NeuroSyncDbContext>();
            (await db.Users.AnyAsync(u => u.Id == session.UserId)).Should().BeFalse();
            (await db.UserPrivacyProfiles.AnyAsync(p => p.UserId == session.UserId)).Should().BeFalse();
            (await db.ConversationHistories.AnyAsync(c => c.UserId == session.UserId)).Should().BeFalse();
            (await db.PersistentMemories.AnyAsync(m => m.UserId == session.UserId)).Should().BeFalse();
            (await db.RefreshTokens.AnyAsync(t => t.UserId == session.UserId)).Should().BeFalse();
        }
    }

    [Fact]
    public async Task DeleteHistory_ClearsCurrentEphemeralSession()
    {
        var client = _factory.CreateClient();
        var session = await RegisterAsync(client, "historyclear");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        const string sessionId = "history_clear_session";
        var chat = await client.PostAsJsonAsync("/api/companion/message", new
        {
            text = "nalaiku interview",
            userId = session.UserId,
            sessionId
        });
        chat.StatusCode.Should().Be(HttpStatusCode.OK);

        var sessions = _factory.Services.GetRequiredService<IEphemeralSessionContext>();
        sessions.GetRecentTurns(session.UserId, sessionId).Should().NotBeEmpty();

        var deleted = await client.DeleteAsync($"/api/privacy/memory/{session.UserId}");
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);
        sessions.GetRecentTurns(session.UserId, sessionId).Should().BeEmpty();
    }

    [Fact]
    public async Task Preferences_CanBeReadAndUpdatedForSignedInUser()
    {
        var client = _factory.CreateClient();
        var session = await RegisterAsync(client, "prefs");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var initial = await client.GetAsync("/api/account/preferences");
        initial.StatusCode.Should().Be(HttpStatusCode.OK);
        (await initial.Content.ReadAsStringAsync()).Should().Contain("Beta Tester");

        var updated = await client.PutAsJsonAsync("/api/account/preferences", new
        {
            displayName = "Krish Test",
            locale = "tanglish"
        });
        var body = await updated.Content.ReadAsStringAsync();
        updated.StatusCode.Should().Be(HttpStatusCode.OK, body);
        body.Should().Contain("Krish Test");
        body.Should().Contain("tanglish");
    }

    [Fact]
    public async Task ChangePassword_InvalidatesOldPassword_AndAcceptsNewPassword()
    {
        var client = _factory.CreateClient();
        var email = $"password.{Guid.NewGuid():N}@example.com";
        const string oldPassword = "Live-Beta-1a";
        const string newPassword = "New-Live-Beta-2b!";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = oldPassword,
            displayName = "Password Tester",
            acceptWellbeingNotice = true,
            acknowledgeRetention = true
        });
        var registerBody = await register.Content.ReadAsStringAsync();
        register.StatusCode.Should().Be(HttpStatusCode.OK, registerBody);
        using var doc = System.Text.Json.JsonDocument.Parse(registerBody);
        var token = doc.RootElement.GetProperty("accessToken").GetString();
        token.Should().NotBeNullOrWhiteSpace();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var changed = await client.PutAsJsonAsync("/api/account/password", new
        {
            currentPassword = oldPassword,
            newPassword
        });
        var changedBody = await changed.Content.ReadAsStringAsync();
        changed.StatusCode.Should().Be(HttpStatusCode.OK, changedBody);

        client.DefaultRequestHeaders.Authorization = null;
        var oldLogin = await client.PostAsJsonAsync("/api/auth/login", new { email, password = oldPassword });
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newLogin = await client.PostAsJsonAsync("/api/auth/login", new { email, password = newPassword });
        var newLoginBody = await newLogin.Content.ReadAsStringAsync();
        newLogin.StatusCode.Should().Be(HttpStatusCode.OK, newLoginBody);
    }

    private static async Task<(string Token, string UserId)> RegisterAsync(HttpClient client, string prefix)
    {
        var email = $"{prefix}.{Guid.NewGuid():N}@example.com";
        var res = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Live-Beta-1a",
            displayName = "Beta Tester",
            acceptWellbeingNotice = true,
            acknowledgeRetention = true
        });
        var body = await res.Content.ReadAsStringAsync();
        res.StatusCode.Should().Be(HttpStatusCode.OK, body);
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var token = doc.RootElement.GetProperty("accessToken").GetString();
        var userId = doc.RootElement.GetProperty("user").GetProperty("id").GetString();
        token.Should().NotBeNullOrWhiteSpace();
        userId.Should().NotBeNullOrWhiteSpace();
        return (token!, userId!);
    }
}
