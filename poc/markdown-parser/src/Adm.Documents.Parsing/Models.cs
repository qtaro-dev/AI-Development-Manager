namespace Adm.Documents.Parsing;

public enum ParseSeverity
{
    Warning,
    Fatal
}

public sealed record ParseIssue(ParseSeverity Severity, string Code, string Message);

public sealed record TableResult(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string>> Rows);

public sealed record ParsedDocument(
    string RelativePath,
    string Sha256,
    string Encoding,
    bool FrontMatterPresent,
    bool FrontMatterValid,
    string DocumentType,
    IReadOnlyDictionary<string, object?> FrontMatter,
    IReadOnlyList<string> UnknownFrontMatterKeys,
    IReadOnlyList<string> Headings,
    string Body,
    IReadOnlyList<TableResult> Tables,
    IReadOnlyList<string> AttachmentReferences,
    IReadOnlyList<ParseIssue> Issues);

public sealed class FixtureManifest
{
    public int Version { get; set; }
    public string CorpusId { get; set; } = string.Empty;
    public bool Synthetic { get; set; }
    public List<FixtureSpec> Fixtures { get; set; } = [];
}

public sealed class FixtureSpec
{
    public string Id { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Encoding { get; set; } = string.Empty;
    public string ExpectedDocumentType { get; set; } = string.Empty;
    public List<string> ExpectedWarnings { get; set; } = [];
    public string Sha256 { get; set; } = string.Empty;
}
