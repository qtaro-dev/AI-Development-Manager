using Microsoft.Data.Sqlite;

namespace Adm.Search.Sqlite;

public sealed record SearchDocument(string Id, string Path, string FileName, string Heading, string Body, string Status, string RelatedId);
public sealed record SearchHit(string Id, string Path, string Snippet, double Rank);

public sealed class SqliteSearchIndex : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _tokenizer;

    public SqliteSearchIndex(string databasePath, string tokenizer)
    {
        _tokenizer = tokenizer;
        _connection = new SqliteConnection($"Data Source={databasePath}");
        _connection.Open();
        EnsureSchema();
    }

    public string Tokenizer => _tokenizer;

    public void Rebuild(IEnumerable<SearchDocument> documents)
    {
        using var transaction = _connection.BeginTransaction();
        Execute("DELETE FROM documents_fts", transaction);
        Execute("DELETE FROM documents", transaction);
        foreach (var document in documents) UpsertInternal(document, transaction);
        transaction.Commit();
    }

    public void Upsert(SearchDocument document)
    {
        using var transaction = _connection.BeginTransaction();
        using (var command = _connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM documents_fts WHERE doc_id = $id; DELETE FROM documents WHERE id = $id;";
            command.Parameters.AddWithValue("$id", document.Id);
            command.ExecuteNonQuery();
        }
        UpsertInternal(document, transaction);
        transaction.Commit();
    }

    public void Delete(string id)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM documents_fts WHERE doc_id = $id; DELETE FROM documents WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<SearchHit> Search(string term, int limit = 20)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT doc_id, path, snippet(documents_fts, 3, '[', ']', '…', 18), bm25(documents_fts) FROM documents_fts WHERE documents_fts MATCH $query ORDER BY bm25(documents_fts), path LIMIT $limit";
        command.Parameters.AddWithValue("$query", '"' + term.Replace("\"", "\"\"", StringComparison.Ordinal) + '"');
        command.Parameters.AddWithValue("$limit", limit);
        using var reader = command.ExecuteReader();
        var results = new List<SearchHit>();
        while (reader.Read()) results.Add(new SearchHit(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? string.Empty : reader.GetString(2), reader.GetDouble(3)));
        return results;
    }

    private void EnsureSchema()
    {
        Execute("CREATE TABLE IF NOT EXISTS documents (id TEXT PRIMARY KEY, path TEXT NOT NULL, file_name TEXT NOT NULL, heading TEXT NOT NULL, body TEXT NOT NULL, status TEXT NOT NULL, related_id TEXT NOT NULL);", null);
        var tokenizer = _tokenizer == "trigram" ? "trigram" : "unicode61 remove_diacritics 2";
        Execute($"CREATE VIRTUAL TABLE IF NOT EXISTS documents_fts USING fts5(doc_id UNINDEXED, path, file_name, heading, body, status, related_id, tokenize='{tokenizer}');", null);
    }

    private void UpsertInternal(SearchDocument document, SqliteTransaction transaction)
    {
        using var normal = _connection.CreateCommand();
        normal.Transaction = transaction;
        normal.CommandText = "INSERT INTO documents VALUES ($id, $path, $file, $heading, $body, $status, $related)";
        normal.Parameters.AddWithValue("$id", document.Id); normal.Parameters.AddWithValue("$path", document.Path); normal.Parameters.AddWithValue("$file", document.FileName); normal.Parameters.AddWithValue("$heading", document.Heading); normal.Parameters.AddWithValue("$body", document.Body); normal.Parameters.AddWithValue("$status", document.Status); normal.Parameters.AddWithValue("$related", document.RelatedId);
        normal.ExecuteNonQuery();
        using var fts = _connection.CreateCommand();
        fts.Transaction = transaction;
        fts.CommandText = "INSERT INTO documents_fts VALUES ($id, $path, $file, $heading, $body, $status, $related)";
        fts.Parameters.AddWithValue("$id", document.Id); fts.Parameters.AddWithValue("$path", document.Path); fts.Parameters.AddWithValue("$file", document.FileName); fts.Parameters.AddWithValue("$heading", document.Heading); fts.Parameters.AddWithValue("$body", document.Body); fts.Parameters.AddWithValue("$status", document.Status); fts.Parameters.AddWithValue("$related", document.RelatedId);
        fts.ExecuteNonQuery();
    }

    private void Execute(string sql, SqliteTransaction? transaction)
    {
        using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public void Dispose() => _connection.Dispose();
}
