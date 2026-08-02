using System.IO.Compression;
using System.Text.Json.Serialization;

namespace Adm.Attachments.ArchiveInspection;

public sealed record ZipInspectionLimits(
    long MaxArchiveBytes = 500L * 1024 * 1024,
    int MaxEntries = 10_000,
    long MaxEntryExpandedBytes = 250L * 1024 * 1024,
    long MaxTotalExpandedBytes = 2L * 1024 * 1024 * 1024,
    double MaxCompressionRatio = 100.0,
    int MaxNestingDepth = 2,
    TimeSpan? TemporaryRetention = null);

public enum ZipRejectReason { None, TooLarge, TooManyEntries, ZipSlip, DuplicateName, TooDeep, EntryTooLarge, TotalTooLarge, CompressionBomb, Encrypted, Corrupt, ReadLimitExceeded }

public sealed record ZipEntryInfo(string Name, long CompressedBytes, long ExpandedBytes, int Depth, string Disposition);

public sealed record ZipInspectionResult(bool Allowed, ZipRejectReason Reason, string InternalDetail, string UserMessage, IReadOnlyList<ZipEntryInfo> Entries, long EstimatedExpandedBytes);

public sealed record TemporaryEntryView(string Path, DateTimeOffset ExpiresUtc, string Disposition);

public sealed class ZipSafetyInspector
{
    private static readonly byte[] LocalSignature = [0x50, 0x4b, 0x03, 0x04];
    private static readonly byte[] CentralSignature = [0x50, 0x4b, 0x01, 0x02];
    private readonly ZipInspectionLimits _limits;

    public ZipSafetyInspector(ZipInspectionLimits? limits = null) => _limits = limits ?? new();

    public ZipInspectionResult Inspect(string archivePath)
    {
        var entries = new List<ZipEntryInfo>();
        try
        {
            var length = new FileInfo(archivePath).Length;
            if (length > _limits.MaxArchiveBytes) return Reject(ZipRejectReason.TooLarge, "archive.too_large", entries, 0);
            if (HasEncryptedEntries(archivePath)) return Reject(ZipRejectReason.Encrypted, "archive.encrypted", entries, 0);
            using var stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long total = 0;
            foreach (var entry in archive.Entries)
            {
                if (entries.Count >= _limits.MaxEntries) return Reject(ZipRejectReason.TooManyEntries, "archive.too_many_entries", entries, total);
                var normalized = NormalizeEntryName(entry.FullName);
                if (normalized is null) return Reject(ZipRejectReason.ZipSlip, "entry.unsafe_path", entries, total);
                if (!names.Add(normalized)) return Reject(ZipRejectReason.DuplicateName, "entry.duplicate_name", entries, total);
                var depth = normalized.Split('/').Length - 1;
                if (depth > _limits.MaxNestingDepth) return Reject(ZipRejectReason.TooDeep, "entry.too_deep", entries, total);
                if (entry.Length < 0 || entry.Length > _limits.MaxEntryExpandedBytes) return Reject(ZipRejectReason.EntryTooLarge, "entry.too_large", entries, total);
                total = checked(total + entry.Length);
                if (total > _limits.MaxTotalExpandedBytes) return Reject(ZipRejectReason.TotalTooLarge, "archive.expanded_total_too_large", entries, total);
                if (entry.Length > 0 && (entry.CompressedLength <= 0 || entry.Length / (double)entry.CompressedLength > _limits.MaxCompressionRatio))
                    return Reject(ZipRejectReason.CompressionBomb, "entry.compression_ratio_too_high", entries, total);
                var disposition = IsExecutable(normalized) ? "download_only" : "view_or_download";
                entries.Add(new ZipEntryInfo(normalized, entry.CompressedLength, entry.Length, depth, disposition));
            }
            return new ZipInspectionResult(true, ZipRejectReason.None, "archive.allowed", "ZIPの内容を確認しました。", entries, total);
        }
        catch (InvalidDataException exception) { return Reject(ZipRejectReason.Corrupt, "archive.invalid", entries, 0, exception.GetType().Name); }
        catch (OverflowException exception) { return Reject(ZipRejectReason.TotalTooLarge, "archive.expanded_total_overflow", entries, long.MaxValue, exception.GetType().Name); }
        catch (IOException exception) { return Reject(ZipRejectReason.Corrupt, "archive.read_failed", entries, 0, exception.GetType().Name); }
    }

    public TemporaryEntryView ExtractForTemporaryView(string archivePath, string entryName, string temporaryRoot)
    {
        var inspection = Inspect(archivePath);
        if (!inspection.Allowed) throw new InvalidDataException(inspection.InternalDetail);
        var normalized = NormalizeEntryName(entryName) ?? throw new ArgumentException("Unsafe entry name.", nameof(entryName));
        var info = inspection.Entries.SingleOrDefault(item => item.Name == normalized) ?? throw new FileNotFoundException(entryName);
        var expires = DateTimeOffset.UtcNow + (_limits.TemporaryRetention ?? TimeSpan.FromHours(24));
        var directory = Path.Combine(temporaryRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var output = Path.Combine(directory, Path.GetFileName(normalized));
        using var stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = archive.Entries.Single(item => NormalizeEntryName(item.FullName) == normalized);
        using var input = entry.Open();
        using var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        input.CopyTo(file);
        file.Flush(true);
        return new TemporaryEntryView(output, expires, info.Disposition);
    }

    public static int CleanupExpiredViews(string root, DateTimeOffset now)
    {
        var removed = 0;
        if (!Directory.Exists(root)) return 0;
        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            var marker = Path.Combine(directory, ".expires");
            if (!File.Exists(marker) || DateTimeOffset.TryParse(File.ReadAllText(marker), out var expires) && expires <= now)
            {
                Directory.Delete(directory, true);
                removed++;
            }
        }
        return removed;
    }

    private static string? NormalizeEntryName(string name)
    {
        var normalized = name.Replace('\\', '/');
        if (normalized.StartsWith("/", StringComparison.Ordinal) || normalized.Contains(':', StringComparison.Ordinal)) return null;
        var segments = normalized.Split('/');
        if (segments.Any(segment => segment == "..")) return null;
        var combined = string.Join('/', segments.Where(segment => segment != "."));
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }

    private static bool IsExecutable(string name) => Path.GetExtension(name).ToLowerInvariant() is ".exe" or ".dll" or ".bat" or ".cmd" or ".ps1" or ".com";

    private static bool HasEncryptedEntries(string path)
    {
        var bytes = File.ReadAllBytes(path);
        for (var index = 0; index + 9 < bytes.Length; index++)
        {
            if (bytes.AsSpan(index, 4).SequenceEqual(LocalSignature) && (bytes[index + 6] & 1) != 0) return true;
            if (bytes.AsSpan(index, 4).SequenceEqual(CentralSignature) && (bytes[index + 8] & 1) != 0) return true;
        }
        return false;
    }

    private static ZipInspectionResult Reject(ZipRejectReason reason, string detail, IReadOnlyList<ZipEntryInfo> entries, long total, string? exception = null) => new(false, reason, exception is null ? detail : detail + ":" + exception, "このZIPは安全のため閲覧できません。", entries, total);
}
