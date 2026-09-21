using FluentAssertions;
using NeuroSync.Api.Hubs;
using NeuroSync.Core;
using Xunit;

namespace NeuroSync.Api.Tests;

/// <summary>
/// Proves SignalR emotional payloads are scoped per user group — never Clients.All.
/// </summary>
public class SignalRUserScopeTests
{
    [Fact]
    public void GroupName_IsPerUser_NotGlobalBroadcast()
    {
        EmotionHubUserScope.GroupName("alice").Should().Be("user:alice");
        EmotionHubUserScope.GroupName("bob").Should().Be("user:bob");
        EmotionHubUserScope.GroupName("alice").Should().NotBe(EmotionHubUserScope.GroupName("bob"));
        // Helper documents the isolation contract used by EmotionController / CompanionController
        EmotionHubUserScope.GroupName(null!).Should().Be("user:default");
    }

    [Theory]
    [InlineData("alice", true)]
    [InlineData("u_abc123", true)]
    [InlineData("user_1", true)]
    [InlineData("../etc/passwd", false)]
    [InlineData("a/b", false)]
    [InlineData("evil..path", false)]
    [InlineData("", true)] // empty → default
    public void UserIdSanitizer_RejectsUnsafePathChars(string? id, bool expectOk)
    {
        UserIdSanitizer.TryNormalize(id, out _).Should().Be(expectOk);
    }
}
