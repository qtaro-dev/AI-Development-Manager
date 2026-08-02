using Adm.Search.Sqlite;
using Microsoft.Data.Sqlite;

var root = Path.Combine(Path.GetTempPath(), $"sqlite-fts-ja-poc-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);
try
{
    var documents = new[]
    {
        new SearchDocument("1", "tickets/001.md", "001.md", "データベース接続障害", "本番環境で接続タイムアウトが発生しました。ERR-1234 を確認してください。", "open", "ticket-001"),
        new SearchDocument("2", "tickets/002.md", "002.md", "ユーザー認証の不具合", "ログイン時に認証トークンが拒否されます。ERR-5678。", "closed", "ticket-002"),
        new SearchDocument("3", "design/search.md", "search.md", "日本語検索設計", "Unicode61とTrigramの比較を行う設計資料です。", "draft", "design-search")
    };
    var unicodePath = Path.Combine(root, "unicode.db");
    var trigramPath = Path.Combine(root, "trigram.db");
    using var unicode = new SqliteSearchIndex(unicodePath, "unicode61");
    using var trigram = new SqliteSearchIndex(trigramPath, "trigram");
    unicode.Rebuild(documents);
    trigram.Rebuild(documents);

    var japanese = trigram.Search("ユーザー");
    Require(japanese.Count > 0 && japanese[0].Id == "2" && japanese[0].Snippet.Contains("ユーザー", StringComparison.Ordinal), "Trigram Japanese search failed");
    var mixed = unicode.Search("ERR-1234");
    Require(mixed.Count > 0 && mixed[0].Id == "1", "Unicode61 error code search failed");
    var trigramPartial = trigram.Search("123");
    Require(trigramPartial.Count > 0 && trigramPartial[0].Id == "1", "Trigram partial error search failed");
    Require(unicode.Search("ユーザー認証の不具合").Count > 0, "Unicode61 full Japanese phrase search failed");

    unicode.Upsert(documents[0] with { Body = "更新された本文 ERR-9999", Status = "closed" });
    Require(unicode.Search("ERR-9999").Single().Id == "1" && unicode.Search("ERR-1234").Count == 0, "update index consistency failed");
    unicode.Upsert(documents[1] with { Path = "archive/002-renamed.md", FileName = "002-renamed.md" });
    Require(unicode.Search("ユーザー認証の不具合").Single().Path == "archive/002-renamed.md", "rename index consistency failed");
    unicode.Delete("3");
    Require(unicode.Search("Unicode61").Count == 0, "delete index consistency failed");

    unicode.Dispose();
    SqliteConnection.ClearAllPools();
    File.Delete(unicodePath);
    using var rebuilt = new SqliteSearchIndex(unicodePath, "unicode61");
    rebuilt.Rebuild(documents);
    Require(rebuilt.Search("ユーザー認証の不具合").Single().Id == "2" && rebuilt.Search("ERR-1234").Single().Id == "1", "SQLite rebuild failed");
    Require(File.Exists(unicodePath) && File.Exists(trigramPath), "SQLite cache was not created");
    rebuilt.Dispose();
    trigram.Dispose();
    SqliteConnection.ClearAllPools();
    Console.WriteLine("PASS unicode61_japanese=true unicode61_error_code=true trigram_partial=true trigram_japanese=true snippet=true ranking=true update=true rename=true delete=true sqlite_delete_rebuild=true sqlite_not_source_of_truth=true");
    Console.WriteLine("Tokenizers=unicode61,trigram; SDK=10.0.302 Runtime=10.0.10");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, true);
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
