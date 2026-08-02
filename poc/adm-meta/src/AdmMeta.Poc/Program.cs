using System.Security.Cryptography;
using System.Text;
using Adm.Metadata;

var root = Path.Combine(Path.GetTempPath(), $"adm-meta-poc-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);
try
{
    var markdown = Encoding.UTF8.GetBytes("# P0-007 sample\n\nOriginal content\n");
    var inputPath = Path.Combine(root, "docs", "sample.md");
    Directory.CreateDirectory(Path.GetDirectoryName(inputPath)!);
    File.WriteAllBytes(inputPath, markdown);
    var before = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(inputPath)));

    var store = new MetadataStore(Path.Combine(root, ".adm-meta"));
    store.Initialize();
    var first = store.CreateDocument("docs/sample.md", "test_case", markdown);
    var second = store.CreateDocument("docs/second.md", "test_case", markdown);
    var design = store.CreateDocument("design/overview.md", "design", Encoding.UTF8.GetBytes("# Design"));

    var tasks = Enumerable.Range(0, 32).Select(_ => Task.Run(() => store.CreateDocument("docs/concurrent.md", "test_case", markdown))).ToArray();
    var concurrent = await Task.WhenAll(tasks);
    var all = new[] { first, second, design }.Concat(concurrent).ToArray();
    Require(all.Select(item => item.DocumentId).Distinct(StringComparer.Ordinal).Count() == all.Length, "ULID collision detected");
    Require(concurrent.Select(item => item.SequenceNumber).Distinct().Count() == concurrent.Length, "sequence collision detected");
    Require(concurrent.Select(item => item.SequenceNumber).OrderBy(item => item).SequenceEqual(Enumerable.Range(3, 32)), "sequence allocation is not contiguous");

    store.SaveUserState(new UserDocumentState(1, "user-a", first.DocumentId, true, "adr", DateTimeOffset.UtcNow));
    store.SaveUserState(new UserDocumentState(1, "user-b", first.DocumentId, false, null, DateTimeOffset.UtcNow));
    var userA = Path.Combine(root, ".adm-meta", "users", "user-a", "documents", first.DocumentId + ".json");
    var userB = Path.Combine(root, ".adm-meta", "users", "user-b", "documents", first.DocumentId + ".json");
    Require(File.Exists(userA) && File.Exists(userB), "user state separation missing");
    Require(File.ReadAllText(userA).Contains("classification_override", StringComparison.Ordinal), "manual classification overlay missing");

    var renamed = store.FindRenameCandidates("docs/renamed.md", markdown);
    Require(renamed.Count == all.Length - 1 && renamed.All(item => item.RequiresConfirmation), "rename candidates are not confirmation-gated");
    var changed = store.FindRenameCandidates("docs/changed.md", Encoding.UTF8.GetBytes("changed"));
    Require(changed.Count == 0, "content-changed document was treated as rename");

    var after = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(inputPath)));
    Require(before == after, "source Markdown changed");
    Require(File.Exists(Path.Combine(root, ".adm-meta", "project.json")), "project metadata missing");
    Require(File.Exists(Path.Combine(root, ".adm-meta", "documents", first.DocumentId + ".json")), "document metadata missing");
    Console.WriteLine($"PASS documents={all.Length} ulid_unique=true sequence_unique=true user_isolation=true rename_confirmation=true input_unchanged=true");
    Console.WriteLine($"SDK={Environment.Version} metadata_root=.adm-meta");
    return;
}
finally
{
    Directory.Delete(root, true);
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
