using Microsoft.Extensions.Logging;

namespace Adm.Server.Host.Logging;

internal static partial class RequestTracingLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "HTTP request started {Method} {Path}")]
    public static partial void RequestStarted(this ILogger logger, string Method, string Path);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "HTTP request completed {StatusCode} {ElapsedMs}")]
    public static partial void RequestCompleted(this ILogger logger, int StatusCode, long ElapsedMs);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "HTTP request failed {StatusCode} {ElapsedMs} {ExceptionType}")]
    public static partial void RequestFailed(this ILogger logger, int StatusCode, long ElapsedMs, string ExceptionType);
}
