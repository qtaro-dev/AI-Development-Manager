using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Adm.Storage.AtomicFileWriter;

namespace Adm.Api.Concurrency;

public sealed record DocumentSnapshot(string Path, string ETag, byte[] Content);

public sealed record ConflictResponse(
    int StatusCode,
    string TrackingId,
    string LatestETag,
    string LatestContent,
    string SubmittedContent,
    string DiffEndpoint);

public sealed record UpdateResponse(int StatusCode, string ETag, bool NoOp, ConflictResponse? Conflict = null);

public sealed class ConcurrencyStore
{
    private readonly AtomicFileWriter _writer = new();
    private readonly string _backupDirectory;

    public ConcurrencyStore(string backupDirectory) => _backupDirectory = Path.GetFullPath(backupDirectory);

    public DocumentSnapshot Read(string path)
    {
        var content = File.ReadAllBytes(path);
        return new DocumentSnapshot(path, CreateETag(content), content);
    }

    public UpdateResponse Update(string path, string? ifMatch, ReadOnlySpan<byte> submittedContent)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
            return new UpdateResponse(428, string.Empty, false);

        var current = Read(path);
        if (!string.Equals(ifMatch, current.ETag, StringComparison.Ordinal))
        {
            if (current.Content.AsSpan().SequenceEqual(submittedContent))
                return new UpdateResponse(200, current.ETag, true);

            var conflict = new ConflictResponse(
                409,
                Guid.NewGuid().ToString("N"),
                current.ETag,
                Encoding.UTF8.GetString(current.Content),
                Encoding.UTF8.GetString(submittedContent),
                $"/api/documents/{Uri.EscapeDataString(Path.GetFileName(path))}/diff?from={Uri.EscapeDataString(ifMatch)}&to={Uri.EscapeDataString(current.ETag)}");
            return new UpdateResponse(409, current.ETag, false, conflict);
        }

        if (current.Content.AsSpan().SequenceEqual(submittedContent))
            return new UpdateResponse(200, current.ETag, true);

        var result = _writer.Save(path, submittedContent, _backupDirectory);
        if (!result.Succeeded) throw new IOException(result.ErrorMessage);
        var updated = Read(path);
        return new UpdateResponse(200, updated.ETag, false);
    }

    public static string CreateETag(ReadOnlySpan<byte> content)
    {
        var digest = SHA256.HashData(content);
        return $"\"sha256-{Convert.ToBase64String(digest).TrimEnd('=').Replace('+', '-').Replace('/', '_')}\"";
    }

    public static void WriteConflictAudit(string path, ConflictResponse conflict)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, JsonSerializer.Serialize(conflict) + Environment.NewLine);
    }
}
