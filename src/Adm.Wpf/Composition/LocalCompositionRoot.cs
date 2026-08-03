using Adm.Application.Foundation;
using Adm.Application.ExecutionProfiles;
using Adm.Wpf.Configuration;
using Adm.Wpf.LocalChannel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adm.Wpf.Composition;

public sealed class LocalCompositionRoot : IDisposable
{
    private static readonly JsonSerializerOptions ProfileJsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
    private readonly CancellationTokenSource shutdown = new();
    private readonly CancellationToken shutdownToken;
    private readonly LocalChannelDispatcher dispatcher;
    private readonly ExecutionProfileService executionProfiles;

    public LocalCompositionRoot(ExecutionProfileService? executionProfiles = null)
    {
        shutdownToken = shutdown.Token;
        this.executionProfiles = executionProfiles ?? new ExecutionProfileService(new JsonExecutionProfileStore());
        var foundationStatus = new GetFoundationStatusUseCase();
        dispatcher = new LocalChannelDispatcher(
            LocalChannelOperationRegistry.FromHandlers(new Dictionary<string, LocalChannelHandler>(StringComparer.Ordinal)
            {
                ["getFoundationStatus"] = (request, cancellationToken) =>
                    ExecuteFoundationStatusAsync(foundationStatus, cancellationToken),
                ["executionProfile.get"] = (request, cancellationToken) =>
                    GetExecutionProfileAsync(this.executionProfiles, cancellationToken),
                ["executionProfile.update"] = UpdateExecutionProfileAsync,
            }));
    }

    private static async Task<object?> ExecuteFoundationStatusAsync(
        GetFoundationStatusUseCase useCase,
        CancellationToken cancellationToken) =>
        await useCase.ExecuteAsync(cancellationToken);

    private async Task<object?> UpdateExecutionProfileAsync(LocalChannelRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var update = JsonSerializer.Deserialize<ExecutionProfileUpdate>(request.Payload.GetRawText(), ProfileJsonOptions)
                ?? throw new ExecutionProfileValidationException("invalid_profile");
            return await executionProfiles.UpdateAsync(update, cancellationToken);
        }
        catch (ExecutionProfileValidationException)
        {
            throw new LocalChannelProtocolException("invalid_request", "errors.localChannel.invalidRequest", request.RequestId);
        }
        catch (ExecutionProfileStorageException)
        {
            throw new LocalChannelProtocolException("handler_failed", "errors.localChannel.handlerFailed", request.RequestId);
        }
    }

    private static async Task<object?> GetExecutionProfileAsync(ExecutionProfileService service, CancellationToken cancellationToken) =>
        await service.GetAsync(cancellationToken);

    public Task<string> DispatchAsync(string json, string? source) =>
        dispatcher.DispatchAsync(json, source, shutdownToken);

    public void Dispose()
    {
        shutdown.Cancel();
        shutdown.Dispose();
    }
}
