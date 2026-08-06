using System.Windows;
using Microsoft.Win32;

namespace Adm.Wpf.Bridge;

public interface IProjectFolderPicker
{
    public Task<string?> PickAsync(Window owner, CancellationToken cancellationToken);
}

public sealed class WindowsProjectFolderPicker : IProjectFolderPicker
{
    public Task<string?> PickAsync(Window owner, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dialog = new OpenFolderDialog
        {
            Title = "Projectフォルダーを選択",
            Multiselect = false,
        };
        return Task.FromResult(dialog.ShowDialog(owner) == true ? dialog.FolderName : null);
    }
}

public sealed class ProjectFolderPickerBridge : IDisposable
{
    private readonly IProjectFolderPicker picker;
    private readonly SemaphoreSlim dialogGate = new(1, 1);
    private bool disposed;

    public ProjectFolderPickerBridge(IProjectFolderPicker picker)
    {
        this.picker = picker;
    }

    public async Task<string> DispatchAsync(BridgeRequest request, Window owner, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (request.Operation != BridgeProtocol.SelectProjectFolder)
            throw new BridgeProtocolException("operation_not_allowed", "許可されていないBridge操作です。", request.RequestId);
        if (request.MessageType == "cancel")
            return BridgeProtocol.FolderCancelled(request.RequestId);

        if (!await dialogGate.WaitAsync(0, cancellationToken))
            return BridgeProtocol.Error("busy", "フォルダー選択を開始できませんでした。", request.RequestId, request.Operation);

        try
        {
            var path = await picker.PickAsync(owner, cancellationToken);
            return path is null
                ? BridgeProtocol.FolderCancelled(request.RequestId)
                : BridgeProtocol.FolderSelected(request.RequestId, path);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return BridgeProtocol.FolderCancelled(request.RequestId);
        }
        catch (Exception)
        {
            return BridgeProtocol.Error("bridge_error", "フォルダー選択を処理できませんでした。", request.RequestId, request.Operation);
        }
        finally
        {
            dialogGate.Release();
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        dialogGate.Dispose();
    }
}
