using Microsoft.Extensions.Logging;

namespace Adm.Server.Host.Logging;

public static partial class ServerStoppingLog
{
    [LoggerMessage(EventId = 1101, Level = LogLevel.Information, Message = "Server stopping {startupMode}; configured stop timeout {stopTimeout}")]
    public static partial void ServerStopping(this ILogger logger, string startupMode, TimeSpan stopTimeout);
}
