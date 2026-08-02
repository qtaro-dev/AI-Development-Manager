using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Adm.Server.Host.Health;

public sealed record AdmHealthResponse(
    string Status,
    string BuildVersion,
    string StartupMode,
    DateTimeOffset ServerTimeUtc,
    IReadOnlyList<HealthFailure> FailedContributors);

public static class AdmHealthEndpointExtensions
{
    public static IEndpointRouteBuilder MapAdmHealthEndpoints(
        this IEndpointRouteBuilder endpoints,
        string startupMode,
        string buildVersion)
    {
        endpoints.MapGet("/health/live", (AdmHealthLifecycle lifecycle) =>
        {
            var status = lifecycle.IsStopping ? "stopping" : "healthy";
            var response = new AdmHealthResponse(
                status,
                buildVersion,
                startupMode,
                DateTimeOffset.UtcNow,
                Array.Empty<HealthFailure>());
            return Results.Json(response, statusCode: lifecycle.IsStopping ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status200OK);
        }).WithName("GetLiveness").WithTags("Health");

        endpoints.MapGet("/health/ready", async (
            AdmHealthLifecycle lifecycle,
            AdmHealthRegistry registry,
            CancellationToken cancellationToken) =>
        {
            var failures = lifecycle.IsStopping
                ? [new HealthFailure("server", "stopping")]
                : (await registry.CheckReadinessAsync(cancellationToken)).ToArray();
            var isReady = lifecycle.IsStarted && failures.Length == 0;
            var response = new AdmHealthResponse(
                isReady ? "ready" : "not_ready",
                buildVersion,
                startupMode,
                DateTimeOffset.UtcNow,
                failures);
            return Results.Json(response, statusCode: isReady ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
        }).WithName("GetReadiness").WithTags("Health");

        return endpoints;
    }
}
