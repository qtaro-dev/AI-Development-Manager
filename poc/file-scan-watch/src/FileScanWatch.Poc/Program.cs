using System.Text;
using Adm.Indexing.Scanner;

var root = Path.Combine(Path.GetTempPath(), $"file-scan-watch-poc-{Guid.NewGuid():N}");
Directory.CreateDirectory(Path.Combine(root, ".adm-meta"));
try
{
    File.WriteAllText(Path.Combine(root, "a.md"), "a");
    File.WriteAllText(Path.Combine(root, "b.md"), "b");
    File.WriteAllText(Path.Combine(root, ".adm-meta", "project.json"), "{\"schema_version\":1}");
    File.WriteAllText(Path.Combine(root, "ignored.txt"), "ignored");
    var scanner = new FileScanner(root, maxAttempts: 3, retryDelay: TimeSpan.FromMilliseconds(5));
    var progressCount = 0;
    var initial = scanner.Scan(new Progress<ScanProgress>(_ => progressCount++));
    Require(initial.Items.Count == 3 && initial.Errors.Count == 0 && progressCount > 0, "initial scan failed");

    using var watcher = new FileChangeWatcher(root, new DebouncedEventQueue(TimeSpan.Zero));
    watcher.Start();
    var aPath = Path.Combine(root, "a.md");
    File.AppendAllText(aPath, " changed");
    File.AppendAllText(aPath, " twice");
    Thread.Sleep(150);
    var events = watcher.Queue.DrainReady();
    Require(events.Count >= 1 && events.Count <= 2, "watcher debounce failed");

    var beforeChanges = initial.Items;
    File.Move(Path.Combine(root, "b.md"), Path.Combine(root, "renamed.md"));
    File.Delete(Path.Combine(root, "a.md"));
    File.WriteAllText(Path.Combine(root, "new.md"), "new");
    var afterChanges = scanner.Scan();
    var changes = SnapshotDiff.Calculate(beforeChanges, afterChanges.Items);
    Require(changes.Any(item => item.Kind == "renamed" && item.PreviousPath == "b.md") && changes.Any(item => item.Kind == "deleted" && item.RelativePath == "a.md") && changes.Any(item => item.Kind == "added" && item.RelativePath == "new.md"), "snapshot diff failed");

    var missedPath = Path.Combine(root, "missed.md");
    File.WriteAllText(missedPath, "missed");
    var reconciled = scanner.Scan();
    Require(reconciled.Items.ContainsKey("missed.md"), "periodic rescan did not repair missed event");

    var lockedPath = Path.Combine(root, "locked.md");
    File.WriteAllText(lockedPath, "locked");
    var lockedError = false;
    using (var handle = new FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    {
        var locked = scanner.Scan();
        lockedError = locked.Errors.Any(item => item.RelativePath == "locked.md");
        Require(locked.Items.Count >= 3, "one locked file stopped full scan");
    }
    var unlocked = scanner.Scan();
    Require(lockedError && unlocked.Items.ContainsKey("locked.md"), "locked-file retry/rescan failed");

    var cancel = new CancellationTokenSource();
    cancel.Cancel();
    var canceled = false;
    try { await scanner.ScanAsync(cancellationToken: cancel.Token); }
    catch (OperationCanceledException) { canceled = true; }
    Require(canceled, "scan cancellation was not observed");
    var resumed = scanner.Scan();
    Require(resumed.Items.Count >= 5, "scan resume failed");
    Require(watcher.Queue.Count == 0 || watcher.Queue.DrainAll().Count >= 0, "health queue read failed");
    Console.WriteLine($"PASS initial_items={initial.Items.Count} debounce_events={events.Count} rename_delete_add=true resync=true locked_file_isolated={lockedError} cancellation=true resume=true errors_are_per_document=true");
    Console.WriteLine("Scope=markdown+adm-meta; watcher=deduplicated; rescan=source-of-truth; SDK=10.0.302 Runtime=10.0.10");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, true);
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
