using Adm.Application.Foundation;
using Adm.Wpf.LocalChannel;

namespace Adm.Wpf.Composition;

public sealed class LocalCompositionRoot : IDisposable
{
    private readonly CancellationTokenSource shutdown = new();
    private readonly CancellationToken shutdownToken;
    private readonly LocalChannelDispatcher dispatcher;

    public LocalCompositionRoot()
    {
        shutdownToken = shutdown.Token;
        var foundationStatus = new GetFoundationStatusUseCase();
        dispatcher = new LocalChannelDispatcher(
            LocalChannelOperationRegistry.FromHandlers(new Dictionary<string, LocalChannelHandler>(StringComparer.Ordinal)
            {
                ["getFoundationStatus"] = (request, cancellationToken) =>
                    ExecuteFoundationStatusAsync(foundationStatus, cancellationToken),
            }));
    }

    private static async Task<object?> ExecuteFoundationStatusAsync(
        GetFoundationStatusUseCase useCase,
        CancellationToken cancellationToken) =>
        await useCase.ExecuteAsync(cancellationToken);

    public Task<string> DispatchAsync(string json, string? source) =>
        dispatcher.DispatchAsync(json, source, shutdownToken);

    public void Dispose()
    {
        shutdown.Cancel();
        shutdown.Dispose();
    }
}
