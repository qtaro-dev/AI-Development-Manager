using System.Net;
using System.Text;

namespace Adm.Storage.SafePath;

public enum PathOperation { Read, Write, Download, Upload }

public enum PathRejectReason
{
    None, Empty, EncodedTraversal, AbsolutePath, Traversal, OutsideRoot, ReparsePoint, AlternateDataStream,
    ReservedName, TrailingDotOrSpace, InvalidUploadName, InvalidCharacters
}

public sealed record PathValidationResult(bool Allowed, string? FullPath, PathRejectReason Reason, string InternalDetail, string UserMessage);

public sealed class SafePathService
{
    private readonly string _root;
    private readonly string _rootWithSeparator;

    public SafePathService(string projectRoot)
    {
        _root = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        _rootWithSeparator = _root + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(_root);
    }

    public PathValidationResult Validate(PathOperation operation, string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Reject(PathRejectReason.Empty, "path.empty");
        var decoded = DecodeAtMostTwice(input, out var encodedTraversal);
        if (encodedTraversal) return Reject(PathRejectReason.EncodedTraversal, "path.encoded_traversal");
        if (decoded.IndexOf('\0') >= 0) return Reject(PathRejectReason.InvalidCharacters, "path.null_character");
        if (Path.IsPathRooted(decoded) || decoded.StartsWith("\\\\", StringComparison.Ordinal) || decoded.StartsWith("//", StringComparison.Ordinal) || IsDevicePath(decoded))
            return Reject(PathRejectReason.AbsolutePath, "path.absolute_or_device");
        if (decoded.Contains(':', StringComparison.Ordinal)) return Reject(PathRejectReason.AlternateDataStream, "path.colon_or_ads");

        var segments = decoded.Replace('\\', '/').Split('/', StringSplitOptions.None);
        if (segments.Any(segment => segment == "..")) return Reject(PathRejectReason.Traversal, "path.parent_segment");
        if (segments.Any(segment => HasTrailingDotOrSpace(segment))) return Reject(PathRejectReason.TrailingDotOrSpace, "path.trailing_dot_or_space");
        if (segments.Any(segment => IsReservedName(segment))) return Reject(PathRejectReason.ReservedName, "path.reserved_name");

        var candidate = Path.GetFullPath(Path.Combine(_root, decoded.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));
        if (!candidate.Equals(_root, StringComparison.OrdinalIgnoreCase) && !candidate.StartsWith(_rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            return Reject(PathRejectReason.OutsideRoot, "path.resolved_outside_root");
        if (ContainsReparsePoint(candidate)) return Reject(PathRejectReason.ReparsePoint, "path.reparse_point");
        return new PathValidationResult(true, candidate, PathRejectReason.None, "path.allowed", "ファイルパスを確認しました。");
    }

    public PathValidationResult NormalizeUploadName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Reject(PathRejectReason.InvalidUploadName, "upload.empty");
        var decoded = DecodeAtMostTwice(input, out var traversal);
        if (traversal || decoded.Contains('/', StringComparison.Ordinal) || decoded.Contains('\\', StringComparison.Ordinal) || decoded is "." or "..")
            return Reject(PathRejectReason.InvalidUploadName, "upload.must_be_single_name");
        if (decoded.Contains(':', StringComparison.Ordinal) || HasTrailingDotOrSpace(decoded) || IsReservedName(decoded))
            return Reject(PathRejectReason.InvalidUploadName, "upload.invalid_windows_name");
        var normalized = decoded.Normalize(NormalizationForm.FormC);
        if (normalized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return Reject(PathRejectReason.InvalidUploadName, "upload.invalid_character");
        return new PathValidationResult(true, normalized, PathRejectReason.None, "upload.normalized", "アップロード名を確認しました。");
    }

    private PathValidationResult Reject(PathRejectReason reason, string detail) => new(false, null, reason, detail, "指定されたパスは安全のため利用できません。");

    private bool ContainsReparsePoint(string candidate)
    {
        var relative = Path.GetRelativePath(_root, candidate);
        if (relative == ".") return false;
        var current = _root;
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current) || Directory.Exists(current))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return true;
            }
        }
        return false;
    }

    private static bool IsDevicePath(string value) => value.StartsWith("\\.\\", StringComparison.Ordinal) || value.StartsWith("\\?\\", StringComparison.Ordinal) || value.StartsWith("\\??\\", StringComparison.Ordinal);

    private static string DecodeAtMostTwice(string value, out bool encodedTraversal)
    {
        var current = value;
        encodedTraversal = false;
        for (var index = 0; index < 2; index++)
        {
            var decoded = WebUtility.UrlDecode(current);
            if (decoded != current && decoded.Replace('\\', '/').Split('/').Any(segment => segment == "..")) encodedTraversal = true;
            if (decoded == current) break;
            current = decoded;
        }
        return current;
    }

    private static bool HasTrailingDotOrSpace(string value) => value.EndsWith(' ') || value.EndsWith('.') || value.Length == 0;

    private static bool IsReservedName(string value)
    {
        var baseName = value.TrimEnd(' ', '.').Split('.')[0];
        return baseName.Equals("CON", StringComparison.OrdinalIgnoreCase) || baseName.Equals("PRN", StringComparison.OrdinalIgnoreCase) || baseName.Equals("AUX", StringComparison.OrdinalIgnoreCase) || baseName.Equals("NUL", StringComparison.OrdinalIgnoreCase) || (baseName.Length == 4 && (baseName.StartsWith("COM", StringComparison.OrdinalIgnoreCase) || baseName.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) && baseName[3] is >= '1' and <= '9');
    }
}
