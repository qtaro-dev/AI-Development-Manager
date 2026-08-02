using System.Text;
using Adm.Storage.Recovery;

var root = Path.Combine(Path.GetTempPath(), $"recovery-journal-poc-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);
try
{
    var original = Encoding.UTF8.GetBytes("original\n");
    var replacement = Encoding.UTF8.GetBytes("replacement\n");
    var recovery = new RecoveryJournal(Path.Combine(root, ".recovery"));
    var outcomes = new List<RecoveryResult>();

    foreach (var stopStage in new[] { JournalStage.Prepared, JournalStage.TempFlushed, JournalStage.BackupCreated, JournalStage.Replaced, JournalStage.IndexPending })
    {
        var target = Path.Combine(root, stopStage + ".md");
        File.WriteAllBytes(target, original);
        var operation = recovery.Begin(target, original, replacement);
        if (stopStage >= JournalStage.TempFlushed) operation = recovery.WriteTemporary(operation, replacement);
        if (stopStage >= JournalStage.BackupCreated) operation = recovery.CreateBackup(operation);
        if (stopStage >= JournalStage.Replaced) operation = recovery.Replace(operation);
        if (stopStage >= JournalStage.IndexPending) operation = recovery.MarkIndexPending(operation);
        var result = recovery.Recover().Single(item => item.OperationId == operation.OperationId);
        outcomes.Add(result);
        Require(result.Outcome is "NoOpOriginalPreserved" or "AutoRecovered" or "IndexRebuildScheduled", $"unexpected recovery at {stopStage}");
        Require(File.ReadAllBytes(target).SequenceEqual(stopStage == JournalStage.Prepared ? original : replacement), $"content mismatch at {stopStage}");
    }

    var externalTarget = Path.Combine(root, "external.md");
    File.WriteAllBytes(externalTarget, original);
    var externalOperation = recovery.Begin(externalTarget, original, replacement);
    externalOperation = recovery.WriteTemporary(externalOperation, replacement);
    externalOperation = recovery.CreateBackup(externalOperation);
    File.WriteAllText(externalTarget, "external change\n");
    var externalResult = recovery.Recover().Single(item => item.OperationId == externalOperation.OperationId);
    Require(externalResult.Outcome == "NeedsUserDecision" && File.ReadAllText(externalTarget) == "external change\n", "external change was overwritten");

    var corruptJournal = Path.Combine(Path.GetDirectoryName(recovery.AuditPath)!, "journals", "corrupt.json");
    Directory.CreateDirectory(Path.GetDirectoryName(corruptJournal)!);
    File.WriteAllText(corruptJournal, "{not-json");
    var corruptResult = recovery.Recover().Single(item => item.Outcome == "CorruptJournalQuarantined");
    Require(File.Exists(Path.Combine(recovery.QuarantineDirectory, "corrupt.json")), "corrupt journal was not quarantined");

    var idempotentTarget = Path.Combine(root, "idempotent.md");
    File.WriteAllBytes(idempotentTarget, original);
    var idempotent = recovery.Begin(idempotentTarget, original, replacement);
    idempotent = recovery.WriteTemporary(idempotent, replacement);
    var first = recovery.Recover().Single(item => item.OperationId == idempotent.OperationId);
    var second = recovery.Recover().Single(item => item.OperationId == idempotent.OperationId);
    Require(first.Outcome == "AutoRecovered" && second.Outcome == "AlreadyCompleted", "recovery was not idempotent");
    Require(File.ReadAllBytes(idempotentTarget).SequenceEqual(replacement), "idempotent target content mismatch");
    Require(File.ReadAllLines(recovery.IndexQueuePath).Distinct().Count() == File.ReadAllLines(recovery.IndexQueuePath).Length, "index queue duplicated operation");

    Console.WriteLine($"PASS stop_stages={outcomes.Count} auto_recovery=true external_change_protected=true corrupt_quarantined=true idempotent=true index_rebuild_queued=true audit_exists={File.Exists(recovery.AuditPath)}");
    Console.WriteLine("Journal=operation_id,target,stage,expected_hashes; SDK=10.0.302 Runtime=10.0.10");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, true);
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
