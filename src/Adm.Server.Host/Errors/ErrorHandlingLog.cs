using Microsoft.Extensions.Logging;

namespace Adm.Server.Host.Errors;

public static partial class ErrorHandlingLog
{
    [LoggerMessage(EventId = 1200, Level = LogLevel.Error, Message = "API request failed with classified error {ErrorCode}")]
    public static partial void ClassifiedError(this ILogger logger, Exception exception, string errorCode);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Error, Message = "API request failed with unhandled error")]
    public static partial void UnhandledError(this ILogger logger, Exception exception);
}
