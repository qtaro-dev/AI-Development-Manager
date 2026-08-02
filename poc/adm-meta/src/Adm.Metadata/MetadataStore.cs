using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adm.Metadata;

public sealed record ProjectMetadata(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("project_id")] string ProjectId,
    [property: JsonPropertyName("created_utc")] DateTimeOffset CreatedUtc,
    [property: JsonPropertyName("next_sequence_by_type")] Dictionary<string, int> NextSequenceByType);

public sealed record DocumentMetadata(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("document_id")] string DocumentId,
    [property: JsonPropertyName("document_type")] string DocumentType,
    [property: JsonPropertyName("sequence_number")] int SequenceNumber,
    [property: JsonPropertyName("relative_path")] string RelativePath,
    [property: JsonPropertyName("content_sha256")] string ContentSha256,
    [property: JsonPropertyName("created_utc")] DateTimeOffset CreatedUtc,
    [property: JsonPropertyName("updated_utc")] DateTimeOffset UpdatedUtc);

public sealed record UserDocumentState(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("document_id")] string DocumentId,
    [property: JsonPropertyName("confirmed")] bool Confirmed,
    [property: JsonPropertyName("classification_override")] string? ClassificationOverride,
    [property: JsonPropertyName("updated_utc")] DateTimeOffset UpdatedUtc);

public sealed record RenameCandidate(
    [property: JsonPropertyName("document_id")] string DocumentId,
    [property: JsonPropertyName("candidate_path")] string CandidatePath,
    [property: JsonPropertyName("match_kind")] string MatchKind,
    [property: JsonPropertyName("score")] double Score,
    [property: JsonPropertyName("requires_confirmation")] bool RequiresConfirmation);

public sealed class MetadataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _root;
    private readonly string _documents;
    private readonly string _users;
    private readonly string _projectPath;
    private readonly string _lockPath;

    public MetadataStore(string root)
    {
        _root = Path.GetFullPath(root);
        _documents = Path.Combine(_root, "documents");
        _users = Path.Combine(_root, "users");
        _projectPath = Path.Combine(_root, "project.json");
        _lockPath = Path.Combine(_root, "documents.lock");
    }

    public void Initialize()
    {
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_documents);
        Directory.CreateDirectory(_users);
        if (!File.Exists(_projectPath))
        {
            WriteAtomic(_projectPath, new ProjectMetadata(1, Ulid.New(), DateTimeOffset.UtcNow, new Dictionary<string, int>(StringComparer.Ordinal)));
        }
    }

    public DocumentMetadata CreateDocument(string relativePath, string documentType, ReadOnlySpan<byte> content)
    {
        ValidateRelativePath(relativePath);
        if (string.IsNullOrWhiteSpace(documentType)) throw new ArgumentException("Document type is required.", nameof(documentType));
        Initialize();

        using var lockHandle = AcquireLock();
        var project = Read<ProjectMetadata>(_projectPath);
        var next = project.NextSequenceByType.TryGetValue(documentType, out var value) ? value : 1;
        project.NextSequenceByType[documentType] = next + 1;
        WriteAtomic(_projectPath, project);

        var now = DateTimeOffset.UtcNow;
        var metadata = new DocumentMetadata(1, Ulid.New(), documentType, next, Normalize(relativePath), Sha256(content), now, now);
        WriteAtomic(Path.Combine(_documents, metadata.DocumentId + ".json"), metadata);
        return metadata;
    }

    public void SaveUserState(UserDocumentState state)
    {
        if (string.IsNullOrWhiteSpace(state.UserId) || string.IsNullOrWhiteSpace(state.DocumentId)) throw new ArgumentException("User and document IDs are required.");
        var directory = Path.Combine(_users, SanitizeSegment(state.UserId), "documents");
        Directory.CreateDirectory(directory);
        WriteAtomic(Path.Combine(directory, state.DocumentId + ".json"), state);
    }

    public IReadOnlyList<RenameCandidate> FindRenameCandidates(string currentRelativePath, ReadOnlySpan<byte> currentContent)
    {
        ValidateRelativePath(currentRelativePath);
        var hash = Sha256(currentContent);
        var documents = Directory.Exists(_documents)
            ? Directory.EnumerateFiles(_documents, "*.json").Select(path => Read<DocumentMetadata>(path)).ToArray()
            : [];
        return documents
            .Where(item => !string.Equals(item.RelativePath, Normalize(currentRelativePath), StringComparison.OrdinalIgnoreCase))
            .Where(item => string.Equals(item.ContentSha256, hash, StringComparison.OrdinalIgnoreCase))
            .Select(item => new RenameCandidate(item.DocumentId, item.RelativePath, "content_sha256", 1.0, true))
            .ToArray();
    }

    public static string Sha256(ReadOnlySpan<byte> content) => Convert.ToHexString(SHA256.HashData(content));

    public static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');

    private FileStream AcquireLock()
    {
        Directory.CreateDirectory(_root);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (true)
        {
            try { return new FileStream(_lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (DateTime.UtcNow < deadline) { Thread.Sleep(10); }
        }
    }

    private static void ValidateRelativePath(string path)
    {
        var normalized = Normalize(path);
        if (string.IsNullOrWhiteSpace(normalized) || Path.IsPathRooted(path) || normalized.Split('/').Any(segment => segment == ".."))
            throw new ArgumentException("Path must be a non-empty relative path.", nameof(path));
    }

    private static string SanitizeSegment(string value) => string.Concat(value.Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.' ? character : '_'));

    private static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions) ?? throw new InvalidDataException(path);

    private static void WriteAtomic<T>(string path, T value)
    {
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, value, JsonOptions);
                stream.Flush(true);
            }
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}

public static class Ulid
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string New()
    {
        Span<byte> bytes = stackalloc byte[16];
        var milliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        for (var index = 5; index >= 0; index--) { bytes[index] = (byte)(milliseconds & 0xff); milliseconds >>= 8; }
        RandomNumberGenerator.Fill(bytes[6..]);
        Span<char> result = stackalloc char[26];
        var value = new System.Numerics.BigInteger(bytes.ToArray(), isUnsigned: true, isBigEndian: true);
        for (var index = 25; index >= 0; index--) { result[index] = Alphabet[(int)(value & 31)]; value >>= 5; }
        return new string(result);
    }
}
