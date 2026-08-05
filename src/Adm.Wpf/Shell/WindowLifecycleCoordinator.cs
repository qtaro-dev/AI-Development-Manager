namespace Adm.Wpf.Shell;

public sealed class WindowLifecycleCoordinator : IDisposable
{
    private readonly object sync = new();
    private readonly CancellationTokenSource lifetime = new();
    private ConnectionAttempt? currentAttempt;
    private long nextGeneration;
    private bool isDisposed;

    public CancellationToken LifetimeToken => lifetime.Token;

    public ConnectionAttempt BeginAttempt()
    {
        lock (sync)
        {
            ThrowIfDisposed();
            currentAttempt?.Cancel();
            var attempt = new ConnectionAttempt(++nextGeneration, lifetime.Token);
            currentAttempt = attempt;
            return attempt;
        }
    }

    public bool IsCurrent(ConnectionAttempt attempt)
    {
        lock (sync)
        {
            return !isDisposed && ReferenceEquals(currentAttempt, attempt) && !attempt.Token.IsCancellationRequested;
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            currentAttempt?.Cancel();
            currentAttempt = null;
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(isDisposed, this);
}

public sealed class ConnectionAttempt : IDisposable
{
    private readonly CancellationTokenSource cancellation;

    internal ConnectionAttempt(long generation, CancellationToken lifetimeToken)
    {
        Generation = generation;
        cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken);
    }

    public long Generation { get; }

    public CancellationToken Token => cancellation.Token;

    internal void Cancel() => cancellation.Cancel();

    public void Dispose() => cancellation.Dispose();
}
