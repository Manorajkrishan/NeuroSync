using NeuroSync.Api.Auth;
using NeuroSync.Api.Services;

namespace NeuroSync.Api.Middleware;

/// <summary>Loads the signed-in account's consent from the database into the request's consent cache.</summary>
public sealed class PrivacyHydratorMiddleware
{
    private readonly RequestDelegate _next;

    public PrivacyHydratorMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IAccountDataService accounts, EthicalAIFrameworkService consent)
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            var stored = await accounts.GetConsentAsync(userId, context.RequestAborted).ConfigureAwait(false);
            if (stored != null)
                consent.Hydrate(userId, stored);
        }

        await _next(context);
    }
}
