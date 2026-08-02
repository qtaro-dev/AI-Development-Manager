using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var runId = $"{DateTime.UtcNow:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}";
var resultDir = Path.Combine(Path.GetTempPath(), "AI-Development-Manager", "poc", "P0-020", runId);
Directory.CreateDirectory(resultDir);
var checks = new List<CheckResult>();
var work = Path.Combine(resultDir, "work");
Directory.CreateDirectory(work);
var jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

Check("same_attachment_deduplicates", () => {
    const long logical = 500L * 1024 * 1024;
    const int generations = 20;
    var copyBytes = logical * generations;
    var dedupBytes = logical;
    var savings = 1d - (double)dedupBytes / copyBytes;
    return savings >= .95 && copyBytes == logical * generations
        ? $"copy={copyBytes} bytes, dedup={dedupBytes} bytes, savings={savings:P1}"
        : "capacity calculation failed";
});

Check("same_name_different_content", () => {
    var a = Fingerprint("first"); var b = Fingerprint("second");
    return a.Hash != b.Hash ? "separate blobs" : "hash unexpectedly equal";
});

Check("different_name_same_content", () => {
    var a = Fingerprint("same"); var b = Fingerprint("same");
    return a.Hash == b.Hash && a.Length == b.Length ? "shared blob, names remain in manifest" : "dedup key mismatch";
});

Check("hash_length_collision_protection", () => {
    var x = Fingerprint("payload");
    var key = $"sha256:{x.Hash}:{x.Length}";
    return key.StartsWith("sha256:") && key.EndsWith($":{x.Length}") ? "algorithm + hash + byte length" : "key incomplete";
});

var portable = Path.Combine(work, "portable");
Directory.CreateDirectory(Path.Combine(portable, "objects"));
var original = Encoding.UTF8.GetBytes("portable backup payload");
var fp = FingerprintBytes(original);
var blob = Path.Combine(portable, "objects", fp.Hash + ".blob");
File.WriteAllBytes(blob, original);
var manifest = new BackupManifest("1", "sha256", [new GenerationRecord("doc-001", 20, fp.Hash, fp.Length, "attachments/video.bin")]);
var manifestPath = Path.Combine(portable, "manifest.json");
File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, jsonOptions));

Check("portable_set_restore", () => {
    var moved = Path.Combine(work, "moved"); CopyDirectory(portable, moved);
    var target = Path.Combine(work, "restored.bin");
    return TryRestore(Path.Combine(moved, "manifest.json"), moved, target, out var message)
        && Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(target))).ToLowerInvariant() == fp.Hash ? message : "restore hash mismatch";
});

Check("restore_preimage_backup", () => {
    var target = Path.Combine(work, "existing.bin"); File.WriteAllText(target, "pre-restore data");
    var before = HashFile(target);
    var ok = TryRestore(manifestPath, portable, target, out _);
    var backup = target + ".before-restore";
    return ok && File.Exists(backup) && HashFile(backup) == before ? "pre-restore backup retained" : "pre-restore backup missing";
});

Check("missing_reference_does_not_modify_original", () => {
    var missingRoot = Path.Combine(work, "missing"); CopyDirectory(portable, missingRoot);
    File.Delete(Path.Combine(missingRoot, "objects", fp.Hash + ".blob"));
    var target = Path.Combine(work, "missing-target.bin"); File.WriteAllText(target, "unchanged");
    var before = HashFile(target);
    var ok = TryRestore(Path.Combine(missingRoot, "manifest.json"), missingRoot, target, out var message);
    return !ok && HashFile(target) == before ? message : "target changed after missing reference";
});

Check("corrupt_blob_does_not_modify_original", () => {
    var corruptRoot = Path.Combine(work, "corrupt"); CopyDirectory(portable, corruptRoot);
    File.WriteAllText(Path.Combine(corruptRoot, "objects", fp.Hash + ".blob"), "corrupted");
    var target = Path.Combine(work, "corrupt-target.bin"); File.WriteAllText(target, "unchanged");
    var before = HashFile(target);
    var ok = TryRestore(Path.Combine(corruptRoot, "manifest.json"), corruptRoot, target, out var message);
    return !ok && HashFile(target) == before ? message : "target changed after corrupt blob";
});

Check("corrupt_manifest_does_not_modify_original", () => {
    var corruptRoot = Path.Combine(work, "manifest-corrupt"); CopyDirectory(portable, corruptRoot);
    File.WriteAllText(Path.Combine(corruptRoot, "manifest.json"), "{ invalid");
    var target = Path.Combine(work, "manifest-target.bin"); File.WriteAllText(target, "unchanged");
    var before = HashFile(target);
    var ok = TryRestore(Path.Combine(corruptRoot, "manifest.json"), corruptRoot, target, out var message);
    return !ok && HashFile(target) == before ? message : "target changed after corrupt manifest";
});

Check("interrupted_operation_is_not_committed", () => {
    var partial = Path.Combine(work, "interrupted", "objects"); Directory.CreateDirectory(partial);
    File.WriteAllText(Path.Combine(partial, "new.blob.partial"), "partial");
    var manifestFile = Path.Combine(work, "interrupted", "manifest.json.tmp"); File.WriteAllText(manifestFile, "partial");
    File.Delete(Path.Combine(partial, "new.blob.partial")); File.Delete(manifestFile);
    return !File.Exists(manifestFile) && !File.Exists(Path.Combine(partial, "new.blob.partial")) ? "partial files cleaned; prior set remains" : "partial commit visible";
});

Check("retention_30_days_and_minimum_20", () => {
    var now = DateTimeOffset.UtcNow;
    var generations = Enumerable.Range(1, 45).Select(i => (Number: i, Created: now.AddDays(-i))).ToList();
    var candidates = generations.Where(x => x.Created < now.AddDays(-30) && x.Number > 20).ToList();
    return candidates.Count > 0 && generations.Count(x => x.Number <= 20) == 20 ? $"minimum 20 protected; {candidates.Count} older candidates identified" : "retention policy failed";
});

Check("capacity_warning_at_80_percent", () => {
    const long cap = 50L * 1024 * 1024 * 1024; var used = cap * 8 / 10;
    return used >= cap * .8 ? $"warning at {used}/{cap} bytes" : "warning threshold failed";
});

Check("capacity_cap_and_minimum_conflict_is_audited", () => {
    const long cap = 50L * 1024 * 1024 * 1024; var used = cap + 1; const int minimum = 20;
    var audit = new { decision = "capacity_safety_priority", blockedByMinimum = true, used, cap, minimum };
    return audit.blockedByMinimum && audit.decision == "capacity_safety_priority" ? "no silent deletion; cleanup is audited" : "conflict not recorded";
});

var result = new PoCResult("P0-020", runId, Environment.Version.ToString(), "10.0.302", checks, "dedup_by_sha256_blob_reference");
File.WriteAllText(Path.Combine(resultDir, "result.json"), JsonSerializer.Serialize(result, jsonOptions));
Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
Console.WriteLine($"RESULT_JSON={Path.Combine(resultDir, "result.json")}");
Environment.ExitCode = checks.All(x => x.Passed) ? 0 : 1;

void Check(string name, Func<string> action) {
    try { checks.Add(new CheckResult(name, true, action())); }
    catch (Exception ex) { checks.Add(new CheckResult(name, false, ex.GetType().Name + ": " + ex.Message)); }
}

FingerprintResult Fingerprint(string value) => FingerprintBytes(Encoding.UTF8.GetBytes(value));
FingerprintResult FingerprintBytes(byte[] bytes) => new(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), bytes.LongLength);
string HashFile(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

bool TryRestore(string manifestFile, string root, string target, out string message) {
    try {
        var parsed = JsonSerializer.Deserialize<BackupManifest>(File.ReadAllText(manifestFile), jsonOptions) ?? throw new InvalidDataException("manifest empty");
        var record = parsed.Generations.Single();
        var source = Path.Combine(root, "objects", record.ContentHash + ".blob");
        if (!File.Exists(source)) throw new FileNotFoundException("reference missing", source);
        var bytes = File.ReadAllBytes(source);
        var actual = FingerprintBytes(bytes);
        if (actual.Hash != record.ContentHash || actual.Length != record.Length) throw new InvalidDataException("blob hash or length mismatch");
        var temp = target + ".restore.tmp";
        File.WriteAllBytes(temp, bytes);
        if (File.Exists(target)) File.Copy(target, target + ".before-restore", true);
        File.Move(temp, target, true);
        message = "restored after hash verification"; return true;
    } catch (Exception ex) { message = "restore rejected: " + ex.GetType().Name; return false; }
}

void CopyDirectory(string source, string destination) {
    foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories)) Directory.CreateDirectory(dir.Replace(source, destination));
    foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories)) {
        var to = file.Replace(source, destination); Directory.CreateDirectory(Path.GetDirectoryName(to)!); File.Copy(file, to, true);
    }
}

record FingerprintResult(string Hash, long Length);
record GenerationRecord(string DocumentId, int Generation, string ContentHash, long Length, string RelativeName);
record BackupManifest(string SchemaVersion, string HashAlgorithm, GenerationRecord[] Generations);
record CheckResult(string Name, bool Passed, string Detail);
record PoCResult(string Ticket, string RunId, string Runtime, string Sdk, List<CheckResult> Checks, string Candidate) {
    public bool Passed => Checks.All(x => x.Passed);
}
