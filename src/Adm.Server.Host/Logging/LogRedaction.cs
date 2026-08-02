using System.Text.RegularExpressions;

namespace Adm.Server.Host.Logging;

public static partial class LogRedaction
{
    public static bool IsSensitiveKey(string key)
    {
        return key.Contains("password", StringComparison.OrdinalIgnoreCase)
            || key.Contains("cookie", StringComparison.OrdinalIgnoreCase)
            || key.Contains("authorization", StringComparison.OrdinalIgnoreCase)
            || key.Contains("token", StringComparison.OrdinalIgnoreCase)
            || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || key.Contains("privatekey", StringComparison.OrdinalIgnoreCase);
    }

    public static object? RedactValue(string key, object? value)
    {
        return IsSensitiveKey(key) ? "[REDACTED]" : RedactText(value?.ToString());
    }

    public static string? RedactText(string? value)
    {
        return value is null
            ? null
            : SensitiveValueRegex().Replace(
                BearerTokenRegex().Replace(value, "Bearer [REDACTED]"),
                "$1=[REDACTED]");
    }

    [GeneratedRegex("(?i)\\bBearer\\s+[^\\s]+", RegexOptions.CultureInvariant)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex("(?i)\\b(password|token|secret|api[-_]?key|authorization)=([^&\\s]+)", RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveValueRegex();
}
