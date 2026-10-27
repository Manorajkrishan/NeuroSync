using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeuroSync.Api.Data;
using NeuroSync.Api.Services;
using NeuroSync.Core;
using Xunit;

namespace NeuroSync.Api.Tests;

public class P4CompanionQualityTests
{
    [Fact]
    public void ResponseQuality_RoboticUncertainty_IsPenalized()
    {
        var service = new ResponseQualityService();
        var context = new CompanionContext
        {
            CurrentMessage = "i am tired",
            Intent = UserIntent.EmotionalDisclosure,
            Mode = CompanionInteractionMode.Listen,
            Locale = "en",
            ThreadState = new CompanionThreadState { Topic = "fatigue" }
        };

        var result = service.Evaluate(context, new CompanionReply
        {
            Message = "I'm not sure I have enough to go on yet. Want to clarify how you're feeling?",
            ProviderId = "llm-v1"
        });

        result.Score.Should().BeLessThan(0.65);
        result.Flags.Should().Contain("robotic_uncertainty");
    }

    [Fact]
    public void ResponseQuality_NaturalThreadContinuation_ScoresHigh()
    {
        var service = new ResponseQualityService();
        var context = new CompanionContext
        {
            CurrentMessage = "work",
            Intent = UserIntent.EmotionalDisclosure,
            Mode = CompanionInteractionMode.Listen,
            Locale = "en",
            ThreadState = new CompanionThreadState { Topic = "fatigue", PendingFollowUp = "fatigue_cause" }
        };

        var result = service.Evaluate(context, new CompanionReply
        {
            Message = "Work sounds like the drain. Is it the workload, the people, or just a nonstop day?",
            ProviderId = "template-v2"
        });

        result.Score.Should().BeGreaterThanOrEqualTo(0.8);
        result.Flags.Should().NotContain("thread_continuity_miss");
    }

    [Fact]
    public void ThreadState_TracksTopicAndPendingFollowUp()
    {
        var service = new ConversationThreadStateService();

        var state = service.Advance(
            new CompanionThreadState(),
            "i am so tired today",
            UserIntent.EmotionalDisclosure,
            CompanionInteractionMode.Listen,
            "What drained you most?");

        state.Topic.Should().Be("fatigue");
        state.PendingFollowUp.Should().Be("fatigue_cause");
        state.TurnCount.Should().Be(1);

        var next = service.Advance(
            state,
            "work",
            UserIntent.EmotionalDisclosure,
            CompanionInteractionMode.Listen,
            "Is it workload or people?");

        next.Topic.Should().Be("work");
        next.PendingFollowUp.Should().Be("work_source");
        next.TurnCount.Should().Be(2);
    }

    [Fact]
    public void EphemeralSession_ResponseMetadata_IsSessionScoped()
    {
        var sessions = new EphemeralSessionContextService();
        var metadata = new CompanionResponseMetadata
        {
            ResponseId = Guid.NewGuid().ToString("N"),
            ProviderId = "template-v2",
            Intent = UserIntent.EmotionalDisclosure,
            Mode = CompanionInteractionMode.Listen,
            Locale = "en",
            QualityScore = 0.93
        };

        sessions.RegisterResponse("p4-user", "session-a", metadata);
        sessions.UpdateThreadState("p4-user", "session-a", new CompanionThreadState
        {
            Topic = "stress",
            PendingFollowUp = "stress_source",
            TurnCount = 3
        });

        sessions.GetResponseMetadata("p4-user", "session-a", metadata.ResponseId)
            .Should().NotBeNull();
        sessions.GetResponseMetadata("p4-user", "session-b", metadata.ResponseId)
            .Should().BeNull();
        sessions.GetThreadState("p4-user", "session-a").Topic.Should().Be("stress");
        sessions.GetThreadState("p4-user", "session-b").Topic.Should().BeNull();
    }
}

public class P4FeedbackEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public P4FeedbackEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task ExplicitFeedback_UsesTrustedResponseMetadata_AndRespectsLearningConsent()
    {
        var client = _factory.CreateClient();
        var session = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        const string chatSession = "p4_feedback_session";
        var message = await client.PostAsJsonAsync("/api/companion/message", new
        {
            text = "i am so tired today",
            userId = session.UserId,
            sessionId = chatSession
        });
        var messageBody = await message.Content.ReadAsStringAsync();
        message.StatusCode.Should().Be(HttpStatusCode.OK, messageBody);

        using var doc = JsonDocument.Parse(messageBody);
        var responseId = doc.RootElement.GetProperty("responseId").GetString();
        responseId.Should().NotBeNullOrWhiteSpace();
        responseId.Should().HaveLength(32);

        var feedback = await client.PostAsJsonAsync("/api/companion/feedback", new
        {
            userId = session.UserId,
            sessionId = chatSession,
            responseId,
            rating = 1,
            reasonCode = "helpful",
            providerId = "forged-client-provider",
            qualityScore = 0
        });
        var feedbackBody = await feedback.Content.ReadAsStringAsync();
        feedback.StatusCode.Should().Be(HttpStatusCode.OK, feedbackBody);
        feedbackBody.Should().Contain(""shareForLearning":false");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NeuroSyncDbContext>();
            var row = await db.CompanionFeedback.SingleAsync(x =>
                x.UserId == session.UserId && x.ResponseId == responseId);

            row.Rating.Should().Be(1);
            row.ProviderId.Should().NotBe("forged-client-provider");
            row.QualityScore.Should().BeGreaterThan(0);
            row.ShareForLearning.Should().BeFalse();
        }

        var consent = await client.PutAsJsonAsync($"/api/privacy/consent/{session.UserId}", new
        {
            userId = session.UserId,
            memoryConsent = false,
            emotionHistoryConsent = false,
            dataSharingConsent = true
        });
        consent.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await client.PostAsJsonAsync("/api/companion/feedback", new
        {
            userId = session.UserId,
            sessionId = chatSession,
            responseId,
            rating = -1,
            reasonCode = "wrong_context"
        });
        var updatedBody = await updated.Content.ReadAsStringAsync();
        updated.StatusCode.Should().Be(HttpStatusCode.OK, updatedBody);
        updatedBody.Should().Contain(""shareForLearning":true");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NeuroSyncDbContext>();
        var saved = await verifyDb.CompanionFeedback.SingleAsync(x =>
            x.UserId == session.UserId && x.ResponseId == responseId);
        saved.Rating.Should().Be(-1);
        saved.ReasonCode.Should().Be("wrong_context");
        saved.ShareForLearning.Should().BeTrue();
    }

    [Fact]
    public async Task Feedback_ForUnknownResponse_IsRejected()
    {
        var client = _factory.CreateClient();
        var session = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var response = await client.PostAsJsonAsync("/api/companion/feedback", new
        {
            userId = session.UserId,
            sessionId = "p4_missing_session",
            responseId = Guid.NewGuid().ToString("N"),
            rating = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<(string Token, string UserId)> RegisterAsync(HttpClient client)
    {
        var email = $"p4.{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Live-Beta-1a",
            displayName = "P4 Tester",
            acceptWellbeingNotice = true,
            acknowledgeRetention = true
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        using var doc = JsonDocument.Parse(body);
        return (
            doc.RootElement.GetProperty("accessToken").GetString()!,
            doc.RootElement.GetProperty("user").GetProperty("id").GetString()!);
    }
}
