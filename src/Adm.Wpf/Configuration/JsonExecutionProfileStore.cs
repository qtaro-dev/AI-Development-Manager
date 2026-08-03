using System.IO;
using Adm.Application.ExecutionProfiles;

namespace Adm.Wpf.Configuration;

public sealed class JsonExecutionProfileStore(string? filePath = null) : IExecutionProfileStore
{
    private readonly string filePath = filePath ?? ExecutionProfilePathResolver.GetPath();

    public async Task<string?> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return null;

        return await File.ReadAllTextAsync(filePath, cancellationToken);
    }

    public async Task WriteAsync(string json, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(filePath)
            ?? throw new IOException("Execution profile directory is unavailable.");
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.SequentialScan | FileOptions.WriteThrough))
            await using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(json.AsMemory(), cancellationToken);
                await writer.FlushAsync(cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(true);
            }

            if (File.Exists(filePath))
            {
                File.Replace(temporaryPath, filePath, null, true);
            }
            else
            {
                File.Move(temporaryPath, filePath);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
