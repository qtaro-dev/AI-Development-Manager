using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

const int DocumentCount = 10_000;
const int Seed = 15_015;
const int Warmups = 1;
const int Samples = 5;
var runId = $"{DateTime.UtcNow:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}";
var outputRoot = Path.Combine(Path.GetTempPath(), "AI-Development-Manager", "poc", "P0-024", runId);
var corpusRoot = Path.Combine(outputRoot, "corpus");
Directory.CreateDirectory(corpusRoot);
var json = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var process = Process.GetCurrentProcess();
Console.WriteLine($"P0-024 run={runId} SDK=10.0.302 Runtime={Environment.Version}");

var generation = GenerateCorpus(corpusRoot);
var empty = new Dictionary<string, SnapshotEntry>(StringComparer.OrdinalIgnoreCase);
var baseline = Scan(corpusRoot, empty, new ScanOptions());
var baselineCache = baseline.Snapshot;
var scenarios = new List<ScenarioSummary>();
scenarios.Add(MeasureScenario("initial_cold", () => Scan(corpusRoot, new Dictionary<string, SnapshotEntry>(StringComparer.OrdinalIgnoreCase), new ScanOptions())));
scenarios.Add(MeasureScenario("unchanged_warm", () => Scan(corpusRoot, CloneSnapshot(baselineCache), new ScanOptions())));

var singleRelative = "ticket/DOC-00001.md";
var singlePath = Path.Combine(corpusRoot, singleRelative.Replace('/', Path.DirectorySeparatorChar));
var singleOriginal = File.ReadAllBytes(singlePath);
var singleTime = File.GetLastWriteTimeUtc(singlePath);
scenarios.Add(MeasureScenario("single_document_changed", () => {
    try { File.AppendAllText(singlePath, "\n変更検出用の追記。", new UTF8Encoding(false)); return Scan(corpusRoot, CloneSnapshot(baselineCache), new ScanOptions()); }
    finally { File.WriteAllBytes(singlePath, singleOriginal); File.SetLastWriteTimeUtc(singlePath, singleTime); }
}));

var multiRelative = new[] { "ticket/DOC-00002.md", "test_case/DOC-07001.md", "design/DOC-09001.md" };
var multiOriginal = multiRelative.ToDictionary(x => x, x => File.ReadAllBytes(Path.Combine(corpusRoot, x.Replace('/', Path.DirectorySeparatorChar))), StringComparer.OrdinalIgnoreCase);
var deletedRelative = "ticket/DOC-00003.md";
var deletedBytes = File.ReadAllBytes(Path.Combine(corpusRoot, deletedRelative.Replace('/', Path.DirectorySeparatorChar)));
var deletedTime = File.GetLastWriteTimeUtc(Path.Combine(corpusRoot, deletedRelative.Replace('/', Path.DirectorySeparatorChar)));
var renamedFrom = "ticket/DOC-00004.md";
var renamedTo = "ticket/DOC-00004-renamed.md";
var addedRelative = "ticket/DOC-added.md";
scenarios.Add(MeasureScenario("multiple_add_change_delete_rename", () => {
    var from = Path.Combine(corpusRoot, renamedFrom.Replace('/', Path.DirectorySeparatorChar));
    var to = Path.Combine(corpusRoot, renamedTo.Replace('/', Path.DirectorySeparatorChar));
    try {
        foreach (var relative in multiRelative) File.AppendAllText(Path.Combine(corpusRoot, relative.Replace('/', Path.DirectorySeparatorChar)), "\n複数変更。", new UTF8Encoding(false));
        File.Delete(Path.Combine(corpusRoot, deletedRelative.Replace('/', Path.DirectorySeparatorChar)));
        File.Move(from, to);
        File.WriteAllText(Path.Combine(corpusRoot, addedRelative.Replace('/', Path.DirectorySeparatorChar)), "# 追加文書\n", new UTF8Encoding(false));
        return Scan(corpusRoot, CloneSnapshot(baselineCache), new ScanOptions());
    } finally {
        foreach (var pair in multiOriginal) { File.WriteAllBytes(Path.Combine(corpusRoot, pair.Key.Replace('/', Path.DirectorySeparatorChar)), pair.Value); }
        if (File.Exists(to)) File.Move(to, from);
        File.WriteAllBytes(Path.Combine(corpusRoot, deletedRelative.Replace('/', Path.DirectorySeparatorChar)), deletedBytes); File.SetLastWriteTimeUtc(Path.Combine(corpusRoot, deletedRelative.Replace('/', Path.DirectorySeparatorChar)), deletedTime);
        if (File.Exists(Path.Combine(corpusRoot, addedRelative.Replace('/', Path.DirectorySeparatorChar)))) File.Delete(Path.Combine(corpusRoot, addedRelative.Replace('/', Path.DirectorySeparatorChar)));
    }
}));

var failure = Scan(corpusRoot, CloneSnapshot(baselineCache), new ScanOptions("ticket/DOC-00005.md", ForceHash: true));
var cancellationSource = new CancellationTokenSource(); cancellationSource.Cancel();
ScanOutcome canceled;
try { canceled = Scan(corpusRoot, CloneSnapshot(baselineCache), new ScanOptions(), cancellationSource.Token); }
catch (OperationCanceledException) { canceled = new ScanOutcome(ElapsedMs: 0, Snapshot: CloneSnapshot(baselineCache), StageMilliseconds: new Dictionary<string, double>(), TotalFiles: 0, Added: 0, Changed: 0, Deleted: 0, Renamed: 0, BodyReads: 0, HashReads: 0, Errors: 0, BytesRead: 0, Canceled: true); }
var sameAttributeRelative = "ticket/DOC-00006.md";
var sameAttributePath = Path.Combine(corpusRoot, sameAttributeRelative.Replace('/', Path.DirectorySeparatorChar));
var sameAttributeOriginal = File.ReadAllBytes(sameAttributePath); var sameAttributeTime = File.GetLastWriteTimeUtc(sameAttributePath);
ScanOutcome sameAttributeNormal; ScanOutcome sameAttributeForced;
try {
    var changedSameLength = sameAttributeOriginal.ToArray(); changedSameLength[changedSameLength.Length - 1] = (byte)(changedSameLength[^1] == (byte)'X' ? 'Y' : 'X');
    File.WriteAllBytes(sameAttributePath, changedSameLength); File.SetLastWriteTimeUtc(sameAttributePath, sameAttributeTime);
    sameAttributeNormal = Scan(corpusRoot, CloneSnapshot(baselineCache), new ScanOptions());
    sameAttributeForced = Scan(corpusRoot, CloneSnapshot(baselineCache), new ScanOptions(ForceHash: true));
} finally { File.WriteAllBytes(sameAttributePath, sameAttributeOriginal); File.SetLastWriteTimeUtc(sameAttributePath, sameAttributeTime); }

var checks = new List<CheckResult> {
    Check("initial_p95_under_3000ms", scenarios.Single(x => x.Name == "initial_cold").P95Ms <= 3000, scenarios.Single(x => x.Name == "initial_cold").P95Ms),
    Check("unchanged_p95_under_2000ms", scenarios.Single(x => x.Name == "unchanged_warm").P95Ms <= 2000, scenarios.Single(x => x.Name == "unchanged_warm").P95Ms),
    Check("single_change_p95_under_2000ms", scenarios.Single(x => x.Name == "single_document_changed").P95Ms <= 2000, scenarios.Single(x => x.Name == "single_document_changed").P95Ms),
    Check("unchanged_skips_body_and_hash", scenarios.Single(x => x.Name == "unchanged_warm").BodyReads == 0 && scenarios.Single(x => x.Name == "unchanged_warm").HashReads == 0, $"body={scenarios.Single(x => x.Name == "unchanged_warm").BodyReads}, hash={scenarios.Single(x => x.Name == "unchanged_warm").HashReads}"),
    Check("multi_change_detection", scenarios.Single(x => x.Name == "multiple_add_change_delete_rename").Added == 1 && scenarios.Single(x => x.Name == "multiple_add_change_delete_rename").Changed == 3 && scenarios.Single(x => x.Name == "multiple_add_change_delete_rename").Deleted == 1 && scenarios.Single(x => x.Name == "multiple_add_change_delete_rename").Renamed == 1, "add/change/delete/rename counts"),
    Check("read_failure_isolated", failure.Errors == 1 && failure.TotalFiles == DocumentCount, $"errors={failure.Errors}, files={failure.TotalFiles}"),
    Check("cancellation_isolated", canceled.Canceled, $"canceled={canceled.Canceled}"),
    Check("same_attribute_change_forced_hash_detects", sameAttributeNormal.Changed == 0 && sameAttributeForced.Changed == 1, $"normal={sameAttributeNormal.Changed}, forced={sameAttributeForced.Changed}"),
    Check("peak_memory_under_512MiB", process.PeakWorkingSet64 <= 512L * 1024 * 1024, process.PeakWorkingSet64 / 1024d / 1024d)
};
var result = new PocResult("P0-024", runId, DocumentCount, Seed, "10.0.302", Environment.Version.ToString(), generation, scenarios, failure, canceled.Canceled, sameAttributeNormal.Changed, sameAttributeForced.Changed, checks, checks.All(x => x.Passed));
File.WriteAllText(Path.Combine(outputRoot, "result.json"), JsonSerializer.Serialize(result, json));
File.WriteAllText(Path.Combine(outputRoot, "run.log"), $"P0-024 {runId}\nSDK 10.0.302\nRuntime {Environment.Version}\n");
Console.WriteLine(JsonSerializer.Serialize(result, json)); Console.WriteLine($"RESULT_JSON={Path.Combine(outputRoot, "result.json")}");
Environment.ExitCode = result.Passed ? 0 : 1;

ScenarioSummary MeasureScenario(string name, Func<ScanOutcome> operation) {
    for (var i = 0; i < Warmups; i++) _ = operation();
    var samples = new List<ScanOutcome>();
    for (var i = 0; i < Samples; i++) samples.Add(operation());
    var values = samples.Select(x => x.ElapsedMs).OrderBy(x => x).ToArray();
    var last = samples[^1];
    return new ScenarioSummary(name, Percentile(values, .50), Percentile(values, .95), Percentile(values, .99), values[^1], last.BodyReads, last.HashReads, last.Added, last.Changed, last.Deleted, last.Renamed, last.Errors, last.BytesRead, last.StageMilliseconds);
}

ScanOutcome Scan(string root, Dictionary<string, SnapshotEntry> previous, ScanOptions options, CancellationToken cancellationToken = default) {
    var overall = Stopwatch.StartNew(); var stages = new Dictionary<string, double>(); var files = new List<FileAttribute>(); var snapshot = new Dictionary<string, SnapshotEntry>(StringComparer.OrdinalIgnoreCase); var added = 0; var changed = 0; var deleted = 0; var renamed = 0; var bodyReads = 0; var hashReads = 0; var errors = 0; long bytesRead = 0;
    var watch = Stopwatch.StartNew(); var paths = Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories).ToArray(); stages["path_enumeration"] = watch.Elapsed.TotalMilliseconds;
    watch.Restart(); foreach (var path in paths) { cancellationToken.ThrowIfCancellationRequested(); var info = new FileInfo(path); files.Add(new FileAttribute(Path.GetRelativePath(root, path).Replace('\\', '/'), info.Length, info.LastWriteTimeUtc.Ticks, path)); } stages["basic_attributes"] = watch.Elapsed.TotalMilliseconds;
    watch.Restart(); var metaPath = Path.Combine(root, ".adm-meta", "manifest.json"); _ = JsonDocument.Parse(File.ReadAllText(metaPath)); stages["adm_meta_read"] = watch.Elapsed.TotalMilliseconds;
    watch.Restart(); foreach (var file in files) { cancellationToken.ThrowIfCancellationRequested(); var same = previous.TryGetValue(file.RelativePath, out var old) && old.Length == file.Length && old.LastWriteUtcTicks == file.LastWriteUtcTicks && !options.ForceHash; var entry = new SnapshotEntry(file.RelativePath, file.Length, file.LastWriteUtcTicks, old?.Hash ?? ""); if (old is null) added++; snapshot[file.RelativePath] = entry; } deleted = previous.Keys.Count(x => !snapshot.ContainsKey(x)); stages["cache_compare"] = watch.Elapsed.TotalMilliseconds;
    watch.Restart(); var removed = previous.Values.Where(x => !snapshot.ContainsKey(x.RelativePath)).ToList(); var candidates = snapshot.Values.Where(entry => !previous.ContainsKey(entry.RelativePath) || options.ForceHash || previous[entry.RelativePath].Length != entry.Length || previous[entry.RelativePath].LastWriteUtcTicks != entry.LastWriteUtcTicks).ToList(); stages["candidate_extract"] = watch.Elapsed.TotalMilliseconds;
    watch.Restart(); foreach (var candidate in candidates) { cancellationToken.ThrowIfCancellationRequested(); if (candidate.RelativePath.Equals(options.InjectedFailureRelativePath, StringComparison.OrdinalIgnoreCase)) { errors++; continue; } try { var bytes = File.ReadAllBytes(Path.Combine(root, candidate.RelativePath.Replace('/', Path.DirectorySeparatorChar))); bodyReads++; bytesRead += bytes.LongLength; var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(); hashReads++; if (previous.TryGetValue(candidate.RelativePath, out var old) && old.Hash != hash) changed++; snapshot[candidate.RelativePath] = candidate with { Hash = hash }; _ = ParseMarkdown(bytes); } catch (FileNotFoundException) { errors++; } } stages["body_hash_parse"] = watch.Elapsed.TotalMilliseconds;
    watch.Restart(); foreach (var add in snapshot.Values.Where(x => !previous.ContainsKey(x.RelativePath)).ToList()) { var match = removed.FirstOrDefault(x => x.Hash.Length > 0 && x.Hash == add.Hash); if (match is not null) { renamed++; added--; deleted--; } } stages["handoff"] = watch.Elapsed.TotalMilliseconds;
    overall.Stop(); return new ScanOutcome(overall.Elapsed.TotalMilliseconds, snapshot, stages, files.Count, added, changed, deleted, renamed, bodyReads, hashReads, errors, bytesRead, false);
}

int ParseMarkdown(byte[] bytes) { var text = Encoding.UTF8.GetString(bytes); return text.Count(x => x == '#'); }
Dictionary<string, SnapshotEntry> CloneSnapshot(Dictionary<string, SnapshotEntry> source) => source.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
double Percentile(IReadOnlyList<double> values, double p) => values[Math.Min(values.Count - 1, Math.Max(0, (int)Math.Ceiling(values.Count * p) - 1))];
CheckResult Check(string name, bool passed, object detail) => new(name, passed, detail.ToString() ?? string.Empty);

CorpusSummary GenerateCorpus(string root) {
    var random = new Random(Seed); long bytes = 0; var buckets = new Dictionary<string, int>();
    for (var i = 0; i < DocumentCount; i++) { var type = i < 7000 ? "ticket" : i < 8500 ? "test_case" : i < 9500 ? "design" : "adr"; var target = i < 7000 ? 1024 : i < 9500 ? 4096 : 8192; var path = Path.Combine(root, type, $"DOC-{i + 1:00000}.md"); Directory.CreateDirectory(Path.GetDirectoryName(path)!); var text = new StringBuilder($"# 文書 {i + 1}\n\n本文 {random.Next()} ユーザー認証と走査処理の検証。\n"); while (text.Length < target) text.Append("追加本文。 "); File.WriteAllText(path, text.ToString(), new UTF8Encoding(false)); bytes += new FileInfo(path).Length; var bucket = target <= 1024 ? "1-16KiB" : target <= 4096 ? "16-256KiB" : "256KiB-2MiB"; buckets[bucket] = buckets.GetValueOrDefault(bucket) + 1; }
    Directory.CreateDirectory(Path.Combine(root, ".adm-meta")); File.WriteAllText(Path.Combine(root, ".adm-meta", "manifest.json"), JsonSerializer.Serialize(new { schema_version = 1, documents = DocumentCount })); return new CorpusSummary(DocumentCount, bytes, buckets);
}

record FileAttribute(string RelativePath, long Length, long LastWriteUtcTicks, string FullPath);
record SnapshotEntry(string RelativePath, long Length, long LastWriteUtcTicks, string Hash);
record ScanOptions(string? InjectedFailureRelativePath = null, bool ForceHash = false);
record ScanOutcome(double ElapsedMs, Dictionary<string, SnapshotEntry> Snapshot, Dictionary<string, double> StageMilliseconds, int TotalFiles, int Added, int Changed, int Deleted, int Renamed, int BodyReads, int HashReads, int Errors, long BytesRead, bool Canceled);
record ScenarioSummary(string Name, double P50Ms, double P95Ms, double P99Ms, double MaxMs, int BodyReads, int HashReads, int Added, int Changed, int Deleted, int Renamed, int Errors, long BytesRead, Dictionary<string, double> StageMilliseconds);
record CorpusSummary(int Documents, long Bytes, Dictionary<string, int> Buckets);
record CheckResult(string Name, bool Passed, string Detail);
record PocResult(string Ticket, string RunId, int Documents, int Seed, string Sdk, string Runtime, CorpusSummary Corpus, List<ScenarioSummary> Scenarios, ScanOutcome FailureScenario, bool CancellationObserved, int SameAttributeNormalChanged, int SameAttributeForcedChanged, List<CheckResult> Checks, bool Passed);
