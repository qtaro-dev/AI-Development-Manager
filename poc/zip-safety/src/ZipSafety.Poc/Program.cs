using System.IO.Compression;
using System.Text;
using Adm.Attachments.ArchiveInspection;

var root = Path.Combine(Path.GetTempPath(), $"zip-safety-poc-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);
try
{
    var normal = CreateZip(Path.Combine(root, "normal.zip"), ("docs/readme.txt", "hello"), ("bin/run.exe", "not executed"));
    var inspector = new ZipSafetyInspector();
    var normalResult = inspector.Inspect(normal);
    Require(normalResult.Allowed && normalResult.Entries.Count == 2, "normal ZIP rejected");
    Require(normalResult.Entries.Single(item => item.Name.EndsWith("run.exe", StringComparison.Ordinal)).Disposition == "download_only", "executable was viewable");
    var viewRoot = Path.Combine(root, "views");
    var view = inspector.ExtractForTemporaryView(normal, "docs/readme.txt", viewRoot);
    Require(File.ReadAllText(view.Path) == "hello" && view.ExpiresUtc > DateTimeOffset.UtcNow, "temporary view failed");

    var slip = CreateZip(Path.Combine(root, "slip.zip"), ("../outside.txt", "bad"));
    Require(inspector.Inspect(slip).Reason == ZipRejectReason.ZipSlip, "Zip Slip accepted");
    var absolute = CreateZip(Path.Combine(root, "absolute.zip"), ("/outside.txt", "bad"));
    Require(inspector.Inspect(absolute).Reason == ZipRejectReason.ZipSlip, "absolute entry accepted");
    var duplicate = CreateZip(Path.Combine(root, "duplicate.zip"), ("same.txt", "one"), ("same.txt", "two"));
    Require(inspector.Inspect(duplicate).Reason == ZipRejectReason.DuplicateName, "duplicate entry accepted");
    var deep = CreateZip(Path.Combine(root, "deep.zip"), ("a/b/c/file.txt", "deep"));
    Require(inspector.Inspect(deep).Reason == ZipRejectReason.TooDeep, "deep entry accepted");

    var lowLimits = new ZipInspectionLimits(MaxEntries: 2, MaxEntryExpandedBytes: 10, MaxTotalExpandedBytes: 15, MaxCompressionRatio: 2, MaxNestingDepth: 2, TemporaryRetention: TimeSpan.FromHours(1));
    var limited = new ZipSafetyInspector(lowLimits);
    var tooMany = CreateZip(Path.Combine(root, "many.zip"), ("1.txt", "1"), ("2.txt", "2"), ("3.txt", "3"));
    Require(limited.Inspect(tooMany).Reason == ZipRejectReason.TooManyEntries, "entry count limit failed");
    var tooLarge = CreateZip(Path.Combine(root, "large.zip"), ("large.txt", new string('x', 20)));
    Require(limited.Inspect(tooLarge).Reason is ZipRejectReason.EntryTooLarge or ZipRejectReason.CompressionBomb, "entry size limit failed");
    var bomb = CreateZip(Path.Combine(root, "bomb.zip"), ("bomb.txt", new string('0', 10000)));
    var bombInspector = new ZipSafetyInspector(new ZipInspectionLimits(MaxEntryExpandedBytes: 20000, MaxTotalExpandedBytes: 30000, MaxCompressionRatio: 2, MaxNestingDepth: 2));
    Require(bombInspector.Inspect(bomb).Reason == ZipRejectReason.CompressionBomb, "compression ratio limit failed");

    var encrypted = Path.Combine(root, "encrypted.zip");
    File.WriteAllBytes(encrypted, [0x50, 0x4b, 0x03, 0x04, 20, 0, 1, 0, 0, 0, 0, 0]);
    Require(inspector.Inspect(encrypted).Reason == ZipRejectReason.Encrypted, "encrypted ZIP accepted");
    var corrupt = Path.Combine(root, "corrupt.zip");
    File.WriteAllBytes(corrupt, Encoding.ASCII.GetBytes("not a zip"));
    Require(inspector.Inspect(corrupt).Reason == ZipRejectReason.Corrupt, "corrupt ZIP accepted");
    var cleanupMarker = Path.Combine(viewRoot, Path.GetFileName(Path.GetDirectoryName(view.Path)!), ".expires");
    File.WriteAllText(cleanupMarker, DateTimeOffset.UtcNow.AddHours(-1).ToString("O"));
    Require(ZipSafetyInspector.CleanupExpiredViews(viewRoot, DateTimeOffset.UtcNow) == 1, "expired view was not cleaned");
    Console.WriteLine("PASS normal=true zip_slip=false absolute=false duplicate=false depth=false entry_limit=false size_limit=false compression_limit=false encrypted=false corrupt=false temp_view=true executable_download_only=true cleanup=true");
    Console.WriteLine("Limits=500MiB/10000/250MiB/2GiB/100x/depth2/24h; SDK=10.0.302 Runtime=10.0.10");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, true);
}

static string CreateZip(string path, params (string Name, string Content)[] entries)
{
    using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
    using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
    {
        foreach (var item in entries)
        {
            var entry = archive.CreateEntry(item.Name, CompressionLevel.Optimal);
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write(item.Content);
        }
    }
    return path;
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
