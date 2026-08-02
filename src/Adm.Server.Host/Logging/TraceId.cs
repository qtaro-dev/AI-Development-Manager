using System.Text.RegularExpressions;

namespace Adm.Server.Host.Logging;

public static partial class TraceId
{
    public const string HeaderName = "X-Request-Id";

    public static string GetOrCreate(string? candidate)
    {
        return candidate is not null && SafeTraceIdRegex().IsMatch(candidate)
            ? candidate
            : $"adm-{Guid.NewGuid():N}";
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeTraceIdRegex();
}
