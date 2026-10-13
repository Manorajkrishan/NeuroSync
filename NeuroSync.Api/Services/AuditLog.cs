using NeuroSync.Api.Auth;

namespace NeuroSync.Api.Services;

public interface IAuditLog
{
    Task WriteAsync(string action, string? userId, int statusCode, CancellationToken cancellationToken = default);
}

public sealed class AuditLog : IAuditLog
{
    private readonly Data.NeuroSyncDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<AuditLog> _logger;

    public AuditLog(Data.NeuroSyncDbContext db, IHttpContextAccessor http, ILogger<AuditLog> logger)
    {
        _db = db;
        _http = http;
        _logger = logger;
    }

    public async Task WriteAsync(string action, string? userId, int statusCode, CancellationToken cancellationToken = default)
    {
        var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var ipHash = string.IsNullOrEmpty(ip) ? null : AuthPolicy.Sha256(ip);
        _db.AuditEvents.Add(new Data.AuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            Action = action,
            UserId = userId,
            IpHash = ipHash,
            StatusCode = statusCode
        });
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit write failed for {Action}", action);
        }

        _logger.LogInformation("audit {Action} status={StatusCode} user={UserId}", action, statusCode, userId ?? "-");
    }
}
