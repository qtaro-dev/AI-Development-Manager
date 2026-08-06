namespace Adm.Infrastructure.Windows.Tests;

using Adm.Wpf.Bridge;

public sealed class ProjectFolderPickerBridgeTests
{
    [Fact]
    public async Task SelectionReturnsOnlyTheSelectedPath()
    {
        using var bridge = new ProjectFolderPickerBridge(new FakePicker("C:\\Projects\\Demo"));

        var response = await bridge.DispatchAsync(Request(), null!, CancellationToken.None);

        Assert.Contains("\"selected\":true", response);
        Assert.Contains("C:\\\\Projects\\\\Demo", response);
        Assert.DoesNotContain("register", response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UserCancellationReturnsCancelledWithoutAPath()
    {
        using var bridge = new ProjectFolderPickerBridge(new FakePicker(null));

        var response = await bridge.DispatchAsync(Request(), null!, CancellationToken.None);

        Assert.Contains("\"status\":\"cancelled\"", response);
        Assert.DoesNotContain("path", response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConcurrentDialogRequestsAreRejected()
    {
        var picker = new BlockingPicker();
        using var bridge = new ProjectFolderPickerBridge(picker);
        var first = bridge.DispatchAsync(Request("first"), null!, CancellationToken.None);

        await picker.Started.Task;
        var second = await bridge.DispatchAsync(Request("second"), null!, CancellationToken.None);

        Assert.Contains("\"code\":\"busy\"", second);
        picker.Release.TrySetResult(true);
        await first;
    }

    [Fact]
    public async Task DisposedBridgeDoesNotStartARequest()
    {
        var bridge = new ProjectFolderPickerBridge(new FakePicker("C:\\Projects\\Demo"));
        bridge.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => bridge.DispatchAsync(Request(), null!, CancellationToken.None));
    }

    private static BridgeRequest Request(string id = "adm-folder") =>
        new("request", BridgeProtocol.SelectProjectFolder, id);

    private sealed class FakePicker(string? path) : IProjectFolderPicker
    {
        public Task<string?> PickAsync(System.Windows.Window owner, CancellationToken cancellationToken) => Task.FromResult(path);
    }

    private sealed class BlockingPicker : IProjectFolderPicker
    {
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<string?> PickAsync(System.Windows.Window owner, CancellationToken cancellationToken)
        {
            Started.TrySetResult(true);
            await Release.Task.WaitAsync(cancellationToken);
            return "C:\\Projects\\Demo";
        }
    }
}
