using System.Text;
using Adm.Storage.SafePath;

var root = Path.Combine(Path.GetTempPath(), $"path-security-poc-{Guid.NewGuid():N}");
var outside = Path.Combine(Path.GetTempPath(), $"path-security-outside-{Guid.NewGuid():N}");
Directory.CreateDirectory(Path.Combine(root, "docs"));
Directory.CreateDirectory(outside);
var service = new SafePathService(root);
try
{
    var allowed = new[] { "docs/readme.md", "docs/日本語ファイル.md", "docs/sub/normal.txt" };
    foreach (var path in allowed)
        RequireAllOperations(service, path, true, PathRejectReason.None);

    var rejected = new[]
    {
        ("../secret.txt", PathRejectReason.Traversal),
        ("..\\secret.txt", PathRejectReason.Traversal),
        ("%2e%2e%2fsecret.txt", PathRejectReason.EncodedTraversal),
        ("%252e%252e%252fsecret.txt", PathRejectReason.EncodedTraversal),
        ("C:\\secret.txt", PathRejectReason.AbsolutePath),
        ("\\\\server\\share\\secret.txt", PathRejectReason.AbsolutePath),
        ("\\\\.\\PhysicalDrive0", PathRejectReason.AbsolutePath),
        ("docs/file.txt:secret", PathRejectReason.AlternateDataStream),
        ("docs/CON.txt", PathRejectReason.ReservedName),
        ("docs/name. ", PathRejectReason.TrailingDotOrSpace)
    };
    foreach (var (path, reason) in rejected) RequireAllOperations(service, path, false, reason);

    var upload = service.NormalizeUploadName("日本語レポート.md");
    Require(upload.Allowed && upload.FullPath == "日本語レポート.md", "normal upload name rejected");
    Require(!service.NormalizeUploadName("..%2fsecret.txt").Allowed, "encoded upload traversal accepted");
    Require(!service.NormalizeUploadName("folder\\file.txt").Allowed, "upload path accepted");
    Require(!service.NormalizeUploadName("CON.txt").Allowed, "reserved upload name accepted");

    var linkAvailable = false;
    var linkDirectory = Path.Combine(root, "links");
    try
    {
        Directory.CreateSymbolicLink(linkDirectory, outside);
        linkAvailable = true;
        var linkResult = service.Validate(PathOperation.Read, "links/secret.txt");
        Require(!linkResult.Allowed && linkResult.Reason == PathRejectReason.ReparsePoint, "symbolic link escape accepted");
    }
    catch (UnauthorizedAccessException) { }
    catch (IOException) { }

    var boundary = service.Validate(PathOperation.Read, "docs/../docs/readme.md");
    Require(!boundary.Allowed && boundary.Reason == PathRejectReason.Traversal, "parent segment was normalized instead of rejected");
    var result = service.Validate(PathOperation.Download, "docs/readme.md");
    Require(result.Allowed && result.FullPath!.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "download boundary failed");
    Console.WriteLine($"PASS allowed={allowed.Length} rejected={rejected.Length} common_operations=true upload_normalized=true reparse_rejected={linkAvailable} ads_rejected=true reserved_rejected=true japanese_name=true");
    Console.WriteLine("Policy=relative-boundary+reparse-reject+double-decode; SDK=10.0.302 Runtime=10.0.10");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, true);
    if (Directory.Exists(outside)) Directory.Delete(outside, true);
}

static void RequireAllOperations(SafePathService service, string path, bool allowed, PathRejectReason reason)
{
    foreach (var operation in Enum.GetValues<PathOperation>())
    {
        var result = service.Validate(operation, path);
        if (result.Allowed != allowed || (!allowed && result.Reason != reason)) throw new InvalidOperationException($"{operation}:{path} expected={allowed}/{reason} actual={result.Allowed}/{result.Reason}");
    }
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
