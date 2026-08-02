using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

const int DocumentCount = 10_000;
const int Seed = 15_015;
const int Warmups = 1;
const int Samples = 5;
var runId = $"{DateTime.UtcNow:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}";
var outputRoot = Path.Combine(Path.GetTempPath(), "AI-Development-Manager", "poc", "P0-025", runId);
Directory.CreateDirectory(outputRoot);
var json = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
Console.WriteLine($"P0-025 run={runId} SDK=10.0.302 Runtime={Environment.Version}");
var corpus = GenerateCorpus(Path.Combine(outputRoot, "corpus"));
var documents = LoadDocuments(corpus.Root);
var terms = new[]
{
    new QueryCase("standard_japanese", "ユーザー認証", "high"),
    new QueryCase("medium_japanese", "保存処理", "medium"),
    new QueryCase("error_code", "ERR-1000", "medium"),
    new QueryCase("identifier_path", "DOC-00001", "low"),
    new QueryCase("partial_path", "DOC-000", "partial"),
    new QueryCase("partial_japanese", "証と保存", "partial"),
    new QueryCase("no_match", "存在しない検索語", "none")
};
var configurations = new[] { "unicode61_external", "scoped_trigram", "full_trigram_control" };
var results = new List<ConfigurationResult>();
foreach (var configuration in configurations)
{
    var databasePath = Path.Combine(outputRoot, configuration + ".db");
    var built = BuildIndex(databasePath, configuration, documents);
    var queries = terms.Select(term => MeasureQuery(built, term)).ToArray();
    var mutations = VerifyMutations(built, documents);
    var integrity = IntegrityCheck(built.Connection);
    var size = new FileInfo(databasePath).Length;
    results.Add(new ConfigurationResult(configuration, built.BuildMs, size, queries, mutations, integrity));
    built.Dispose();
}

var standard = results.Single(x => x.Name == "unicode61_external");
var checks = new List<CheckResult>
{
    Check("standard_search_p95_under_500ms", results.SelectMany(x => x.Queries.Where(q => q.Category is "high" or "medium")).Max(q => q.P95Ms) <= 500, results.SelectMany(x => x.Queries.Where(q => q.Category is "high" or "medium")).Max(q => q.P95Ms)),
    Check("wide_search_p95_under_1000ms", standard.Queries.Single(q => q.Name == "standard_japanese").P95Ms <= 1000, standard.Queries.Single(q => q.Name == "standard_japanese").P95Ms),
    Check("unicode61_quality", standard.Queries.All(q => q.QualityPassed), "unicode61 query quality"),
    Check("scoped_trigram_path_partial_quality", results.Single(x => x.Name == "scoped_trigram").Queries.Single(q => q.Name == "partial_path").QualityPassed, "path partial"),
    Check("full_trigram_body_partial_quality", results.Single(x => x.Name == "full_trigram_control").Queries.Single(q => q.Name == "partial_japanese").QualityPassed, "body partial"),
    Check("mutation_consistency", results.All(x => x.Mutations.All(m => m.Passed)), "update/delete/rename/rebuild"),
    Check("integrity_check", results.All(x => x.IntegrityPassed), "PRAGMA integrity_check"),
    Check("reduced_trigram_scope", results.Single(x => x.Name == "scoped_trigram").Bytes < results.Single(x => x.Name == "full_trigram_control").Bytes, "scoped trigram is smaller than full control")
};
var result = new PocResult("P0-025", runId, corpus, "10.0.302", Environment.Version.ToString(), results, checks, checks.All(x => x.Passed));
File.WriteAllText(Path.Combine(outputRoot, "result.json"), JsonSerializer.Serialize(result, json));
File.WriteAllText(Path.Combine(outputRoot, "run.log"), $"P0-025 {runId}\nSDK 10.0.302\nRuntime {Environment.Version}\n");
Console.WriteLine(JsonSerializer.Serialize(result, json));
Console.WriteLine($"RESULT_JSON={Path.Combine(outputRoot, "result.json")}");
Environment.ExitCode = result.Passed ? 0 : 1;

BuildState BuildIndex(string path, string name, IReadOnlyList<SearchDocument> documents)
{
    var connection = new SqliteConnection($"Data Source={path}");
    connection.Open();
    Execute(connection, "PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL;");
    Execute(connection, "CREATE TABLE documents (id INTEGER PRIMARY KEY, path TEXT NOT NULL, file_name TEXT NOT NULL, heading TEXT NOT NULL, body TEXT NOT NULL, status TEXT NOT NULL, related_id TEXT NOT NULL);");
    foreach (var document in documents) InsertDocument(connection, document);
    var tokenizer = name == "full_trigram_control" ? "trigram" : "unicode61 remove_diacritics 2";
    Execute(connection, $"CREATE VIRTUAL TABLE fts USING fts5(path, file_name, heading, body, status UNINDEXED, related_id UNINDEXED, content='documents', content_rowid='id', tokenize='{tokenizer}');");
    var watch = Stopwatch.StartNew();
    Execute(connection, "INSERT INTO fts(fts) VALUES ('rebuild');");
    if (name == "scoped_trigram")
    {
        Execute(connection, "CREATE VIRTUAL TABLE partial_fts USING fts5(path, file_name, heading, content='documents', content_rowid='id', tokenize='trigram');");
        Execute(connection, "INSERT INTO partial_fts(partial_fts) VALUES ('rebuild');");
    }
    Execute(connection, "INSERT INTO fts(fts) VALUES ('optimize');");
    watch.Stop();
    return new BuildState(name, watch.Elapsed.TotalMilliseconds, connection, documents);
}

QueryResult MeasureQuery(BuildState state, QueryCase query)
{
    for (var i = 0; i < Warmups; i++) _ = Search(state, query);
    var values = new List<double>(); SearchResult? last = null;
    for (var i = 0; i < Samples; i++) { var watch = Stopwatch.StartNew(); last = Search(state, query); watch.Stop(); values.Add(watch.Elapsed.TotalMilliseconds); }
    values.Sort();
    var expected = state.DocumentsFor(query.Term, query.Name == "partial_japanese" ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
    var quality = query.Name switch
    {
        "partial_japanese" => last!.Ids.Count > 0 && state.Name == "full_trigram_control",
        "partial_path" => last!.Ids.Count > 0 && state.Name is "scoped_trigram" or "full_trigram_control",
        "no_match" => last!.Ids.Count == 0,
        _ => expected.Count == 0 ? last!.Ids.Count == 0 : last!.Ids.Count > 0 && last!.Ids.All(expected.Contains)
    };
    return new QueryResult(query.Name, query.Category, Percentile(values, .50), Percentile(values, .95), Percentile(values, .99), last!.Ids.Count, last.Ids.Take(5).ToArray(), last.Snippet, quality);
}

SearchResult Search(BuildState state, QueryCase query)
{
    var ids = new List<int>(); var snippets = new List<string>();
    using var command = state.Connection.CreateCommand();
    var table = state.Name == "scoped_trigram" && query.Name == "partial_path" ? "partial_fts" : "fts";
    command.CommandText = $"SELECT d.id, snippet({table}, 0, '[', ']', '…', 18) FROM {table} JOIN documents d ON d.id = {table}.rowid WHERE {table} MATCH $term ORDER BY bm25({table}), d.path, d.id LIMIT 1000";
    command.Parameters.AddWithValue("$term", '"' + query.Term.Replace("\"", "\"\"", StringComparison.Ordinal) + '"');
    using var reader = command.ExecuteReader();
    while (reader.Read()) { ids.Add(reader.GetInt32(0)); snippets.Add(reader.IsDBNull(1) ? string.Empty : reader.GetString(1)); }
    return new SearchResult(ids, snippets.FirstOrDefault() ?? string.Empty);
}

MutationResult[] VerifyMutations(BuildState state, IReadOnlyList<SearchDocument> documents)
{
    var target = documents[0];
    Execute(state.Connection, "UPDATE documents SET body = $body WHERE id = $id", ("$body", "更新本文 ERR-9999"), ("$id", target.Id));
    Execute(state.Connection, "INSERT INTO fts(fts) VALUES ('rebuild');");
    var update = Search(state, new QueryCase("update", "ERR-9999", "low")).Ids.Contains(target.Id);
    Execute(state.Connection, "UPDATE documents SET path = 'archive/DOC-00001-renamed.md', file_name = 'DOC-00001-renamed.md' WHERE id = $id", ("$id", target.Id));
    Execute(state.Connection, "INSERT INTO fts(fts) VALUES ('rebuild');");
    var rename = Search(state, new QueryCase("rename", "DOC-00001-renamed", "low")).Ids.Contains(target.Id);
    Execute(state.Connection, "DELETE FROM documents WHERE id = $id", ("$id", target.Id));
    Execute(state.Connection, "INSERT INTO fts(fts) VALUES ('rebuild');");
    var delete = !Search(state, new QueryCase("delete", "DOC-00001-renamed", "low")).Ids.Contains(target.Id);
    return new[] { new MutationResult("update", update), new MutationResult("rename", rename), new MutationResult("delete", delete) };
}

bool IntegrityCheck(SqliteConnection connection)
{
    using var command = connection.CreateCommand(); command.CommandText = "PRAGMA integrity_check;"; return string.Equals(command.ExecuteScalar()?.ToString(), "ok", StringComparison.OrdinalIgnoreCase);
}

CorpusSummary GenerateCorpus(string root)
{
    Directory.CreateDirectory(root); var random = new Random(Seed); long bytes = 0;
    for (var i = 0; i < DocumentCount; i++)
    {
        var type = i < 5000 ? "ticket" : i < 7000 ? "test_case" : i < 8500 ? "test_result" : i < 9500 ? "design" : "adr";
        var target = i < 7000 ? random.Next(1024, 16 * 1024 + 1) : i < 9500 ? random.Next(16 * 1024, 256 * 1024 + 1) : random.Next(256 * 1024, 2 * 1024 * 1024 + 1);
        var path = Path.Combine(root, type, $"DOC-{i + 1:00000}.md"); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var text = new StringBuilder($"# ユーザー認証 性能文書 {i + 1}\n\n"); var unit = $"本文 {i + 1}: ユーザー認証と保存処理の検証。ERR-{1000 + i % 100:0000}。識別子 DOC-{i + 1:00000}。 ";
        var currentBytes = Encoding.UTF8.GetByteCount(text.ToString()); var unitBytes = Encoding.UTF8.GetByteCount(unit);
        while (currentBytes < target) { text.Append(unit); currentBytes += unitBytes; }
        File.WriteAllText(path, text.ToString(), new UTF8Encoding(false)); bytes += new FileInfo(path).Length;
    }
    return new CorpusSummary(DocumentCount, bytes, root);
}

static void InsertDocument(SqliteConnection connection, SearchDocument document)
{
    using var command = connection.CreateCommand(); command.CommandText = "INSERT INTO documents VALUES ($id,$path,$file,$heading,$body,$status,$related)";
    command.Parameters.AddWithValue("$id", document.Id); command.Parameters.AddWithValue("$path", document.Path); command.Parameters.AddWithValue("$file", document.FileName); command.Parameters.AddWithValue("$heading", document.Heading); command.Parameters.AddWithValue("$body", document.Body); command.Parameters.AddWithValue("$status", "active"); command.Parameters.AddWithValue("$related", document.Type); command.ExecuteNonQuery();
}
static void Execute(SqliteConnection connection, string sql, params (string Name, object Value)[] parameters) { using var command = connection.CreateCommand(); command.CommandText = sql; foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value); command.ExecuteNonQuery(); }
static double Percentile(IReadOnlyList<double> values, double percentile) => values[Math.Min(values.Count - 1, Math.Max(0, (int)Math.Ceiling(values.Count * percentile) - 1))];
static CheckResult Check(string name, bool passed, object detail) => new(name, passed, detail.ToString() ?? string.Empty);
static IReadOnlyList<SearchDocument> LoadDocuments(string root) => Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories).Select((path, index) => new SearchDocument(index + 1, Path.GetRelativePath(root, path).Replace('\\', '/'), Path.GetFileName(path), Path.GetFileNameWithoutExtension(path), File.ReadAllText(path), "active", Path.GetDirectoryName(Path.GetRelativePath(root, path)) ?? string.Empty)).ToArray();

record SearchDocument(int Id, string Path, string FileName, string Heading, string Body, string Status, string Type);
record QueryCase(string Name, string Term, string Category);
record SearchResult(IReadOnlyList<int> Ids, string Snippet);
record QueryResult(string Name, string Category, double P50Ms, double P95Ms, double P99Ms, int Hits, IReadOnlyList<int> TopIds, string Snippet, bool QualityPassed);
record MutationResult(string Name, bool Passed);
record CheckResult(string Name, bool Passed, string Detail);
record CorpusSummary(int Documents, long Bytes, string Root);
record ConfigurationResult(string Name, double BuildMs, long Bytes, QueryResult[] Queries, MutationResult[] Mutations, bool IntegrityPassed);
record PocResult(string Ticket, string RunId, CorpusSummary Corpus, string Sdk, string Runtime, List<ConfigurationResult> Configurations, List<CheckResult> Checks, bool Passed);

sealed class BuildState : IDisposable
{
    public BuildState(string name, double buildMs, SqliteConnection connection, IReadOnlyList<SearchDocument> documents) { Name = name; BuildMs = buildMs; Connection = connection; Documents = documents; }
    public string Name { get; }
    public double BuildMs { get; }
    public SqliteConnection Connection { get; }
    public IReadOnlyList<SearchDocument> Documents { get; }
    public IReadOnlySet<int> DocumentsFor(string term, StringComparison comparison) => Documents.Where(x => x.Path.Contains(term, comparison) || x.FileName.Contains(term, comparison) || x.Heading.Contains(term, comparison) || x.Body.Contains(term, comparison)).Select(x => x.Id).ToHashSet();
    public void Dispose() { Connection.Close(); Connection.Dispose(); }
}
