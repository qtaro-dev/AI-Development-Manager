using Adm.Documents.Classification;
using Adm.Documents.Parsing;

if (args.Length < 3 || !string.Equals(args[0], "--verify", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Usage: DocumentClassifier.Poc --verify <poc/fixtures> <rules.yaml>");
    return 2;
}

var fixturesRoot = Path.GetFullPath(args[1]);
var rulesPath = Path.GetFullPath(args[2]);
var manifest = new YamlDotNet.Serialization.DeserializerBuilder()
    .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
    .IgnoreUnmatchedProperties()
    .Build()
    .Deserialize<FixtureManifest>(File.ReadAllText(Path.Combine(fixturesRoot, "manifest.yaml")));
var parser = new MarkdownDocumentParser();
var classifier = new DocumentClassifier(ClassifierRules.Load(rulesPath));
var failures = new List<string>();

foreach (var fixture in manifest.Fixtures)
{
    var filePath = Path.Combine(fixturesRoot, "markdown", fixture.Path.Replace('/', Path.DirectorySeparatorChar));
    var before = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(filePath)));
    var parsed = parser.Parse(filePath, Path.Combine(fixturesRoot, "markdown"));
    var result = classifier.Classify(parsed);
    var after = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(filePath)));

    if (!string.Equals(result.EffectiveType, fixture.ExpectedDocumentType, StringComparison.Ordinal))
    {
        failures.Add($"{fixture.Id}: expected={fixture.ExpectedDocumentType} actual={result.EffectiveType}");
    }

    if (!string.Equals(before, after, StringComparison.Ordinal))
    {
        failures.Add($"{fixture.Id}: input changed");
    }

    Console.WriteLine($"{fixture.Id}: type={result.EffectiveType} confidence={result.EffectiveConfidence:0.00} evidence={string.Join(',', result.EffectiveEvidence.Select(item => item.Code))}");
}

var syntheticCases = new[]
{
    (Path: "test-case-login.md", Folder: "misc", Headings: new[] { "Design note" }, Tables: Array.Empty<TableResult>(), Expected: "test_case", Name: "filename wins over heading"),
    (Path: "generic.md", Folder: "design", Headings: Array.Empty<string>(), Tables: Array.Empty<TableResult>(), Expected: "design", Name: "folder evidence"),
    (Path: "table.md", Folder: "misc", Headings: Array.Empty<string>(), Tables: new[] { new TableResult(["id", "title"], []) }, Expected: "test_case", Name: "table evidence"),
    (Path: "readme.md", Folder: "misc", Headings: new[] { "Unrelated notes" }, Tables: Array.Empty<TableResult>(), Expected: "unknown", Name: "unknown stays unknown"),
    (Path: "manual.md", Folder: "misc", Headings: Array.Empty<string>(), Tables: Array.Empty<TableResult>(), Expected: "adr", Name: "manual override")
};

foreach (var item in syntheticCases)
{
    var overrideType = item.Name == "manual override" ? "adr" : null;
    var document = new ParsedDocument(Path.Combine(item.Folder, item.Path).Replace('\\', '/'), "synthetic", "utf-8", false, true, "unknown", new Dictionary<string, object?>(), [], item.Headings, "", item.Tables, [], []);
    var result = classifier.Classify(document, overrideType);
    if (!string.Equals(result.EffectiveType, item.Expected, StringComparison.Ordinal))
    {
        failures.Add($"{item.Name}: expected={item.Expected} actual={result.EffectiveType}");
    }

    Console.WriteLine($"synthetic:{item.Name}: type={result.EffectiveType} confidence={result.EffectiveConfidence:0.00}");
}

if (failures.Count > 0)
{
    Console.Error.WriteLine($"FAIL failures={failures.Count}");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine(failure);
    }

    return 1;
}

Console.WriteLine($"PASS fixtures={manifest.Fixtures.Count} synthetic={syntheticCases.Length} input_unchanged=true");
return 0;
