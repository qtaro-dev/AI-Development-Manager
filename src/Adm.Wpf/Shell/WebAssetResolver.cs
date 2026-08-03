using System.IO;

namespace Adm.Wpf.Shell;

public sealed record WebAssetResolution(string RootDirectory, string EntryPoint);

public static class WebAssetResolver
{
    public const string WebAssetsDirectoryName = "WebAssets";

    public static string GetRootDirectory(string? baseDirectory)
    {
        return Path.GetFullPath(Path.Combine(
            baseDirectory ?? AppContext.BaseDirectory,
            WebAssetsDirectoryName));
    }

    public static bool TryResolve(string? baseDirectory, out WebAssetResolution? resolution)
    {
        var rootDirectory = GetRootDirectory(baseDirectory);
        var entryPoint = Path.Combine(rootDirectory, "index.html");

        if (!Directory.Exists(rootDirectory) || !File.Exists(entryPoint))
        {
            resolution = null;
            return false;
        }

        resolution = new WebAssetResolution(rootDirectory, entryPoint);
        return true;
    }
}
