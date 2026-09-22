using System.Security.Cryptography;
using System.Text;

namespace NeuroSync.Api.Middleware;

/// <summary>
/// Simple API-key auth for V1. Production requires X-Api-Key (or ?api_key= for SignalR).
/// Development can disable via Auth:RequireApiKey=false.
/// </summary>
public class ApiKeyAuthMiddleware
{
    public const string HeaderName = "X-Api-Key";
    public const string QueryName = "api_key";

    private static readonly PathString[] AnonymousPaths =
    {
        new("/health"),
        new("/swagger"),
        new("/index.html"),
        new("/v1.html"),
        new("/legacy/index.html"),
        new("/favicon.ico")
    };

    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;

    public ApiKeyAuthMiddleware(
        RequestDelegate next,
        IConfiguration config,
        IHostEnvironment env,
        ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _config = config;
        _env = env;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsAnonymous(context.Request))
        {
            await _next(context);
            return;
        }

        var require = _config.GetValue("Auth:RequireApiKey", !_env.IsDevelopment());
        if (!require)
        {
            await _next(context);
            return;
        }

        var expected = _config["Auth:ApiKey"];
        if (string.IsNullOrWhiteSpace(expected))
        {
            _logger.LogError("Auth:RequireApiKey is true but Auth:ApiKey is empty");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { error = "API authentication is misconfigured." });
            return;
        }

        var provided = context.Request.Headers[HeaderName].FirstOrDefault()
                       ?? context.Request.Query[QueryName].FirstOrDefault();

        if (string.IsNullOrEmpty(provided) || !FixedTimeEquals(provided, expected))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized. Provide a valid X-Api-Key header (or api_key query for SignalR)."
            });
            return;
        }

        await _next(context);
    }

    private static bool IsAnonymous(HttpRequest request)
    {
        var path = request.Path;
        if (!path.HasValue) return true;

        // Static demo assets
        if (path.StartsWithSegments("/css") || path.StartsWithSegments("/js")
            || path.Value!.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
            || path.Value.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
            || path.Value.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
            || path.Value.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || path.Value.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)
            || path.Value.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var p in AnonymousPaths)
        {
            if (path.StartsWithSegments(p)) return true;
        }

        // Swagger UI assets
        if (path.StartsWithSegments("/swagger")) return true;

        return false;
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
