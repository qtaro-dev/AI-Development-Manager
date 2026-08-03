namespace Adm.Application.Foundation;

public sealed record FoundationStatus(
    string State,
    string ApiVersion,
    string ContractVersion,
    DateTimeOffset ServerTimeUtc,
    string ProductName,
    string ProductVersion,
    string ExecutionMode);
