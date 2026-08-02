using Microsoft.Extensions.Logging;

namespace Adm.Server.Host.Health;

public static partial class AdmHealthLog
{
    [LoggerMessage(EventId = 1300, Level = LogLevel.Error, Message = "Health contributor failed: {ContributorName}")]
    public static partial void ContributorFailed(this ILogger logger, Exception exception, string contributorName);
}
