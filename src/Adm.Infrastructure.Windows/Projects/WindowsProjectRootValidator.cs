using System.Security.AccessControl;
using Adm.Application.Projects;
using Adm.Core.Projects;

namespace Adm.Infrastructure.Windows.Projects;

public sealed class WindowsProjectRootValidator : IProjectRootValidator
{
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    public Task<ProjectRootValidationResult> ValidateAsync(
        ProjectRootInput root,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (!OperatingSystem.IsWindows())
                return Task.FromResult(ProjectRootValidationResult.Invalid(ProjectErrorCode.UnsupportedFileSystem));

            if (!TryNormalizeLocalPath(root.Value, out var canonicalPath))
                return Task.FromResult(ProjectRootValidationResult.Invalid(ProjectErrorCode.InvalidRoot));

            if (!Directory.Exists(canonicalPath))
                return Task.FromResult(ProjectRootValidationResult.Invalid(ProjectErrorCode.InvalidRoot));

            if (HasReparsePoint(canonicalPath) || HasReparsePointAncestor(canonicalPath))
                return Task.FromResult(ProjectRootValidationResult.Invalid(ProjectErrorCode.InvalidRoot));

            var drive = new DriveInfo(Path.GetPathRoot(canonicalPath)!);
            if (!string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(ProjectRootValidationResult.Invalid(ProjectErrorCode.UnsupportedFileSystem));

            _ = new DirectoryInfo(canonicalPath).GetAccessControl(AccessControlSections.Access);

            return Task.FromResult(ProjectRootValidationResult.Valid(new ValidatedProjectRoot(canonicalPath)));
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(ProjectRootValidationResult.Invalid(ProjectErrorCode.AccessDenied));
        }
        catch (ArgumentException)
        {
            return Task.FromResult(ProjectRootValidationResult.Invalid(ProjectErrorCode.InvalidRoot));
        }
        catch (IOException)
        {
            return Task.FromResult(ProjectRootValidationResult.Invalid(ProjectErrorCode.InvalidRoot));
        }
        catch (PlatformNotSupportedException)
        {
            return Task.FromResult(ProjectRootValidationResult.Invalid(ProjectErrorCode.UnsupportedFileSystem));
        }
    }

    private static bool TryNormalizeLocalPath(string rawPath, out string canonicalPath)
    {
        canonicalPath = string.Empty;
        if (string.IsNullOrWhiteSpace(rawPath) ||
            rawPath.StartsWith(@"\\", StringComparison.Ordinal) ||
            rawPath.StartsWith(@"\\?\", StringComparison.Ordinal) ||
            rawPath.StartsWith(@"\\.\", StringComparison.Ordinal) ||
            !Path.IsPathFullyQualified(rawPath) ||
            ContainsAlternateDataStream(rawPath) ||
            ContainsUnsafeSegment(rawPath))
        {
            return false;
        }

        canonicalPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rawPath));
        return !string.IsNullOrWhiteSpace(canonicalPath) && Path.IsPathFullyQualified(canonicalPath);
    }

    private static bool ContainsAlternateDataStream(string path)
    {
        var rootLength = Path.GetPathRoot(path)?.Length ?? 0;
        return path[rootLength..].Contains(':', StringComparison.Ordinal);
    }

    private static bool ContainsUnsafeSegment(string path)
    {
        var root = Path.GetPathRoot(path) ?? string.Empty;
        foreach (var segment in path[root.Length..].Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.EndsWith('.') || segment.EndsWith(' '))
                return true;

            var name = segment.Split('.', 2)[0];
            if (ReservedNames.Contains(name))
                return true;
        }

        return false;
    }

    private static bool HasReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static bool HasReparsePointAncestor(string path)
    {
        var current = Directory.GetParent(path);
        while (current is not null && !PathComparer.Equals(current.FullName, Path.GetPathRoot(path)))
        {
            if (HasReparsePoint(current.FullName))
                return true;

            current = current.Parent;
        }

        return current is not null && HasReparsePoint(current.FullName);
    }
}
