using System.Text.RegularExpressions;

namespace NeuroSync.Core;

/// <summary>
/// Validates userIds used in file paths and SignalR groups.
/// Allows only safe charset: letters, digits, underscore, hyphen (max 64).
/// </summary>
public static class UserIdSanitizer
{
    private static readonly Regex SafeRx = new(
        @"^[A-Za-z0-9_-]{1,64}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public const string DefaultUserId = "default";

    public static bool IsValid(string? userId) =>
        !string.IsNullOrWhiteSpace(userId) && SafeRx.IsMatch(userId);

    /// <summary>
    /// Returns a safe userId, or DefaultUserId when null/empty.
    /// Returns false when the value is present but unsafe (path traversal risk).
    /// </summary>
    public static bool TryNormalize(string? userId, out string normalized)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            normalized = DefaultUserId;
            return true;
        }

        var trimmed = userId.Trim();
        if (!SafeRx.IsMatch(trimmed))
        {
            normalized = DefaultUserId;
            return false;
        }

        normalized = trimmed;
        return true;
    }

    public static string NormalizeOrDefault(string? userId) =>
        TryNormalize(userId, out var n) ? n : DefaultUserId;
}
