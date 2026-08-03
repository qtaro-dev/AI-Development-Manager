using System.Text.Json;
using System.IO;

namespace Adm.Poc.P1029;

public sealed class PocTelemetry(string evidencePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly object gate = new();
    private readonly List<object> entries = [];

    public void Record(string category, object details)
    {
        lock (gate)
        {
            entries.Add(new
            {
                timestamp_utc = DateTimeOffset.UtcNow,
                category,
                details
            });
        }
    }

    public void Save(string? measurementPath, object summary)
    {
        Directory.CreateDirectory(evidencePath);
        var payload = new
        {
            generated_at_utc = DateTimeOffset.UtcNow,
            summary,
            entries
        };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        File.WriteAllText(Path.Combine(evidencePath, "telemetry.json"), json);
        if (!string.IsNullOrWhiteSpace(measurementPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(measurementPath)!);
            File.WriteAllText(measurementPath, json);
        }
    }
}
