using System.Security.Cryptography;
using System.Text;

namespace NeuroSync.Api.Auth;

public static class AuthPolicy
{
    public const string RefreshCookieName = "ns_refresh";
    public const string DevSigningKey = "dev-only-signing-key-not-for-production-00";

    public static bool RequireAuthentication(IConfiguration config, IHostEnvironment env) =>
        config.GetValue<bool?>("Auth:RequireAuthentication") ?? env.IsProduction();

    public static string SigningKey(IConfiguration config, IHostEnvironment env)
    {
        var key = config["Auth:Jwt:SigningKey"];
        if (!string.IsNullOrWhiteSpace(key))
            return key.Trim();

        if (env.IsProduction())
            throw new InvalidOperationException(
                "Auth:Jwt:SigningKey must be provided as a server environment secret in Production.");

        return DevSigningKey;
    }

    public static bool FixedTimeEquals(string provided, string expected)
    {
        var left = Encoding.UTF8.GetBytes(provided);
        var right = Encoding.UTF8.GetBytes(expected);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }

    public static string Sha256(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash);
    }
}
