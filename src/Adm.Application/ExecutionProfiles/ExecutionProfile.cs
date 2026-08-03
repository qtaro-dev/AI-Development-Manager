using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adm.Application.ExecutionProfiles;

public enum ExecutionProfileMode
{
    Local,
    Server,
}

public sealed record ExecutionProfile(
    int SchemaVersion,
    ExecutionProfileMode Mode,
    string? ServerUri);

public sealed record ExecutionProfileUpdate(
    ExecutionProfileMode Mode,
    string? ServerUri);

public sealed record ExecutionProfileReadResult(
    ExecutionProfile Profile,
    bool UsedLocalFallback,
    string? WarningCode);

public interface IExecutionProfileStore
{
    public Task<string?> ReadAsync(CancellationToken cancellationToken = default);

    public Task WriteAsync(string json, CancellationToken cancellationToken = default);
}

public sealed class ExecutionProfileValidationException(string code) : Exception
{
    public string Code { get; } = code;
}

public sealed class ExecutionProfileStorageException : Exception
{
    public ExecutionProfileStorageException(Exception innerException)
        : base("Execution profile storage failed.", innerException)
    {
    }
}

public sealed class ExecutionProfileService
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    static ExecutionProfileService()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    private readonly IExecutionProfileStore store;
    private readonly bool allowLoopbackHttp;

    public ExecutionProfileService(IExecutionProfileStore store, bool allowLoopbackHttp = false)
    {
        this.store = store;
        this.allowLoopbackHttp = allowLoopbackHttp;
    }

    public async Task<ExecutionProfileReadResult> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await store.ReadAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new(DefaultLocal(), false, null);
            }

            var profile = ParseAndValidate(json);
            return new(profile, false, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or ExecutionProfileValidationException or IOException or UnauthorizedAccessException)
        {
            return new(DefaultLocal(), true, "profile_recovered_local");
        }
    }

    public async Task<ExecutionProfile> UpdateAsync(
        ExecutionProfileUpdate update,
        CancellationToken cancellationToken = default)
    {
        var profile = Validate(new ExecutionProfile(CurrentSchemaVersion, update.Mode, update.ServerUri));
        try
        {
            await store.WriteAsync(JsonSerializer.Serialize(profile, JsonOptions), cancellationToken);
            return profile;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new ExecutionProfileStorageException(exception);
        }
    }

    public ExecutionProfile ParseAndValidate(string json)
    {
        try
        {
            var profile = JsonSerializer.Deserialize<ExecutionProfile>(json, JsonOptions)
                ?? throw new ExecutionProfileValidationException("invalid_profile");
            return Validate(profile);
        }
        catch (JsonException)
        {
            throw new ExecutionProfileValidationException("invalid_profile");
        }
    }

    private ExecutionProfile Validate(ExecutionProfile profile)
    {
        if (profile.SchemaVersion != CurrentSchemaVersion)
            throw new ExecutionProfileValidationException("unsupported_schema");

        if (profile.Mode == ExecutionProfileMode.Local)
        {
            if (profile.ServerUri is not null)
                throw new ExecutionProfileValidationException("invalid_local_profile");

            return profile;
        }

        if (profile.Mode != ExecutionProfileMode.Server || string.IsNullOrWhiteSpace(profile.ServerUri))
            throw new ExecutionProfileValidationException("invalid_server_profile");

        if (!Uri.TryCreate(profile.ServerUri, UriKind.Absolute, out var uri) || !IsAllowedServerUri(uri))
            throw new ExecutionProfileValidationException("invalid_server_url");

        return profile with { ServerUri = EnsureTrailingSlash(uri).AbsoluteUri };
    }

    private bool IsAllowedServerUri(Uri uri) =>
        uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
        allowLoopbackHttp && uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && IsLoopback(uri.Host);

    private static bool IsLoopback(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("127.0.0.1", StringComparison.Ordinal) ||
        host.Equals("::1", StringComparison.Ordinal);

    private static Uri EnsureTrailingSlash(Uri uri) =>
        uri.AbsolutePath.EndsWith('/')
            ? uri
            : new Uri(uri + "/");

    public static ExecutionProfile DefaultLocal() =>
        new(CurrentSchemaVersion, ExecutionProfileMode.Local, null);
}
