namespace NeuroSync.Api.Auth;

/// <summary>Stored refresh-token hash. The raw token is only returned once, in an HttpOnly cookie.</summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByHash { get; set; }

    public bool IsActive => RevokedAtUtc == null && ExpiresAtUtc > DateTime.UtcNow;
}
