namespace Adm.Application.Health;

public interface IHealthContributor
{
    public string Name { get; }

    public ValueTask<HealthContributorResult> CheckAsync(CancellationToken cancellationToken);
}

public sealed record HealthContributorResult(bool IsReady, string FailureCode = "dependency_unavailable");
