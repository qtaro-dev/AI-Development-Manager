using Adm.Application.Health;
using Microsoft.Extensions.Logging;

namespace Adm.Server.Host.Health;

public sealed record HealthFailure(string Name, string Code);

public sealed class AdmHealthRegistry(
    IEnumerable<IHealthContributor> contributors,
    ILogger<AdmHealthRegistry> logger)
{
    public async ValueTask<IReadOnlyList<HealthFailure>> CheckReadinessAsync(CancellationToken cancellationToken)
    {
        var failures = new List<HealthFailure>();

        foreach (var contributor in contributors)
        {
            try
            {
                var result = await contributor.CheckAsync(cancellationToken);
                if (!result.IsReady)
                {
                    failures.Add(new HealthFailure(contributor.Name, result.FailureCode));
                }
            }
            catch (Exception exception)
            {
                logger.ContributorFailed(exception, contributor.Name);
                failures.Add(new HealthFailure(contributor.Name, "dependency_unavailable"));
            }
        }

        return failures;
    }
}
