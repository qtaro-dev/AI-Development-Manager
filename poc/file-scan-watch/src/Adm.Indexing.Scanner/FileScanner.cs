using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Adm.Indexing.Scanner;

public sealed record ScanItem(string RelativePath, long Length, DateTimeOffset LastWriteUtc, string Sha256);
public sealed record ScanError(string RelativePath, string ErrorCode, string Detail, int Attempts);
public sealed record ScanProgress(int Processed, int Discovered, string? CurrentPath, bool Completed);
public sealed record ScanReport(IReadOnlyDictionary<string, ScanItem> Items, IReadOnlyList<ScanError> Errors, bool Canceled, int Processed, int Discovered);
public sealed record SnapshotChange(string Kind, string RelativePath, string? PreviousPath = null);
public sealed record HealthStatus(bool Healthy, int PendingEvents, DateTimeOffset? LastScanUtc, int ErrorCount, string Status);

public sealed class FileScanner
{
    private readonly string _root;
    private readonly int _maxAttempts;
    private readonly TimeSpan _retryDelay;

    public FileScanner(string root, int maxAttempts = 3, TimeSpan? retryDelay = null)
    {
        _root = Path.GetFullPath(root);
        _maxAttempts = maxAttempts;
        _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(25);
    }

    public async Task<ScanReport> ScanAsync(IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var paths = EnumerateTargetFiles().ToArray();
        var items = new Dictionary<string, ScanItem>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<ScanError>();
        var processed = 0;
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Normalize(Path.GetRelativePath(_root, path));
            if (TryRead(path, relative, out var item, out var error)) items[relative] = item!;
            else errors.Add(error!);
            processed++;
            progress?.Report(new ScanProgress(processed, paths.Length, relative, false));
            await Task.Yield();
        }
        progress?.Report(new ScanProgress(processed, paths.Length, null, true));
        return new ScanReport(items, errors, false, processed, paths.Length);
    }

    public ScanReport Scan(IProgress<ScanProgress>? progress = null) => ScanAsync(progress).GetAwaiter().GetResult();

    private bool TryRead(string path, string relative, out ScanItem? item, out ScanError? error)
    {
        item = null; error = null;
        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                var info = new FileInfo(path);
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var hash = Convert.ToHexString(SHA256.HashData(stream));
                item = new ScanItem(relative, info.Length, info.LastWriteTimeUtc, hash);
                return true;
            }
            catch (IOException exception) when (attempt < _maxAttempts)
            {
                Thread.Sleep(_retryDelay);
                error = new ScanError(relative, "read_retry", exception.GetType().Name, attempt);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                error = new ScanError(relative, "read_failed", exception.GetType().Name, attempt);
                return false;
            }
        }
        return false;
    }

    private IEnumerable<string> EnumerateTargetFiles()
    {
        if (!Directory.Exists(_root)) yield break;
        foreach (var path in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
        {
            var relative = Normalize(Path.GetRelativePath(_root, path));
            if (relative.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || (relative.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && (relative.StartsWith(".adm-meta/", StringComparison.OrdinalIgnoreCase) || relative.Contains("/.adm-meta/", StringComparison.OrdinalIgnoreCase)))) yield return path;
        }
    }

    public static string Normalize(string path) => path.Replace('\\', '/');
}

public static class SnapshotDiff
{
    public static IReadOnlyList<SnapshotChange> Calculate(IReadOnlyDictionary<string, ScanItem> previous, IReadOnlyDictionary<string, ScanItem> current)
    {
        var changes = new List<SnapshotChange>();
        var removed = previous.Keys.Except(current.Keys, StringComparer.OrdinalIgnoreCase).ToDictionary(key => key, key => previous[key], StringComparer.OrdinalIgnoreCase);
        var added = current.Keys.Except(previous.Keys, StringComparer.OrdinalIgnoreCase).ToDictionary(key => key, key => current[key], StringComparer.OrdinalIgnoreCase);
        foreach (var path in current.Keys.Intersect(previous.Keys, StringComparer.OrdinalIgnoreCase))
            if (current[path].Sha256 != previous[path].Sha256 || current[path].Length != previous[path].Length) changes.Add(new SnapshotChange("modified", path));
        foreach (var addedPair in added)
        {
            var rename = removed.FirstOrDefault(item => item.Value.Sha256 == addedPair.Value.Sha256);
            if (!string.IsNullOrEmpty(rename.Key)) { changes.Add(new SnapshotChange("renamed", addedPair.Key, rename.Key)); removed.Remove(rename.Key); }
            else changes.Add(new SnapshotChange("added", addedPair.Key));
        }
        changes.AddRange(removed.Keys.Select(path => new SnapshotChange("deleted", path)));
        return changes;
    }
}

public sealed class DebouncedEventQueue
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeSpan _window;
    public DebouncedEventQueue(TimeSpan? window = null) => _window = window ?? TimeSpan.FromMilliseconds(100);
    public int Count => _pending.Count;
    public void Enqueue(string path) => _pending[FileScanner.Normalize(path)] = DateTimeOffset.UtcNow;
    public IReadOnlyList<string> DrainReady()
    {
        var threshold = DateTimeOffset.UtcNow - _window;
        var ready = new List<string>();
        foreach (var item in _pending)
            if (item.Value <= threshold && _pending.TryRemove(item.Key, out _)) ready.Add(item.Key);
        return ready;
    }
    public IReadOnlyList<string> DrainAll()
    {
        var ready = _pending.Keys.ToArray();
        foreach (var path in ready) _pending.TryRemove(path, out _);
        return ready;
    }
}

public sealed class FileChangeWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    public DebouncedEventQueue Queue { get; }
    public FileChangeWatcher(string root, DebouncedEventQueue? queue = null)
    {
        Queue = queue ?? new DebouncedEventQueue();
        _watcher = new FileSystemWatcher(root) { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size, Filter = "*" };
        _watcher.Created += (_, args) => QueueIfTarget(args.FullPath);
        _watcher.Changed += (_, args) => QueueIfTarget(args.FullPath);
        _watcher.Deleted += (_, args) => QueueIfTarget(args.FullPath);
        _watcher.Renamed += (_, args) => { QueueIfTarget(args.OldFullPath); QueueIfTarget(args.FullPath); };
    }
    public void Start() => _watcher.EnableRaisingEvents = true;
    public void Stop() => _watcher.EnableRaisingEvents = false;
    private void QueueIfTarget(string path) { if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || path.Contains(".adm-meta", StringComparison.OrdinalIgnoreCase)) Queue.Enqueue(path); }
    public void Dispose() { _watcher.Dispose(); }
}
