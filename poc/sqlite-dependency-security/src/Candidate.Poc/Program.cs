using System.Text.Json;
using Microsoft.Data.Sqlite;

var result = Probe("candidate", "Microsoft.Data.Sqlite.Core 10.0.10 + SQLitePCLRaw.bundle_e_sqlite3 3.0.3");
var output = Path.Combine(Path.GetTempPath(), "AI-Development-Manager", "poc", "P0-026", result.RunId);
Directory.CreateDirectory(output);
File.WriteAllText(Path.Combine(output, "candidate-result.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"RESULT_JSON={Path.Combine(output, "candidate-result.json")}");
Environment.ExitCode = result.Passed ? 0 : 1;

ProbeResult Probe(string name, string dependency)
{
    SQLitePCL.Batteries_V2.Init();
    using var connection = new SqliteConnection("Data Source=:memory:"); connection.Open();
    var sqliteVersion = Scalar(connection, "select sqlite_version();");
    Execute(connection, "CREATE VIRTUAL TABLE docs USING fts5(title, body, tokenize='unicode61'); INSERT INTO docs VALUES ('日本語検索', 'ユーザー認証 ERR-1234'); INSERT INTO docs VALUES ('部分一致', 'trigram fallback');");
    var fts = Scalar(connection, "select count(*) from docs where docs match 'ユーザー認証';") == "1";
    var trigram = false; try { Execute(connection, "CREATE VIRTUAL TABLE tri USING fts5(body, tokenize='trigram'); INSERT INTO tri VALUES ('abcdef');"); trigram = Scalar(connection, "select count(*) from tri where tri match 'bcd';") == "1"; } catch { }
    var native = Directory.EnumerateFiles(AppContext.BaseDirectory, "e_sqlite3.dll", SearchOption.AllDirectories).Select(Path.GetFullPath).ToArray();
    return new ProbeResult(name, dependency, $"{DateTime.UtcNow:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}", "10.0.302", Environment.Version.ToString(), sqliteVersion, native, fts, trigram, fts && trigram);
}
static void Execute(SqliteConnection connection, string sql) { using var command = connection.CreateCommand(); command.CommandText = sql; command.ExecuteNonQuery(); }
static string Scalar(SqliteConnection connection, string sql) { using var command = connection.CreateCommand(); command.CommandText = sql; return command.ExecuteScalar()?.ToString() ?? string.Empty; }
record ProbeResult(string Configuration, string Dependency, string RunId, string Sdk, string Runtime, string SqliteVersion, string[] NativeSqliteFiles, bool Fts5Unicode61, bool Fts5Trigram, bool Passed);
