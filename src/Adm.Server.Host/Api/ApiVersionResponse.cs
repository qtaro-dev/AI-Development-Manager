namespace Adm.Server.Host.Api;

public enum ApiStatus
{
    Ready
}

public sealed record ApiVersionResponse(
    string ApiVersion,
    string ContractVersion,
    DateTimeOffset ServerTimeUtc,
    ApiStatus Status,
    string? ResourceId);
