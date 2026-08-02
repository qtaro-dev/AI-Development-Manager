using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Adm.Server.Host.Errors;

namespace Adm.Server.Host.Api;

public static class ApiEndpointExtensions
{
    public static RouteGroupBuilder MapAdmApiV1(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");

        api.MapGet("/version", () => new ApiVersionResponse(
                "v1",
                "1.0",
                DateTimeOffset.UtcNow,
                ApiStatus.Ready,
                null))
            .WithName("GetApiVersion")
            .WithTags("System")
            .WithSummary("Returns the API version and protocol readiness.")
            .Produces<ApiVersionResponse>(StatusCodes.Status200OK)
            .Produces<AdmProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
            .Produces<AdmProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json");

        return api;
    }
}
