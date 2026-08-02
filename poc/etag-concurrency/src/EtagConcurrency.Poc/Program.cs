using System.Text;
using Adm.Api.Concurrency;

var root = Path.Combine(Path.GetTempPath(), $"etag-concurrency-poc-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);
var path = Path.Combine(root, "sample.md");
var backups = Path.Combine(root, "backups");
var audit = Path.Combine(root, "audit", "conflicts.jsonl");
var original = Encoding.UTF8.GetBytes("# Original\n");
var clientAContent = Encoding.UTF8.GetBytes("# Client A\n");
var clientBContent = Encoding.UTF8.GetBytes("# Client B\n");
var externalContent = Encoding.UTF8.GetBytes("# External editor\n");

try
{
    File.WriteAllBytes(path, original);
    var store = new ConcurrencyStore(backups);

    var clientARead = store.Read(path);
    var clientBRead = store.Read(path);
    Require(clientARead.ETag == clientBRead.ETag, "clients did not read the same ETag");

    var success = store.Update(path, clientARead.ETag, clientAContent);
    Require(success.StatusCode == 200 && !success.NoOp, "correct If-Match update failed");
    Require(store.Read(path).Content.SequenceEqual(clientAContent), "successful content mismatch");

    var conflict = store.Update(path, clientBRead.ETag, clientBContent);
    Require(conflict.StatusCode == 409 && conflict.Conflict is not null, "stale ETag was not rejected");
    Require(conflict.Conflict!.LatestContent == Encoding.UTF8.GetString(clientAContent), "latest content missing");
    Require(conflict.Conflict.SubmittedContent == Encoding.UTF8.GetString(clientBContent), "submitted content missing");
    Require(conflict.Conflict.DiffEndpoint.Contains("diff", StringComparison.Ordinal), "diff endpoint missing");
    ConcurrencyStore.WriteConflictAudit(audit, conflict.Conflict);

    var missing = store.Update(path, null, clientBContent);
    Require(missing.StatusCode == 428, "missing If-Match was not rejected");

    var sameContentResend = store.Update(path, clientBRead.ETag, clientAContent);
    Require(sameContentResend.StatusCode == 200 && sameContentResend.NoOp, "same-content resend was not idempotent");

    var externalRead = store.Read(path);
    File.WriteAllBytes(path, externalContent);
    var externalConflict = store.Update(path, externalRead.ETag, clientBContent);
    Require(externalConflict.StatusCode == 409, "external editor conflict was not detected");
    Require(externalConflict.Conflict!.LatestContent == Encoding.UTF8.GetString(externalContent), "external latest content missing");

    var current = store.Read(path);
    var etagRecomputed = ConcurrencyStore.CreateETag(current.Content);
    Require(current.ETag == etagRecomputed, "ETag is not deterministic");
    Require(File.ReadAllLines(audit).Length == 1, "conflict audit count mismatch");
    Console.WriteLine("PASS same_version_read=true correct_if_match=true stale_rejected_409=true missing_if_match_428=true external_conflict=true same_content_noop=true both_inputs_preserved=true audit_record=true");
    Console.WriteLine("ETag=strong-sha256-base64url; lock=optimistic-no-long-held-lock; SDK=10.0.302 Runtime=10.0.10");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, true);
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
