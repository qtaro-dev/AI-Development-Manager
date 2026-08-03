using System.Text.Json;

namespace Adm.Wpf.LocalChannel;

public delegate Task<object?> LocalChannelHandler(LocalChannelRequest request, CancellationToken cancellationToken);

public sealed class LocalChannelOperationRegistry
{
    private readonly IReadOnlyDictionary<string, LocalChannelHandler> handlers;

    private LocalChannelOperationRegistry(IReadOnlyDictionary<string, LocalChannelHandler> handlers) => this.handlers = handlers;

    public static LocalChannelOperationRegistry Empty { get; } = new(new Dictionary<string, LocalChannelHandler>(StringComparer.Ordinal));

    public static LocalChannelOperationRegistry FromHandlers(IReadOnlyDictionary<string, LocalChannelHandler> handlers) =>
        new(new Dictionary<string, LocalChannelHandler>(handlers, StringComparer.Ordinal));

    public bool TryGet(string operation, out LocalChannelHandler? handler) => handlers.TryGetValue(operation, out handler);
}

public sealed class LocalChannelDispatcher(LocalChannelOperationRegistry registry)
{
    public async Task<string> DispatchAsync(string json, string? source, CancellationToken cancellationToken = default)
    {
        LocalChannelRequest? request = null;
        try
        {
            request = LocalChannelProtocol.ParseRequest(json, source);
            if (!registry.TryGet(request.Operation, out var handler) || handler is null)
                throw new LocalChannelProtocolException("operation_not_allowed", "errors.localChannel.operationNotAllowed", request.RequestId);

            var result = await handler(request, cancellationToken);
            return LocalChannelProtocol.SerializeResponse(new LocalChannelResponse(request.RequestId, result));
        }
        catch (OperationCanceledException)
        {
            return LocalChannelProtocol.SerializeError(new LocalChannelError(request?.RequestId, "channel_unavailable", "errors.localChannel.channelUnavailable"));
        }
        catch (LocalChannelProtocolException exception)
        {
            return LocalChannelProtocol.SerializeError(new LocalChannelError(exception.RequestId, exception.Code, exception.MessageKey));
        }
        catch (Exception)
        {
            return LocalChannelProtocol.SerializeError(new LocalChannelError(request?.RequestId, "handler_failed", "errors.localChannel.handlerFailed"));
        }
    }
}
