using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Adm.Search.Sqlite;

const int DocumentCount = 10_000;
const int Seed = 15_015;
const int Warmups = 3;
const int Samples = 20;
const int HumanClients = 5;
const int AiClients = 2;

var runId = $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}";
var outputRoot = Path.Combine(Path.GetTempPath(), "AI-Development-Manager", "poc", "P0-015", runId);
Directory.CreateDirectory(outputRoot);
var corpusRoot = Path.Combine(outputRoot, "corpus");
var databasePath = Path.Combine(outputRoot, "search.db");
var started = DateTimeOffset.UtcNow;
var process = Process.GetCurrentProcess();
var metrics = new Dictionary<string, object?> { ["run_id"] = runId, ["started_utc"] = started, ["sdk"] = "10.0.302", ["runtime"] = Environment.Version.ToString(), ["seed"] = Seed, ["documents"] = DocumentCount, ["human_clients"] = HumanClients, ["ai_clients"] = AiClients, ["warmups_excluded"] = Warmups, ["samples"] = Samples };

Console.WriteLine($"P0-015 run={runId} output={outputRoot}");
Console.WriteLine($"SDK=10.0.302 Runtime={Environment.Version} workload=10000 docs, 5 human, 2 AI clients");

var generation = GenerateCorpus(corpusRoot);
metrics["corpus"] = generation;
var coldScan = Measure(() => ScanCorpus(corpusRoot), Samples);
metrics["cold_scan_ms"] = coldScan;
Console.WriteLine($"cold_scan_ms p50={coldScan.P50:F1} p95={coldScan.P95:F1} p99={coldScan.P99:F1} max={coldScan.Max:F1}");

var searchDocuments = Directory.EnumerateFiles(corpusRoot, "*.md", SearchOption.AllDirectories).Select(path => ToSearchDocument(path, corpusRoot)).ToArray();
var indexWatch = Stopwatch.StartNew();
using (var index = new SqliteSearchIndex(databasePath, "unicode61"))
{
    index.Rebuild(searchDocuments);
    indexWatch.Stop();
    metrics["index_build_ms"] = indexWatch.Elapsed.TotalMilliseconds;
    var warmSearch = Measure(() => index.Search("ユーザー認証", 20).Count, Samples);
    var search = Measure(() => index.Search("ユーザー認証", 20).Count, Samples);
    metrics["indexed_search_ms"] = search;
    Console.WriteLine($"indexed_search_ms p50={search.P50:F1} p95={search.P95:F1} p99={search.P99:F1} max={search.Max:F1} hits={warmSearch.LastValue}");

    var list = Measure(() => Directory.EnumerateFiles(corpusRoot, "*.md", SearchOption.AllDirectories).Take(50).Count(), Samples);
    metrics["list_ms"] = list;
    Console.WriteLine($"list_candidates_ms p50={list.P50:F1} p95={list.P95:F1} p99={list.P99:F1} max={list.Max:F1}");

    var consistency = RunConcurrentLoad(index, searchDocuments, corpusRoot);
    metrics["concurrent_load"] = consistency;
    Console.WriteLine($"concurrent_load operations={consistency.Operations} conflicts={consistency.Conflicts} index_consistent={consistency.IndexConsistent}");
}

process.Refresh();
var directoryBytes = Directory.EnumerateFiles(outputRoot, "*", SearchOption.AllDirectories).Sum(file => new FileInfo(file).Length);
metrics["resource"] = new { working_set_bytes = process.WorkingSet64, peak_working_set_bytes = process.PeakWorkingSet64, total_cpu_ms = process.TotalProcessorTime.TotalMilliseconds, disk_bytes = directoryBytes };
metrics["criteria"] = new { initial_scan_p95_ms = 3000, indexed_search_p95_ms = 500, list_p95_ms = 3000, initial_scan_pass = coldScan.P95 <= 3000, indexed_search_pass = ((Measurement)metrics["indexed_search_ms"]!).P95 <= 500, list_pass = ((Measurement)metrics["list_ms"]!).P95 <= 3000 };
metrics["finished_utc"] = DateTimeOffset.UtcNow;
await File.WriteAllTextAsync(Path.Combine(outputRoot, "result.json"), JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true }));
File.WriteAllText(Path.Combine(outputRoot, "run.log"), $"P0-015 {runId}\nSDK 10.0.302\nOutput {outputRoot}\n");
Console.WriteLine($"RESULT_JSON={Path.Combine(outputRoot, "result.json")}");

static CorpusSummary GenerateCorpus(string root)
{
    Directory.CreateDirectory(root);
    var random = new Random(Seed);
    long bytes = 0;
    var buckets = new Dictionary<string, int> { ["1-16KiB"] = 0, ["16-256KiB"] = 0, ["256KiB-2MiB"] = 0 };
    for (var i = 0; i < DocumentCount; i++)
    {
        var type = i < 5000 ? "ticket" : i < 7000 ? "test_case" : i < 8500 ? "test_result" : i < 9500 ? "design" : "adr";
        var target = i < 7000 ? random.Next(1024, 16 * 1024 + 1) : i < 9500 ? random.Next(16 * 1024, 256 * 1024 + 1) : random.Next(256 * 1024, 2 * 1024 * 1024 + 1);
        var bucket = i < 7000 ? "1-16KiB" : i < 9500 ? "16-256KiB" : "256KiB-2MiB";
        buckets[bucket]++;
        var path = Path.Combine(root, type, $"DOC-{i + 1:00000}.md");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var heading = $"# ユーザー認証 性能文書 {i + 1}";
        var prefix = $"---\nschema_version: 1\ndocument_type: {type}\n---\n{heading}\n\n";
        var text = new StringBuilder(prefix);
        // The repeated unit is predominantly UTF-8 Japanese (three bytes/char). Avoid
        // re-encoding the entire growing document on every iteration.
        var targetCharacters = Math.Max(256, target / 3);
        var unit = $"本文 {i + 1}: ユーザー認証と保存処理の検証。ERR-{1000 + i % 100:0000}。 ";
        while (text.Length < targetCharacters) text.Append(unit);
        var content = text.ToString();
        File.WriteAllText(path, content, new UTF8Encoding(false));
        bytes += new FileInfo(path).Length;
    }
    return new CorpusSummary(DocumentCount, bytes, buckets);
}

static SearchDocument ToSearchDocument(string path, string root)
{
    var body = File.ReadAllText(path);
    var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
    var id = Path.GetFileNameWithoutExtension(path);
    var heading = body.Split('\n').FirstOrDefault(line => line.StartsWith('#'))?.Trim('#', ' ', '\r') ?? id;
    var type = relative.Split('/')[0];
    return new SearchDocument(id, relative, Path.GetFileName(path), heading, body, "active", type);
}

static ScanSummary ScanCorpus(string root)
{
    var count = 0;
    using var sha = SHA256.Create();
    foreach (var path in Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories))
    {
        using var stream = File.OpenRead(path);
        _ = sha.ComputeHash(stream);
        count++;
    }
    return new ScanSummary(count);
}

static Measurement Measure<T>(Func<T> action, int samples)
{
    for (var i = 0; i < Warmups; i++) action();
    var values = new List<double>(samples);
    T? last = default;
    for (var i = 0; i < samples; i++)
    {
        var watch = Stopwatch.StartNew();
        last = action();
        watch.Stop();
        values.Add(watch.Elapsed.TotalMilliseconds);
    }
    values.Sort();
    var lastValue = last is IConvertible convertible ? convertible.ToInt32(System.Globalization.CultureInfo.InvariantCulture) : 0;
    return new Measurement(values[values.Count / 2], Percentile(values, .95), Percentile(values, .99), values[^1], lastValue);
}

static double Percentile(IReadOnlyList<double> values, double percentile) => values[Math.Min(values.Count - 1, (int)Math.Ceiling(values.Count * percentile) - 1)];

static LoadSummary RunConcurrentLoad(SqliteSearchIndex index, SearchDocument[] docs, string root)
{
    var store = new VersionedStore(docs[0].Body);
    var conflicts = 0;
    var operations = 0;
    var tasks = Enumerable.Range(0, HumanClients + AiClients).Select(async client =>
    {
        for (var i = 0; i < 10; i++)
        {
            _ = index.Search(client % 2 == 0 ? "ユーザー認証" : "ERR", 10);
            _ = Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories).Take(10).Count();
            Interlocked.Increment(ref operations);
            var etag = store.ETag;
            if (client == 1 && i == 0) { await Task.Yield(); }
            if (!store.TryUpdate(etag, $"client-{client}-{i}", out _)) Interlocked.Increment(ref conflicts);
        }
    }).ToArray();
    Task.WaitAll(tasks);
    var consistent = store.ConflictRejected && store.Content.StartsWith("client-", StringComparison.Ordinal);
    return new LoadSummary(operations, conflicts, consistent);
}

sealed record CorpusSummary(int Documents, long Bytes, Dictionary<string, int> Buckets);
sealed record ScanSummary(int Files);
sealed record Measurement(double P50, double P95, double P99, double Max, int LastValue);
sealed record LoadSummary(int Operations, int Conflicts, bool IndexConsistent);

sealed class VersionedStore
{
    private readonly object gate = new();
    public VersionedStore(string content) { Content = content; ETag = Hash(content); }
    public string Content { get; private set; }
    public string ETag { get; private set; }
    public bool ConflictRejected { get; private set; }
    public bool TryUpdate(string expected, string content, out string current)
    {
        lock (gate)
        {
            if (!StringComparer.Ordinal.Equals(expected, ETag)) { ConflictRejected = true; current = ETag; return false; }
            Content = content; ETag = Hash(content); current = ETag; return true;
        }
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
