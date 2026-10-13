namespace NeuroSync.Api.Hosting;

public static class ProductionConfigurationGuard
{
    public static void Validate(IHostEnvironment env, IConfiguration config)
    {
        if (!env.IsProduction())
            return;

        var key = config["Auth:Jwt:SigningKey"] ?? "";
        if (key.Length < 32 || IsPlaceholder(key))
        {
            throw new InvalidOperationException(
                "Set a unique Auth:Jwt:SigningKey of at least 32 characters as a server environment secret.");
        }

        if (config.GetValue("Auth:RequireAuthentication", true) == false)
            throw new InvalidOperationException("Production requires Auth:RequireAuthentication=true.");

        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        if (!origins.Any(origin => !string.IsNullOrWhiteSpace(origin)))
            throw new InvalidOperationException("Set Cors:AllowedOrigins for Production. The API fails closed without an explicit origin.");

        var provider = LiveBetaComposition.ResolveProvider(config["Database:Provider"], config.GetConnectionString("DefaultConnection"));
        var connectionString = config.GetConnectionString("DefaultConnection");
        if (provider is "inmemory" || string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Production requires Postgres (or SQL Server). In-memory storage is not allowed.");

        if (IsPlaceholder(connectionString) || HasDatabasePassword(connectionString, "neurosync"))
            throw new InvalidOperationException("Set a unique database password in the server environment. The committed local default is rejected.");

        if (config.GetValue("Beta:InviteOnly", false))
        {
            var codes = config.GetSection("Beta:InviteCodes").Get<string[]>() ?? Array.Empty<string>();
            var usable = codes.Any(code => !string.IsNullOrWhiteSpace(code) && !IsPlaceholder(code));
            if (!usable)
                throw new InvalidOperationException("Invite-only beta needs at least one real Beta:InviteCodes value.");
        }

        if (config.GetValue("Companion:Llm:Enabled", false) && string.IsNullOrWhiteSpace(config["Companion:Llm:ApiKey"]))
        {
            throw new InvalidOperationException(
                "Companion:Llm:Enabled is true but Companion:Llm:ApiKey is empty. Put the key in the server environment only.");
        }
    }

    private static bool HasDatabasePassword(string connectionString, string password)
    {
        const string marker = "Password=";
        var index = 0;
        while ((index = connectionString.IndexOf(marker, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var start = index + marker.Length;
            var end = connectionString.IndexOf(';', start);
            if (end < 0)
                end = connectionString.Length;
            if (string.Equals(connectionString[start..end], password, StringComparison.Ordinal))
                return true;
            index = start;
        }

        return false;
    }

    private static bool IsPlaceholder(string value)
    {
        var markers = new[] { "dev-only", "change-me", "replace-this", "replace-with" };
        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
