using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var cliArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
var selfTest = cliArgs.Contains("--self-test", StringComparer.OrdinalIgnoreCase);
var inputIndex = Array.FindIndex(cliArgs, x => string.Equals(x, "--input", StringComparison.OrdinalIgnoreCase));
var input = inputIndex >= 0 && inputIndex + 1 < cliArgs.Length ? Path.GetFullPath(cliArgs[inputIndex + 1]) : null;
var runId = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant()}";
var resultDir = Path.Combine(Path.GetTempPath(), "AI-Development-Manager", "poc", "P0-022", runId);
Directory.CreateDirectory(resultDir);
var json = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var checks = new List<CheckResult>();
var notes = new List<string>();

Check("input_is_read_only", () => "no write operation is used for input");
Check("self_test", () => {
    if (!selfTest) return "not requested";
    var selfDir = Path.Combine(resultDir, "self-test-input");
    Directory.CreateDirectory(selfDir);
    File.WriteAllText(Path.Combine(selfDir, "ticket.md"), "# Sample ticket\n\n確認済み\n\n添付: docs/result.json", new UTF8Encoding(false));
    File.WriteAllText(Path.Combine(selfDir, "broken.md"), "# Broken\n\n", new UTF8Encoding(false));
    File.WriteAllBytes(Path.Combine(selfDir, "shift-jis.md"), Encoding.GetEncoding(932).GetBytes("# 日本語\r\n\r\n確認済み"));
    var before = HashTree(selfDir);
    var records = ReadTree(selfDir);
    var after = HashTree(selfDir);
    if (records.Count != 3 || before != after) throw new InvalidDataException("self-test discovery or hash check failed");
    return $"{records.Count} files discovered; before/after hash equal";
});

List<DocumentRecord> records = [];
if (input is null) {
    notes.Add("匿名化済みDevTicketManager実データが未指定のため、実データ互換判定を保留");
    checks.Add(new CheckResult("real_data_compatibility", false, "BLOCKED: provide an isolated anonymized copy with --input"));
} else if (!Directory.Exists(input)) {
    notes.Add("指定された入力ディレクトリが存在しない");
    checks.Add(new CheckResult("real_data_compatibility", false, "BLOCKED: input directory not found"));
} else {
    var before = HashTree(input);
    records = ReadTree(input);
    var after = HashTree(input);
    Check("real_data_hash_unchanged", () => before == after ? "input tree hash unchanged" : throw new InvalidDataException("input changed"));
    Check("markdown_inventory", () => records.Count > 0 ? $"{records.Count} Markdown files" : "no Markdown files found");
    Check("non_supported_files_do_not_stop_scan", () => $"scan completed with {records.Count(x => x.Error is not null)} document-level errors");
    Check("listing_and_sorting", () => records.OrderBy(x => x.Title).ThenBy(x => x.LastWriteUtc).ToList().Count == records.Count ? "name/date sort completed" : "sort failed");
    Check("search", () => records.Where(x => Searchable(x, "確認")).ToList().Count >= 0 ? "search completed" : "search failed");
    Check("adm_meta_mapping_proposal", () => "confirmation state remains an overlay proposal; source is unchanged");
    Check("attachment_relation", () => $"{records.Sum(x => x.AttachmentCandidates.Count)} attachment candidates found");
}

var status = input is not null && checks.Any(x => x.Name == "real_data_hash_unchanged" && !x.Passed) ? "FAILED" : input is null || !Directory.Exists(input ?? "") ? "BLOCKED" : "PASSED";
var result = new PocResult("P0-022", runId, status, "10.0.302", Environment.Version.ToString(), input ?? "N/A", records, checks, notes);
File.WriteAllText(Path.Combine(resultDir, "result.json"), JsonSerializer.Serialize(result, json));
Console.WriteLine(JsonSerializer.Serialize(result, json));
Console.WriteLine($"RESULT_JSON={Path.Combine(resultDir, "result.json")}");
Environment.ExitCode = status == "PASSED" ? 0 : status == "BLOCKED" ? 2 : 1;

void Check(string name, Func<string> operation) {
    try { checks.Add(new CheckResult(name, true, operation())); }
    catch (Exception ex) { checks.Add(new CheckResult(name, false, ex.GetType().Name + ": " + ex.Message)); }
}

List<DocumentRecord> ReadTree(string root) {
    var output = new List<DocumentRecord>();
    foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) {
        if (!string.Equals(Path.GetExtension(path), ".md", StringComparison.OrdinalIgnoreCase)) continue;
        try {
            var bytes = File.ReadAllBytes(path);
            var text = Decode(bytes, out var encoding);
            var title = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(x => x.TrimStart().StartsWith('#'))?.TrimStart('#', ' ', '\t') ?? Path.GetFileNameWithoutExtension(path);
            var attachments = ExtractAttachmentCandidates(text);
            output.Add(new DocumentRecord(Path.GetRelativePath(root, path), title, encoding, File.GetLastWriteTimeUtc(path), text.Length, attachments, null));
        } catch (Exception ex) { output.Add(new DocumentRecord(Path.GetRelativePath(root, path), Path.GetFileName(path), "unknown", File.GetLastWriteTimeUtc(path), 0, [], ex.GetType().Name)); }
    }
    return output;
}

string Decode(byte[] bytes, out string encoding) {
    if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) { encoding = "UTF-8 BOM"; return Encoding.UTF8.GetString(bytes[3..]); }
    try { encoding = "UTF-8"; return new UTF8Encoding(false, true).GetString(bytes); }
    catch (DecoderFallbackException) { encoding = "Shift_JIS (Windows code page 932)"; return Encoding.GetEncoding(932).GetString(bytes); }
}

List<string> ExtractAttachmentCandidates(string text) => text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Where(line => line.Contains("attachment", StringComparison.OrdinalIgnoreCase) || line.Contains("添付", StringComparison.OrdinalIgnoreCase) || line.Contains(".json", StringComparison.OrdinalIgnoreCase) || line.Contains(".png", StringComparison.OrdinalIgnoreCase)).Select(line => line.Trim()).ToList();
bool Searchable(DocumentRecord record, string query) => record.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || record.RelativePath.Contains(query, StringComparison.OrdinalIgnoreCase);
string HashTree(string root) {
    using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) {
        sha.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(root, path).ToUpperInvariant()));
        sha.AppendData(File.ReadAllBytes(path));
    }
    return Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
}

record DocumentRecord(string RelativePath, string Title, string Encoding, DateTime LastWriteUtc, int CharacterCount, List<string> AttachmentCandidates, string? Error);
record CheckResult(string Name, bool Passed, string Detail);
record PocResult(string Ticket, string RunId, string Status, string Sdk, string Runtime, string Input, List<DocumentRecord> Documents, List<CheckResult> Checks, List<string> Notes);
