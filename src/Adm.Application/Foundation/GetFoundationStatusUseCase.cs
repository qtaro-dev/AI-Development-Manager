using System.Reflection;

namespace Adm.Application.Foundation;

public interface IGetFoundationStatusUseCase
{
    public Task<FoundationStatus> ExecuteAsync(CancellationToken cancellationToken = default);
}

public sealed class GetFoundationStatusUseCase : IGetFoundationStatusUseCase
{
    public const string ProductName = "AI Development Manager";
    public static string ProductVersion =>
        typeof(GetFoundationStatusUseCase).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "unknown";

    private readonly string apiVersion;
    private readonly string executionMode;

    public GetFoundationStatusUseCase(string apiVersion = ApiVersion, string executionMode = ExecutionMode)
    {
        this.apiVersion = apiVersion;
        this.executionMode = executionMode;
    }
    public const string ApiVersion = "local";
    public const string ContractVersion = "1.0";
    public const string ExecutionMode = "local";

    public Task<FoundationStatus> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new FoundationStatus(
            State: "ready",
            apiVersion,
            ContractVersion,
            DateTimeOffset.UtcNow,
            ProductName,
            ProductVersion,
            executionMode));
    }
}
