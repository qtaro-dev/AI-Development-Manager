using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adm.Storage.Recovery;

public enum JournalStage { Prepared, TempFlushed, BackupCreated, Replaced, IndexPending, Completed, NeedsUserDecision }

public sealed record JournalEntry(
    [property: JsonPropertyName("operation_id")] string OperationId,
    [property: JsonPropertyName("target_path")] string TargetPath,
    [property: JsonPropertyName("temporary_path")] string TemporaryPath,
    [property: JsonPropertyName("backup_path")] string BackupPath,
    [property: JsonPropertyName("expected_original_sha256")] string ExpectedOriginalSha256,
    [property: JsonPropertyName("expected_new_sha256")] string ExpectedNewSha256,
    [property: JsonPropertyName("stage")] JournalStage Stage,
    [property: JsonPropertyName("created_utc")] DateTimeOffset CreatedUtc,
    [property: JsonPropertyName("updated_utc")] DateTimeOffset UpdatedUtc);

public sealed record RecoveryResult(string OperationId, string Outcome, string Reason, JournalStage Stage);

public sealed class RecoveryJournal
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    private readonly string _root;
    private readonly string _journals;
    private readonly string _quarantine;
    private readonly string _auditPath;
    private readonly string _indexQueuePath;

    public RecoveryJournal(string root)
    {
        _root = Path.GetFullPath(root);
        _journals = Path.Combine(_root, "journals");
        _quarantine = Path.Combine(_root, "quarantine");
        _auditPath = Path.Combine(_root, "audit.jsonl");
        _indexQueuePath = Path.Combine(_root, "index-rebuild.queue");
        Directory.CreateDirectory(_journals);
    }

    public JournalEntry Begin(string targetPath, ReadOnlySpan<byte> originalContent, ReadOnlySpan<byte> newContent)
    {
        targetPath = Path.GetFullPath(targetPath);
        var operationId = Guid.NewGuid().ToString("N");
        var directory = Path.GetDirectoryName(targetPath)!;
        var temporary = Path.Combine(directory, ".adm-recovery-tmp-" + operationId + ".tmp");
        var backup = Path.Combine(directory, ".adm-recovery-backup-" + operationId + ".bak");
        var now = DateTimeOffset.UtcNow;
        var entry = new JournalEntry(operationId, targetPath, temporary, backup, Sha256(originalContent), Sha256(newContent), JournalStage.Prepared, now, now);
        Write(entry);
        return entry;
    }

    public JournalEntry WriteTemporary(JournalEntry entry, ReadOnlySpan<byte> newContent)
    {
        WriteBytes(entry.TemporaryPath, newContent);
        return Advance(entry, JournalStage.TempFlushed);
    }

    public JournalEntry CreateBackup(JournalEntry entry)
    {
        File.Copy(entry.TargetPath, entry.BackupPath, false);
        return Advance(entry, JournalStage.BackupCreated);
    }

    public JournalEntry Replace(JournalEntry entry)
    {
        File.Move(entry.TemporaryPath, entry.TargetPath, true);
        return Advance(entry, JournalStage.Replaced);
    }

    public JournalEntry MarkIndexPending(JournalEntry entry) => Advance(entry, JournalStage.IndexPending);

    public JournalEntry Complete(JournalEntry entry)
    {
        AppendIndexQueue(entry);
        return Advance(entry, JournalStage.Completed);
    }

    public IReadOnlyList<RecoveryResult> Recover()
    {
        var results = new List<RecoveryResult>();
        foreach (var journalPath in Directory.EnumerateFiles(_journals, "*.json"))
        {
            JournalEntry entry;
            try { entry = JsonSerializer.Deserialize<JournalEntry>(File.ReadAllText(journalPath), Options) ?? throw new InvalidDataException(); }
            catch (Exception exception) when (exception is JsonException or InvalidDataException or NotSupportedException)
            {
                Directory.CreateDirectory(_quarantine);
                var destination = Path.Combine(_quarantine, Path.GetFileName(journalPath));
                File.Move(journalPath, destination, true);
                results.Add(new RecoveryResult(Path.GetFileNameWithoutExtension(journalPath), "CorruptJournalQuarantined", exception.GetType().Name, JournalStage.NeedsUserDecision));
                continue;
            }

            if (entry.Stage == JournalStage.Completed)
            {
                results.Add(new RecoveryResult(entry.OperationId, "AlreadyCompleted", "journal already completed", entry.Stage));
                continue;
            }

            var currentHash = File.Exists(entry.TargetPath) ? Sha256(File.ReadAllBytes(entry.TargetPath)) : string.Empty;
            var newContentPresent = currentHash == entry.ExpectedNewSha256;
            var originalPresent = currentHash == entry.ExpectedOriginalSha256;
            if (newContentPresent)
            {
                entry = Advance(entry, JournalStage.IndexPending);
                entry = Complete(entry);
                results.Add(new RecoveryResult(entry.OperationId, "IndexRebuildScheduled", "target already contains expected new content", entry.Stage));
                continue;
            }

            if (!originalPresent)
            {
                entry = Advance(entry, JournalStage.NeedsUserDecision);
                results.Add(new RecoveryResult(entry.OperationId, "NeedsUserDecision", "target changed outside this operation; original was not modified", entry.Stage));
                continue;
            }

            if (entry.Stage == JournalStage.Prepared)
            {
                entry = Advance(entry, JournalStage.Completed);
                results.Add(new RecoveryResult(entry.OperationId, "NoOpOriginalPreserved", "stopped before temporary write", entry.Stage));
                continue;
            }

            if (!File.Exists(entry.TemporaryPath) || Sha256(File.ReadAllBytes(entry.TemporaryPath)) != entry.ExpectedNewSha256)
            {
                entry = Advance(entry, JournalStage.NeedsUserDecision);
                results.Add(new RecoveryResult(entry.OperationId, "NeedsUserDecision", "expected temporary content is unavailable", entry.Stage));
                continue;
            }

            if (!File.Exists(entry.BackupPath)) File.Copy(entry.TargetPath, entry.BackupPath, false);
            File.Move(entry.TemporaryPath, entry.TargetPath, true);
            entry = Advance(entry, JournalStage.Replaced);
            entry = Complete(entry);
            results.Add(new RecoveryResult(entry.OperationId, "AutoRecovered", "original matched and temporary content was verified", entry.Stage));
        }
        return results;
    }

    public int CleanupCompleted(TimeSpan olderThan)
    {
        var threshold = DateTimeOffset.UtcNow - olderThan;
        var removed = 0;
        foreach (var path in Directory.EnumerateFiles(_journals, "*.json"))
        {
            try
            {
                var entry = JsonSerializer.Deserialize<JournalEntry>(File.ReadAllText(path), Options);
                if (entry?.Stage == JournalStage.Completed && entry.UpdatedUtc < threshold) { File.Delete(path); removed++; }
            }
            catch (JsonException) { }
        }
        return removed;
    }

    public string AuditPath => _auditPath;
    public string QuarantineDirectory => _quarantine;
    public string IndexQueuePath => _indexQueuePath;

    private JournalEntry Advance(JournalEntry entry, JournalStage stage)
    {
        var updated = entry with { Stage = stage, UpdatedUtc = DateTimeOffset.UtcNow };
        Write(updated);
        File.AppendAllText(_auditPath, JsonSerializer.Serialize(new { updated.OperationId, Stage = stage.ToString(), AtUtc = updated.UpdatedUtc }) + Environment.NewLine);
        return updated;
    }

    private void Write(JournalEntry entry) => WriteAtomic(Path.Combine(_journals, entry.OperationId + ".json"), entry);

    private void AppendIndexQueue(JournalEntry entry) => File.AppendAllText(_indexQueuePath, entry.OperationId + Environment.NewLine);

    private static string Sha256(ReadOnlySpan<byte> content) => Convert.ToHexString(SHA256.HashData(content));

    private static void WriteBytes(string path, ReadOnlySpan<byte> content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
        stream.Write(content);
        stream.Flush(true);
    }

    private static void WriteAtomic<T>(string path, T value)
    {
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, value, Options);
                stream.Flush(true);
            }
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
