using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NeuroSync.Api.Data;

namespace NeuroSync.Api.Auth;

public sealed class JwtTokenService
{
    private readonly NeuroSyncDbContext _db;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;

    public JwtTokenService(NeuroSyncDbContext db, IConfiguration config, IHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    public int AccessTokenMinutes => Math.Clamp(_config.GetValue("Auth:Jwt:AccessTokenMinutes", 15), 5, 60);
    public int RefreshTokenDays => Math.Clamp(_config.GetValue("Auth:Jwt:RefreshTokenDays", 14), 1, 60);

    public async Task<IssuedTokens> IssueAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var refresh = NewRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = AuthPolicy.Sha256(refresh),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshTokenDays),
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new IssuedTokens(CreateAccessToken(user), refresh, AccessTokenMinutes * 60);
    }

    public async Task<RotatedTokens?> RotateAsync(string rawRefresh, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefresh))
            return null;

        var hash = AuthPolicy.Sha256(rawRefresh.Trim());
        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            .ConfigureAwait(false);
        if (existing == null)
            return null;

        if (existing.RevokedAtUtc != null || existing.ExpiresAtUtc <= DateTime.UtcNow)
        {
            // Reuse of a revoked token invalidates the rest of that account's sessions.
            if (existing.RevokedAtUtc != null)
                await RevokeAllAsync(existing.UserId, cancellationToken).ConfigureAwait(false);
            return null;
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == existing.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (user == null)
            return null;

        var replacement = NewRefreshToken();
        var replacementHash = AuthPolicy.Sha256(replacement);
        existing.RevokedAtUtc = DateTime.UtcNow;
        existing.ReplacedByHash = replacementHash;
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = replacementHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshTokenDays),
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new RotatedTokens(user, CreateAccessToken(user), replacement, AccessTokenMinutes * 60);
    }

    public async Task RevokeAsync(string? rawRefresh, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefresh))
            return;
        var hash = AuthPolicy.Sha256(rawRefresh.Trim());
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            .ConfigureAwait(false);
        if (existing == null || existing.RevokedAtUtc != null)
            return;
        existing.RevokedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RevokeAllAsync(string userId, CancellationToken cancellationToken = default)
    {
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var now = DateTime.UtcNow;
        foreach (var token in active)
            token.RevokedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private string CreateAccessToken(ApplicationUser user)
    {
        var key = AuthPolicy.SigningKey(_config, _env);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.Name, user.DisplayName ?? user.Email ?? user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var token = new JwtSecurityToken(
            issuer: _config["Auth:Jwt:Issuer"] ?? "NeuroSync",
            audience: _config["Auth:Jwt:Audience"] ?? "NeuroSync",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string NewRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

public readonly record struct IssuedTokens(string AccessToken, string RefreshToken, int ExpiresInSeconds);
public readonly record struct RotatedTokens(ApplicationUser User, string AccessToken, string RefreshToken, int ExpiresInSeconds);
