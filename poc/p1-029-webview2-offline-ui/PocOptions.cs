using System.IO;

namespace Adm.Poc.P1029;

public sealed record PocOptions(string AssetsPath, string EvidencePath, string? MeasurementPath, int AutoExitMilliseconds)
{
    public static PocOptions Parse(string[] args)
    {
        var values = args
            .Select(argument => argument.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

        var assets = values.GetValueOrDefault("--assets")
            ?? throw new ArgumentException("--assets=<path> is required.");
        var evidence = values.GetValueOrDefault("--evidence")
            ?? throw new ArgumentException("--evidence=<path> is required.");
        var measurement = values.GetValueOrDefault("--measurement");
        var autoExit = values.TryGetValue("--auto-exit-ms", out var rawMilliseconds) && int.TryParse(rawMilliseconds, out var milliseconds)
            ? milliseconds
            : 0;

        return new PocOptions(
            Path.GetFullPath(assets),
            Path.GetFullPath(evidence),
            string.IsNullOrWhiteSpace(measurement) ? null : Path.GetFullPath(measurement),
            autoExit);
    }
}
