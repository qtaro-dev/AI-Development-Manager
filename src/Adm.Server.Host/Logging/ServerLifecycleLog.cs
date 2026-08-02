using Microsoft.Extensions.Logging;

namespace Adm.Server.Host.Logging;

internal static partial class ServerLifecycleLog
{
    [LoggerMessage(EventId = 1100, Level = LogLevel.Information, Message = "Server started {StartupMode} {EnvironmentName} {BuildVersion}")]
    public static partial void ServerStarted(this ILogger logger, string StartupMode, string EnvironmentName, string BuildVersion);
}
