using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;
using NeuroSync.Api.Auth;
using NeuroSync.Core;

namespace NeuroSync.Api.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        headers["Content-Security-Policy"] =
            "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'; " +
            "script-src 'self' 'unsafe-inline'; style-src 'self' https://fonts.googleapis.com 'unsafe-inline'; " +
            "font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; connect-src 'self'";
        await _next(context);
    }
}

/// <summary>
/// When authentication is required, the signed-in account is the only user id that may be used.
/// A body, query, route, or header that names someone else is rejected.
/// </summary>
public sealed class UserIdentityEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;

    public UserIdentityEnforcementMiddleware(RequestDelegate next, IConfiguration config, IHostEnvironment env)
    {
        _next = next;
        _config = config;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!AuthPolicy.RequireAuthentication(_config, _env) || IsAnonymous(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !UserIdSanitizer.IsValid(userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Sign in required." });
            return;
        }

        if (!IdentifiersMatch(context, userId))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "That user id does not match the signed-in account." });
            return;
        }

        if (context.Request.RouteValues.ContainsKey("userId"))
            context.Request.RouteValues["userId"] = userId;

        var header = context.Request.Headers["X-User-Id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(header))
            context.Request.Headers["X-User-Id"] = userId;

        RewriteQuery(context, userId);
        if (!await RewriteJsonUserIdAsync(context, userId).ConfigureAwait(false))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "That user id does not match the signed-in account." });
            return;
        }

        context.Items["NeuroSync.UserId"] = userId;
        await _next(context);
    }

    private static bool IsAnonymous(PathString path)
    {
        if (path.StartsWithSegments("/health")) return true;
        if (path.StartsWithSegments("/swagger")) return true;
        if (path.StartsWithSegments("/api/auth/register")) return true;
        if (path.StartsWithSegments("/api/auth/login")) return true;
        if (path.StartsWithSegments("/api/auth/refresh")) return true;
        if (path.StartsWithSegments("/api/auth/config")) return true;
        if (path.StartsWithSegments("/api/ops/metrics")) return true;
        if (!path.StartsWithSegments("/api") && !path.StartsWithSegments("/emotionHub"))
            return true;
        return false;
    }

    private static bool IdentifiersMatch(HttpContext context, string userId)
    {
        if (context.Request.RouteValues.TryGetValue("userId", out var route) &&
            route is string routeId &&
            !string.IsNullOrWhiteSpace(routeId) &&
            !string.Equals(routeId, userId, StringComparison.Ordinal))
            return false;

        var query = context.Request.Query["userId"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(query) && !string.Equals(query, userId, StringComparison.Ordinal))
            return false;

        var header = context.Request.Headers["X-User-Id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(header) && !string.Equals(header, userId, StringComparison.Ordinal))
            return false;

        return true;
    }

    private static void RewriteQuery(HttpContext context, string userId)
    {
        if (!context.Request.Query.ContainsKey("userId") && context.Request.QueryString.HasValue == false)
            return;
        if (!context.Request.Query.ContainsKey("userId"))
            return;

        var parsed = QueryHelpers.ParseQuery(context.Request.QueryString.Value);
        parsed["userId"] = userId;
        var pairs = new List<string>();
        foreach (var pair in parsed)
        {
            foreach (var value in pair.Value)
                pairs.Add($"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(value ?? "")}");
        }
        context.Request.QueryString = new QueryString(pairs.Count == 0 ? "" : "?" + string.Join("&", pairs));
    }

    private static async Task<bool> RewriteJsonUserIdAsync(HttpContext context, string userId)
    {
        var method = context.Request.Method;
        if (method != HttpMethods.Post && method != HttpMethods.Put && method != HttpMethods.Patch)
            return true;
        var contentType = context.Request.ContentType ?? "";
        if (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return true;
        if (context.Request.ContentLength is 0)
            return true;

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync().ConfigureAwait(false);
        context.Request.Body.Position = 0;
        if (string.IsNullOrWhiteSpace(body))
            return true;

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            return true;
        }

        if (node is not JsonObject obj)
            return true;

        if (TryReadUserId(obj, out var supplied) &&
            !string.IsNullOrWhiteSpace(supplied) &&
            !string.Equals(supplied, userId, StringComparison.Ordinal))
            return false;

        obj["userId"] = userId;
        var rewritten = Encoding.UTF8.GetBytes(obj.ToJsonString());
        context.Request.Body = new MemoryStream(rewritten);
        context.Request.ContentLength = rewritten.Length;
        return true;
    }

    private static bool TryReadUserId(JsonObject obj, out string? userId)
    {
        foreach (var property in obj)
        {
            if (!property.Key.Equals("userId", StringComparison.OrdinalIgnoreCase))
                continue;
            try
            {
                userId = property.Value?.GetValue<string>();
            }
            catch (InvalidOperationException)
            {
                userId = property.Value?.ToJsonString();
            }
            return true;
        }
        userId = null;
        return false;
    }
}
