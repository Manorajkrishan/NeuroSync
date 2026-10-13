using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NeuroSync.Api.Auth;
using NeuroSync.Api.Services;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private static readonly Regex DisplayNameRx = new(@"^[\p{L}\p{M}0-9 .'-]{1,40}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly UserManager<ApplicationUser> _users;
    private readonly JwtTokenService _tokens;
    private readonly IAccountDataService _accounts;
    private readonly IAuditLog _audit;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly LiveBetaMetrics _metrics;

    public AuthController(
        UserManager<ApplicationUser> users,
        JwtTokenService tokens,
        IAccountDataService accounts,
        IAuditLog audit,
        IConfiguration config,
        IHostEnvironment env,
        LiveBetaMetrics metrics)
    {
        _users = users;
        _tokens = tokens;
        _accounts = accounts;
        _audit = audit;
        _config = config;
        _env = env;
        _metrics = metrics;
    }

    [AllowAnonymous]
    [HttpGet("config")]
    public IActionResult Config()
    {
        return Ok(new
        {
            requireAuthentication = AuthPolicy.RequireAuthentication(_config, _env),
            requireApiKey = _config.GetValue("Auth:RequireApiKey", !_env.IsDevelopment()),
            inviteOnly = _config.GetValue("Beta:InviteOnly", false),
            memoryDefault = false,
            learningDataSharingDefault = false,
            sessionTtlMinutes = 45,
            retentionDays = Math.Clamp(_config.GetValue("Database:RetentionDays", 365), 7, 3650),
            productNotice = "NeuroSync is a wellbeing companion. It is not therapy, counselling, or a diagnosis."
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { error = "Registration details are required." });
        if (!request.AcceptWellbeingNotice || !request.AcknowledgeRetention)
            return BadRequest(new { error = "The wellbeing notice and data-retention acknowledgement are required." });
        var email = request.Email?.Trim() ?? "";
        var password = request.Password ?? "";
        if (!IsEmail(email))
            return BadRequest(new { error = "Enter a valid email address." });
        if (string.IsNullOrWhiteSpace(password) || password.Length > 128)
            return BadRequest(new { error = "Password is required." });
        var displayName = (request.DisplayName ?? "").Trim();
        if (!DisplayNameRx.IsMatch(displayName))
            return BadRequest(new { error = "Display name must be 1-40 letters, numbers, spaces, or . ' -" });
        if (!InviteAccepted(request.InviteCode))
        {
            await _audit.WriteAsync("auth.register.denied", null, StatusCodes.Status403Forbidden, cancellationToken);
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "A valid invite code is required for the beta." });
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            CreatedAtUtc = DateTime.UtcNow,
            ProductNoticeAcceptedAtUtc = DateTime.UtcNow,
            RetentionAcknowledgedAtUtc = DateTime.UtcNow,
            ConsentVersion = ConsentVersions.Current,
            LockoutEnabled = true
        };
        var created = await _users.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            var duplicate = created.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName");
            await _audit.WriteAsync("auth.register.rejected", null, duplicate ? 409 : 400, cancellationToken);
            if (duplicate)
                return Conflict(new { error = "An account with that email already exists." });
            return BadRequest(new { error = "Could not create the account.", details = created.Errors.Select(e => e.Description) });
        }

        await _accounts.EnsureDefaultsAsync(user, cancellationToken);
        var issued = await _tokens.IssueAsync(user, cancellationToken);
        SetRefreshCookie(issued.RefreshToken);
        await _audit.WriteAsync("auth.register", user.Id, StatusCodes.Status200OK, cancellationToken);
        return Ok(TokenResponse(user, issued.AccessToken, issued.ExpiresInSeconds));
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request?.Email?.Trim() ?? "";
        var password = request?.Password ?? "";
        if (!IsEmail(email) || string.IsNullOrEmpty(password))
        {
            _metrics.AuthFailure();
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var user = await _users.FindByEmailAsync(email);
        if (user == null)
        {
            _metrics.AuthFailure();
            await _audit.WriteAsync("auth.login.failed", null, StatusCodes.Status401Unauthorized, cancellationToken);
            return Unauthorized(new { error = "Invalid email or password." });
        }

        if (await _users.IsLockedOutAsync(user))
        {
            _metrics.AuthFailure();
            await _audit.WriteAsync("auth.login.locked", user.Id, StatusCodes.Status429TooManyRequests, cancellationToken);
            return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "This account is temporarily locked. Try again later." });
        }

        if (!await _users.CheckPasswordAsync(user, password))
        {
            await _users.AccessFailedAsync(user);
            _metrics.AuthFailure();
            await _audit.WriteAsync("auth.login.failed", user.Id, StatusCodes.Status401Unauthorized, cancellationToken);
            return Unauthorized(new { error = "Invalid email or password." });
        }

        await _users.ResetAccessFailedCountAsync(user);
        var issued = await _tokens.IssueAsync(user, cancellationToken);
        SetRefreshCookie(issued.RefreshToken);
        await _audit.WriteAsync("auth.login", user.Id, StatusCodes.Status200OK, cancellationToken);
        return Ok(TokenResponse(user, issued.AccessToken, issued.ExpiresInSeconds));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var raw = Request.Cookies[AuthPolicy.RefreshCookieName];
        var rotated = await _tokens.RotateAsync(raw ?? "", cancellationToken);
        if (rotated == null)
        {
            _metrics.AuthFailure();
            ClearRefreshCookie();
            return Unauthorized(new { error = "Sign in again." });
        }

        SetRefreshCookie(rotated.Value.RefreshToken);
        return Ok(TokenResponse(rotated.Value.User, rotated.Value.AccessToken, rotated.Value.ExpiresInSeconds));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var raw = Request.Cookies[AuthPolicy.RefreshCookieName];
        await _tokens.RevokeAsync(raw, cancellationToken);
        ClearRefreshCookie();
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _audit.WriteAsync("auth.logout", userId, StatusCodes.Status200OK, cancellationToken);
        return Ok(new { signedOut = true });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Unauthorized(new { error = "Sign in required." });
        var consent = await _accounts.GetConsentAsync(user.Id, cancellationToken);
        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            displayName = user.DisplayName,
            consentVersion = user.ConsentVersion,
            memoryEnabled = consent?.MemoryConsent ?? false,
            shareLearningData = consent?.DataSharingConsent ?? false,
            retentionDays = Math.Clamp(_config.GetValue("Database:RetentionDays", 365), 7, 3650)
        });
    }

    private bool InviteAccepted(string? inviteCode)
    {
        if (!_config.GetValue("Beta:InviteOnly", false))
            return true;
        var codes = _config.GetSection("Beta:InviteCodes").Get<string[]>() ?? Array.Empty<string>();
        if (string.IsNullOrWhiteSpace(inviteCode))
            return false;
        foreach (var code in codes)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Contains("replace-with", StringComparison.OrdinalIgnoreCase))
                continue;
            if (AuthPolicy.FixedTimeEquals(inviteCode.Trim(), code.Trim()))
                return true;
        }
        return false;
    }

    private void SetRefreshCookie(string refreshToken)
    {
        Response.Cookies.Append(AuthPolicy.RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = HttpContext.Request.IsHttps || _env.IsProduction(),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(_tokens.RefreshTokenDays),
            IsEssential = true
        });
    }

    private void ClearRefreshCookie()
    {
        Response.Cookies.Delete(AuthPolicy.RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = HttpContext.Request.IsHttps || _env.IsProduction(),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth"
        });
    }

    private static object TokenResponse(ApplicationUser user, string accessToken, int expiresIn) => new
    {
        accessToken,
        expiresIn,
        tokenType = "Bearer",
        user = new { id = user.Id, email = user.Email, displayName = user.DisplayName }
    };

    private static bool IsEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254)
            return false;
        try
        {
            var parsed = new MailAddress(email.Trim());
            return parsed.Address.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed class RegisterRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? DisplayName { get; set; }
    public string? InviteCode { get; set; }
    public bool AcceptWellbeingNotice { get; set; }
    public bool AcknowledgeRetention { get; set; }
}

public sealed class LoginRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}
