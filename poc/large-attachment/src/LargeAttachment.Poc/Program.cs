using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

const long MiB = 1024L * 1024L;
const long MaxFileBytes = 500 * MiB;
const long MaxBatchBytes = 1024 * MiB;
const int ChunkBytes = 64 * 1024;
var runId = $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}";
var output = Path.Combine(Path.GetTempPath(), "AI-Development-Manager", "poc", "P0-019", runId);
var tempRoot = Path.Combine(output, "temp");
var finalRoot = Path.Combine(output, "final");
Directory.CreateDirectory(tempRoot); Directory.CreateDirectory(finalRoot);
var policy = new AttachmentPolicy(MaxFileBytes, MaxBatchBytes, TimeSpan.FromHours(24), 10 * MiB, 100_000);
var uploader = new AttachmentUploader(policy, tempRoot, finalRoot);
var checks = new List<CheckResult>();

checks.Add(Check("exact_500_mib_boundary_allowed", policy.ValidateFileSize(MaxFileBytes).Allowed, "500 MiB is allowed"));
checks.Add(Check("over_500_mib_rejected", !policy.ValidateFileSize(MaxFileBytes + 1).Allowed, "over file limit rejected"));
checks.Add(Check("exact_1_gib_batch_boundary_allowed", policy.ValidateBatchSize(MaxBatchBytes).Allowed, "1 GiB batch is allowed"));
checks.Add(Check("over_1_gib_batch_rejected", !policy.ValidateBatchSize(MaxBatchBytes + 1).Allowed, "over batch limit rejected"));
checks.Add(Check("unsafe_name_rejected", !policy.ValidateName("..\\outside.exe").Allowed && !policy.ValidateName("report.txt:secret").Allowed, "path traversal and ADS rejected"));

var progress = new List<UploadProgress>();
var normal = await uploader.UploadAsync(new UploadRequest("upload-001", "sample.log", 4 * MiB, "text/plain", () => new LogicalPayloadStream(4 * MiB)), progress.Add, CancellationToken.None);
checks.Add(Check("streaming_upload_commits", normal.Status == UploadStatus.Completed && File.Exists(normal.FinalPath), "small streamed upload committed"));
checks.Add(Check("progress_reaches_100", progress.Count > 0 && progress[^1].Percent == 100, $"events={progress.Count}"));

var cancelled = new CancellationTokenSource();
var cancelProgress = new List<UploadProgress>();
var cancelTask = uploader.UploadAsync(new UploadRequest("upload-cancel", "cancel.bin", 32 * MiB, "application/octet-stream", () => new DelayedLogicalPayloadStream(32 * MiB)), cancelProgress.Add, cancelled.Token);
await Task.Delay(25); cancelled.Cancel();
var cancelledResult = await cancelTask;
checks.Add(Check("cancel_does_not_commit", cancelledResult.Status == UploadStatus.Cancelled && !File.Exists(cancelledResult.FinalPath) && !Directory.EnumerateFiles(tempRoot).Any(path => path.Contains("upload-cancel", StringComparison.Ordinal)), "temporary data removed"));

var interrupted = await uploader.UploadAsync(new UploadRequest("upload-retry", "retry.bin", 8 * MiB, "application/octet-stream", () => new InterruptingPayloadStream(8 * MiB, 2 * MiB)), null, CancellationToken.None);
var retried = await uploader.UploadAsync(new UploadRequest("upload-retry", "retry.bin", 8 * MiB, "application/octet-stream", () => new LogicalPayloadStream(8 * MiB)), null, CancellationToken.None);
checks.Add(Check("communication_failure_is_retryable", interrupted.Status == UploadStatus.Failed && interrupted.Retryable && retried.Status == UploadStatus.Completed, "failed temporary upload can retry"));
checks.Add(Check("retry_has_single_final_file", Directory.EnumerateFiles(finalRoot, "retry.bin", SearchOption.AllDirectories).Count() == 1, "no duplicate attachment"));

var batch = new BatchUploadCoordinator(policy, uploader);
var concurrent = await Task.WhenAll(Enumerable.Range(0, 3).Select(index => batch.UploadAsync(new UploadRequest($"concurrent-{index}", $"file-{index}.bin", 2 * MiB, "application/octet-stream", () => new LogicalPayloadStream(2 * MiB)))));
checks.Add(Check("concurrent_uploads_complete", concurrent.All(result => result.Status == UploadStatus.Completed), "3 concurrent uploads completed"));
checks.Add(Check("batch_total_is_tracked", batch.AcceptedBytes == 6 * MiB, $"accepted={batch.AcceptedBytes}"));

var stale = Path.Combine(tempRoot, "stale.uploading"); File.WriteAllBytes(stale, [1, 2, 3]); File.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddHours(-25));
var cleaned = uploader.CleanupExpiredTemps();
checks.Add(Check("stale_temp_cleanup", cleaned == 1 && !File.Exists(stale), "expired temporary file removed"));

var largeMeasure = await MeasureStreamingMemory(uploader);
checks.Add(Check("memory_is_chunk_bounded", largeMeasure.PeakDeltaBytes < 32 * MiB, $"peak_delta={largeMeasure.PeakDeltaBytes} chunk={ChunkBytes}"));

var range = RangeDownload.Read(new byte[1024], 100, 200);
checks.Add(Check("range_download_returns_206", range.StatusCode == 206 && range.ContentRange == "bytes 100-299/1024" && range.Length == 200, "partial content"));
checks.Add(Check("range_out_of_bounds_rejected", RangeDownload.Read(new byte[10], 9, 2).StatusCode == 416, "invalid range rejected"));
checks.Add(Check("preview_policy_by_content_type", PreviewPolicy.Mode("application/pdf") == "inline" && PreviewPolicy.Mode("video/mp4") == "inline" && PreviewPolicy.Mode("application/x-msdownload") == "download_only", "PDF/video inline, executable download only"));
var logPreview = PreviewPolicy.LimitLog(string.Join('\n', Enumerable.Repeat("line", 100_005)));
checks.Add(Check("log_preview_is_limited", logPreview.Lines == 100_000 && logPreview.Truncated, "line limit applied"));

var result = new { run_id = runId, sdk = "10.0.302", runtime = Environment.Version.ToString(), policy = new { max_file_bytes = MaxFileBytes, max_batch_bytes = MaxBatchBytes, chunk_bytes = ChunkBytes, temp_retention_hours = 24, log_preview_bytes = policy.MaxLogBytes, log_preview_lines = policy.MaxLogLines }, checks, memory = largeMeasure, final_file_count = Directory.EnumerateFiles(finalRoot, "*", SearchOption.AllDirectories).Count(), output_directory = output, completed_utc = DateTimeOffset.UtcNow };
await File.WriteAllTextAsync(Path.Combine(output, "result.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"P0-019 run={runId} output={output}");
foreach (var check in checks) Console.WriteLine($"{(check.Passed ? "PASS" : "FAIL")} {check.Name}: {check.Detail}");
Console.WriteLine($"RESULT_JSON={Path.Combine(output, "result.json")}");
Environment.ExitCode = checks.All(check => check.Passed) ? 0 : 1;

static CheckResult Check(string name, bool passed, string detail) => new(name, passed, detail);

async Task<MemoryMeasure> MeasureStreamingMemory(AttachmentUploader service)
{
    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
    var before = GC.GetTotalMemory(true); var process = Process.GetCurrentProcess(); process.Refresh(); var workingBefore = process.WorkingSet64;
    var measured = await service.UploadAsync(new UploadRequest("memory-check", "memory-check.bin", 64 * MiB, "application/octet-stream", () => new LogicalPayloadStream(64 * MiB)), null, CancellationToken.None);
    process.Refresh(); var after = GC.GetTotalMemory(false); var workingAfter = process.WorkingSet64;
    return new MemoryMeasure(64 * MiB, Math.Max(after - before, workingAfter - workingBefore), ChunkBytes, measured.Status == UploadStatus.Completed);
}

record CheckResult(string Name, bool Passed, string Detail);
record MemoryMeasure(long LogicalBytes, long PeakDeltaBytes, int ChunkBytes, bool Completed);
record UploadRequest(string Id, string FileName, long Length, string ContentType, Func<Stream> OpenStream);
record UploadProgress(string Id, long BytesSent, long TotalBytes, int Percent);
record UploadResult(string Id, UploadStatus Status, string FinalPath, bool Retryable, string? Reason);
enum UploadStatus { Completed, Cancelled, Failed, Rejected }
record ValidationResult(bool Allowed, string? Reason);

sealed class AttachmentPolicy
{
    public AttachmentPolicy(long maxFileBytes, long maxBatchBytes, TimeSpan tempRetention, long maxLogBytes, int maxLogLines) { MaxFileBytes = maxFileBytes; MaxBatchBytes = maxBatchBytes; TempRetention = tempRetention; MaxLogBytes = maxLogBytes; MaxLogLines = maxLogLines; }
    public long MaxFileBytes { get; } public long MaxBatchBytes { get; } public TimeSpan TempRetention { get; } public long MaxLogBytes { get; } public int MaxLogLines { get; }
    public ValidationResult ValidateFileSize(long length) => length >= 0 && length <= MaxFileBytes ? new(true, null) : new(false, "file_size_limit");
    public ValidationResult ValidateBatchSize(long length) => length >= 0 && length <= MaxBatchBytes ? new(true, null) : new(false, "batch_size_limit");
    public ValidationResult ValidateName(string name) => string.IsNullOrWhiteSpace(name) || name.Contains('\\') || name.Contains('/') || name.Contains(':') || name is "." or ".." ? new(false, "unsafe_file_name") : new(true, null);
}

sealed class BatchUploadCoordinator
{
    private readonly AttachmentPolicy policy; private readonly AttachmentUploader uploader; private long accepted;
    public BatchUploadCoordinator(AttachmentPolicy policy, AttachmentUploader uploader) { this.policy = policy; this.uploader = uploader; }
    public long AcceptedBytes => Interlocked.Read(ref accepted);
    public async Task<UploadResult> UploadAsync(UploadRequest request)
    {
        var total = Interlocked.Add(ref accepted, request.Length);
        if (!policy.ValidateBatchSize(total).Allowed) { Interlocked.Add(ref accepted, -request.Length); return new(request.Id, UploadStatus.Rejected, "", false, "batch_size_limit"); }
        return await uploader.UploadAsync(request, null, CancellationToken.None);
    }
}

sealed class AttachmentUploader
{
    private const int UploadChunkBytes = 64 * 1024;
    private readonly AttachmentPolicy policy; private readonly string tempRoot; private readonly string finalRoot;
    public AttachmentUploader(AttachmentPolicy policy, string tempRoot, string finalRoot) { this.policy = policy; this.tempRoot = tempRoot; this.finalRoot = finalRoot; }
    public async Task<UploadResult> UploadAsync(UploadRequest request, Action<UploadProgress>? progress, CancellationToken cancellationToken)
    {
        var name = policy.ValidateName(request.FileName); var size = policy.ValidateFileSize(request.Length);
        if (!name.Allowed) return new(request.Id, UploadStatus.Rejected, "", false, name.Reason);
        if (!size.Allowed) return new(request.Id, UploadStatus.Rejected, "", false, size.Reason);
        var tempPath = Path.Combine(tempRoot, $"{request.Id}-{Guid.NewGuid():N}.uploading");
        var finalPath = Path.Combine(finalRoot, request.FileName);
        try
        {
            await using var source = request.OpenStream();
            await using var target = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, UploadChunkBytes, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var buffer = new byte[UploadChunkBytes]; long sent = 0; int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken); sent += read;
                progress?.Invoke(new UploadProgress(request.Id, sent, request.Length, (int)Math.Min(100, sent * 100 / request.Length)));
            }
            if (sent != request.Length) throw new IOException("content_length_mismatch");
            await target.FlushAsync(cancellationToken);
            await target.DisposeAsync();
            File.Move(tempPath, finalPath, true);
            progress?.Invoke(new UploadProgress(request.Id, sent, request.Length, 100));
            return new(request.Id, UploadStatus.Completed, finalPath, false, null);
        }
        catch (OperationCanceledException) { TryDelete(tempPath); return new(request.Id, UploadStatus.Cancelled, finalPath, true, "cancelled"); }
        catch (Exception exception) { TryDelete(tempPath); return new(request.Id, UploadStatus.Failed, finalPath, true, exception.Message); }
    }
    public int CleanupExpiredTemps()
    {
        var threshold = DateTime.UtcNow - policy.TempRetention; var count = 0;
        foreach (var path in Directory.EnumerateFiles(tempRoot, "*.uploading")) if (File.GetLastWriteTimeUtc(path) < threshold) { TryDelete(path); count++; }
        return count;
    }
    private static void TryDelete(string path) { if (File.Exists(path)) File.Delete(path); }
}

class LogicalPayloadStream : Stream
{
    private readonly long length; private long position; public LogicalPayloadStream(long length) => this.length = length;
    public override bool CanRead => true; public override bool CanSeek => true; public override bool CanWrite => false; public override long Length => length; public override long Position { get => position; set => position = value; }
    public override int Read(byte[] buffer, int offset, int count) { var read = (int)Math.Min(count, length - position); if (read <= 0) return 0; Array.Clear(buffer, offset, read); position += read; return read; }
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => new(Read(buffer.Span));
    public override long Seek(long offset, SeekOrigin origin) { position = origin switch { SeekOrigin.Begin => offset, SeekOrigin.Current => position + offset, _ => length + offset }; return position; }
    public override void SetLength(long value) => throw new NotSupportedException(); public override void Flush() { } public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

sealed class DelayedLogicalPayloadStream : LogicalPayloadStream { public DelayedLogicalPayloadStream(long length) : base(length) { } public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) { await Task.Delay(2, cancellationToken); return await base.ReadAsync(buffer, cancellationToken); } }
sealed class InterruptingPayloadStream : LogicalPayloadStream { private readonly long failAfter; public InterruptingPayloadStream(long length, long failAfter) : base(length) => this.failAfter = failAfter; public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) { if (Position >= failAfter) throw new IOException("simulated_connection_reset"); return base.ReadAsync(buffer, cancellationToken); } }

static class RangeDownload
{
    public static RangeResult Read(byte[] data, long start, long length) { if (start < 0 || length <= 0 || start >= data.Length || start + length > data.Length) return new(416, "", 0); return new(206, $"bytes {start}-{start + length - 1}/{data.Length}", length); }
}
record RangeResult(int StatusCode, string ContentRange, long Length);

static class PreviewPolicy
{
    public static string Mode(string contentType) => contentType switch { "application/pdf" or "image/png" or "image/jpeg" or "video/mp4" or "video/webm" => "inline", _ => "download_only" };
    public static LogPreview LimitLog(string content) { var lines = content.Split('\n'); var truncated = lines.Length > 100_000; return new(string.Join('\n', lines.Take(100_000)), Math.Min(lines.Length, 100_000), truncated); }
}
record LogPreview(string Content, int Lines, bool Truncated);
