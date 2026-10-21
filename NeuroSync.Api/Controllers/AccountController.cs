using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Auth;
using NeuroSync.Api.Services;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private static readonly Regex DisplayNameRx = new(@"^[\p{L}\p{M}0-9 .'-]{1,40}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly HashSet<string> Locales = new(StringComparer.OrdinalIgnoreCase) { "en", "ta", "tanglish" };

    private readonly UserManager<ApplicationUser> _users;
    private readonly IAccountDataService _accounts;
    private readonly IEphemeralSessionContext _sessions;
    private readonly ConversationMemory _memory;
    private readonly UserProfileService _profiles;
    private readonly EthicalAIFrameworkService _consent;
    private readonly JwtTokenService _tokens;
    private readonly IAuditLog _audit;
    private readonly IHostEnvironment _env;

    public AccountController(
        UserManager<ApplicationUser> users,
        IAccountDataService accounts,
        IEphemeralSessionContext sessions,
        ConversationMemory memory,
        UserProfileService profiles,
        EthicalAIFrameworkService consent,
        JwtTokenService tokens,
        IAuditLog audit,
        IHostEnvironment env)
    {
        _users = users;
        _accounts = accounts;
        _sessions = sessions;
        _memory = memory;
        _profiles = profiles;
        _consent = consent;
        _tokens = tokens;
        _audit = audit;
        _env = env;
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Unauthorized(new { error = "Sign in required." });
        var payload = await _accounts.ExportAsync(user.Id, cancellationToken);
        await _audit.WriteAsync("account.export", user.Id, StatusCodes.Status200OK, cancellationToken);
        return Ok(payload);
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Unauthorized(new { error = "Sign in required." });

        await _accounts.EnsureDefaultsAsync(user, cancellationToken);
        var preferences = await _accounts.GetPreferencesAsync(user.Id, cancellationToken);
        return Ok(new
        {
            displayName = user.DisplayName ?? preferences?.DisplayName,
            locale = preferences?.Locale ?? "en"
        });
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> Preferences([FromBody] PreferenceUpdate request, CancellationToken cancellationToken)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Unauthorized(new { error = "Sign in required." });
        if (request == null)
            return BadRequest(new { error = "Preferences are required." });

        // Backfill defaults for legacy accounts created before preferences/privacy rows existed.
        await _accounts.EnsureDefaultsAsync(user, cancellationToken);

        string? displayName = null;
        if (request.DisplayName != null)
        {
            displayName = request.DisplayName.Trim();
            if (!DisplayNameRx.IsMatch(displayName))
                return BadRequest(new { error = "Display name must be 1-40 letters, numbers, spaces, or . ' -" });
            user.DisplayName = displayName;
            await _users.UpdateAsync(user);
        }

        string? locale = null;
        if (request.Locale != null)
        {
            locale = request.Locale.Trim().ToLowerInvariant();
            if (!Locales.Contains(locale))
                return BadRequest(new { error = "Locale must be en, ta, or tanglish." });
        }

        await _accounts.UpdatePreferencesAsync(user.Id, displayName, locale, cancellationToken);
        var saved = await _accounts.GetPreferencesAsync(user.Id, cancellationToken);
        return Ok(new
        {
            displayName = user.DisplayName ?? saved?.DisplayName,
            locale = saved?.Locale ?? "en"
        });
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Unauthorized(new { error = "Sign in required." });
        if (request == null || string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { error = "Current and new passwords are required." });
        if (request.CurrentPassword.Length > 128 || request.NewPassword.Length > 128)
            return BadRequest(new { error = "Password is too long." });

        var changed = await _users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!changed.Succeeded)
        {
            await _audit.WriteAsync("account.password.rejected", user.Id, StatusCodes.Status400BadRequest, cancellationToken);
            return BadRequest(new
            {
                error = "Password could not be changed.",
                details = changed.Errors.Select(e => e.Description)
            });
        }

        await _tokens.RevokeAllAsync(user.Id, cancellationToken);
        Response.Cookies.Delete(AuthPolicy.RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps || _env.IsProduction(),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth"
        });
        await _audit.WriteAsync("account.password.changed", user.Id, StatusCodes.Status200OK, cancellationToken);
        return Ok(new { changed = true, requiresSignIn = true });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Unauthorized(new { error = "Sign in required." });

        var userId = user.Id;
        _sessions.ClearUser(userId);
        _memory.ClearUserData(userId);
        _profiles.DeleteProfile(userId);
        _consent.RevokeAllSensitive(userId);
        await _tokens.RevokeAllAsync(userId, cancellationToken);
        await _accounts.DeleteOwnedDataAsync(userId, cancellationToken);
        var deleted = await _users.DeleteAsync(user);
        if (!deleted.Succeeded)
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "The account could not be deleted." });

        Response.Cookies.Delete(AuthPolicy.RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps || _env.IsProduction(),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth"
        });
        await _audit.WriteAsync("account.delete", userId, StatusCodes.Status200OK, cancellationToken);
        return Ok(new { deleted = true, message = "Account, consent, memory, history, and baseline were deleted." });
    }
}

public sealed class PreferenceUpdate
{
    public string? DisplayName { get; set; }
    public string? Locale { get; set; }
}

public sealed class PasswordChangeRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
