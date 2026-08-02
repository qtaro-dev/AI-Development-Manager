using System.Text.Json;

namespace Adm.Storage.AtomicFileWriter;

public enum FailureStage
{
    None,
    AccessDeniedEquivalent,
    DiskFullEquivalent,
    FileInUseEquivalent,
    AfterTempFlush,
    AfterBackupBeforeReplace
}

public sealed record SaveResult(
    bool Succeeded,
    string TargetPath,
    string? BackupPath,
    string? FailureStage,
    string? ErrorMessage,
    FileAttributes OriginalAttributes,
    bool TemporaryCleaned,
    DateTimeOffset CompletedUtc);

public sealed class AtomicFileWriter
{
    private const string TempPrefix = ".adm-tmp-";

    public SaveResult Save(string targetPath, ReadOnlySpan<byte> content, string backupDirectory, FailureStage failureStage = FailureStage.None)
    {
        targetPath = Path.GetFullPath(targetPath);
        backupDirectory = Path.GetFullPath(backupDirectory);
        var directory = Path.GetDirectoryName(targetPath) ?? throw new ArgumentException("Target directory is required.", nameof(targetPath));
        Directory.CreateDirectory(directory);
        Directory.CreateDirectory(backupDirectory);

        var existed = File.Exists(targetPath);
        var attributes = existed ? File.GetAttributes(targetPath) : FileAttributes.Normal;
        var temporary = Path.Combine(directory, TempPrefix + Path.GetFileName(targetPath) + "-" + Guid.NewGuid().ToString("N") + ".tmp");
        string? backup = null;
        var attributesTemporarilyCleared = false;
        try
        {
            if (failureStage is FailureStage.AccessDeniedEquivalent or FailureStage.DiskFullEquivalent or FailureStage.FileInUseEquivalent)
                throw new IOException($"Injected failure: {failureStage}");

            WriteAndFlush(temporary, content);
            if (failureStage == FailureStage.AfterTempFlush)
                throw new IOException($"Injected failure: {failureStage}");

            if (existed)
            {
                backup = Path.Combine(backupDirectory, Path.GetFileName(targetPath) + "." + DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + ".bak");
                CopyAndFlush(targetPath, backup);
            }

            if (failureStage == FailureStage.AfterBackupBeforeReplace)
                throw new IOException($"Injected failure: {failureStage}");

            if (existed && attributes != FileAttributes.Normal)
            {
                File.SetAttributes(targetPath, FileAttributes.Normal);
                attributesTemporarilyCleared = true;
            }
            File.Move(temporary, targetPath, true);
            File.SetAttributes(targetPath, attributes);
            return new SaveResult(true, targetPath, backup, null, null, attributes, true, DateTimeOffset.UtcNow);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            if (File.Exists(temporary)) File.Delete(temporary);
            return new SaveResult(false, targetPath, backup, failureStage.ToString(), exception.Message, attributes, !File.Exists(temporary), DateTimeOffset.UtcNow);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
            if (attributesTemporarilyCleared && File.Exists(targetPath)) File.SetAttributes(targetPath, attributes);
        }
    }

    public static int CleanupOrphanedTemporaryFiles(string directory, TimeSpan olderThan)
    {
        var threshold = DateTime.UtcNow - olderThan;
        var removed = 0;
        foreach (var path in Directory.EnumerateFiles(directory, TempPrefix + "*", SearchOption.TopDirectoryOnly))
        {
            if (File.GetLastWriteTimeUtc(path) >= threshold) continue;
            File.Delete(path);
            removed++;
        }
        return removed;
    }

    public static void WriteAuditRecord(string path, SaveResult result)
    {
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        var line = JsonSerializer.Serialize(result);
        File.AppendAllText(path, line + Environment.NewLine);
    }

    private static void WriteAndFlush(string path, ReadOnlySpan<byte> content)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
        stream.Write(content);
        stream.Flush(true);
    }

    private static void CopyAndFlush(string source, string destination)
    {
        using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
        using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
        input.CopyTo(output);
        output.Flush(true);
    }
}
