using Microsoft.AspNetCore.Identity;

namespace NeuroSync.Api.Auth;

/// <summary>
/// Signed-in account. The id is the only identity the API trusts.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProductNoticeAcceptedAtUtc { get; set; }
    public DateTime? RetentionAcknowledgedAtUtc { get; set; }
    public string ConsentVersion { get; set; } = ConsentVersions.Current;
}

public static class ConsentVersions
{
    public const string Current = "2026-10";
}
