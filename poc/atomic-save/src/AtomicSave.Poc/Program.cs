using System.Text;
using Adm.Storage.AtomicFileWriter;

var root = Path.Combine(Path.GetTempPath(), $"atomic-save-poc-{Guid.NewGuid():N}");
var target = Path.Combine(root, "docs", "sample.md");
var backups = Path.Combine(root, "backups");
Directory.CreateDirectory(Path.GetDirectoryName(target)!);
var writer = new AtomicFileWriter();
var original = Encoding.UTF8.GetBytes("# Original\n");
var replacement = Encoding.UTF8.GetBytes("# Replacement\n");
var results = new List<SaveResult>();

try
{
    File.WriteAllBytes(target, original);
    File.SetAttributes(target, FileAttributes.ReadOnly);
    var success = writer.Save(target, replacement, backups);
    results.Add(success);
    Require(success.Succeeded && File.ReadAllBytes(target).SequenceEqual(replacement), "normal save failed");
    Require(success.BackupPath is not null && File.ReadAllBytes(success.BackupPath).SequenceEqual(original), "backup mismatch");
    Require(File.GetAttributes(target) == FileAttributes.ReadOnly, "file attributes were not preserved");

    File.SetAttributes(target, FileAttributes.Normal);
    foreach (var stage in new[] { FailureStage.AccessDeniedEquivalent, FailureStage.DiskFullEquivalent, FailureStage.FileInUseEquivalent, FailureStage.AfterTempFlush, FailureStage.AfterBackupBeforeReplace })
    {
        File.WriteAllBytes(target, original);
        var result = writer.Save(target, replacement, backups, stage);
        results.Add(result);
        Require(!result.Succeeded, $"failure injection did not fail: {stage}");
        Require(File.ReadAllBytes(target).SequenceEqual(original), $"original was lost at {stage}");
        Require(result.TemporaryCleaned, $"temporary file remained at {stage}");
        if (stage == FailureStage.AfterBackupBeforeReplace)
            Require(result.BackupPath is not null && File.ReadAllBytes(result.BackupPath).SequenceEqual(original), "failure backup mismatch");
    }

    File.WriteAllBytes(target, original);
    using (var handle = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read))
    {
        var locked = writer.Save(target, replacement, backups);
        results.Add(locked);
        Require(!locked.Succeeded && File.ReadAllBytes(target).SequenceEqual(original), "locked-file failure was not safe");
    }

    var stale = Path.Combine(Path.GetDirectoryName(target)!, ".adm-tmp-sample-stale.tmp");
    var fresh = Path.Combine(Path.GetDirectoryName(target)!, ".adm-tmp-sample-fresh.tmp");
    File.WriteAllText(stale, "stale");
    File.WriteAllText(fresh, "fresh");
    File.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddHours(-2));
    var removed = AtomicFileWriter.CleanupOrphanedTemporaryFiles(Path.GetDirectoryName(target)!, TimeSpan.FromMinutes(30));
    Require(removed == 1 && !File.Exists(stale) && File.Exists(fresh), "temporary cleanup policy failed");

    var audit = Path.Combine(root, "audit", "save-results.jsonl");
    foreach (var result in results) AtomicFileWriter.WriteAuditRecord(audit, result);
    Require(File.ReadAllLines(audit).Length == results.Count, "audit record count mismatch");
    Console.WriteLine($"PASS scenarios={results.Count} normal_save=true backup_match=true failure_safe=true locked_file_safe=true temp_cleanup=true audit_records={results.Count}");
    Console.WriteLine("ACL=not_modified_by_poc; Windows ACL preservation requires product-level verification");
    Console.WriteLine("SDK=10.0.302 Runtime=10.0.10");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, true);
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
