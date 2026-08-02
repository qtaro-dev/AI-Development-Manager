using System.Security.Cryptography;
using System.Text;
using Adm.Documents.Parsing;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

if (args.Length < 2 || !string.Equals(args[0], "--verify", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Usage: MarkdownParser.Poc --verify <poc/fixtures>");
    return 2;
}

var fixturesRoot = Path.GetFullPath(args[1]);
var manifestPath = Path.Combine(fixturesRoot, "manifest.yaml");
var markdownRoot = Path.Combine(fixturesRoot, "markdown");
var manifest = new DeserializerBuilder()
    .WithNamingConvention(UnderscoredNamingConvention.Instance)
    .IgnoreUnmatchedProperties()
    .Build()
    .Deserialize<FixtureManifest>(File.ReadAllText(manifestPath, Encoding.UTF8));
var parser = new MarkdownDocumentParser();
var failures = new List<string>();
var parsedCount = 0;

foreach (var fixture in manifest.Fixtures)
{
    var filePath = Path.Combine(markdownRoot, fixture.Path.Replace('/', Path.DirectorySeparatorChar));
    try
    {
        var before = SHA256.HashData(File.ReadAllBytes(filePath));
        var result = parser.Parse(filePath, markdownRoot);
        var after = SHA256.HashData(File.ReadAllBytes(filePath));
        var actualWarnings = result.Issues
            .Where(issue => issue.Severity == ParseSeverity.Warning || issue.Severity == ParseSeverity.Fatal)
            .Select(issue => issue.Code)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();
        var expectedWarnings = fixture.ExpectedWarnings.OrderBy(code => code, StringComparer.Ordinal).ToArray();

        if (!string.Equals(result.DocumentType, fixture.ExpectedDocumentType, StringComparison.Ordinal))
        {
            failures.Add($"{fixture.Id}: document_type expected={fixture.ExpectedDocumentType} actual={result.DocumentType}");
        }

        if (!expectedWarnings.SequenceEqual(actualWarnings, StringComparer.Ordinal))
        {
            failures.Add($"{fixture.Id}: warnings expected=[{string.Join(',', expectedWarnings)}] actual=[{string.Join(',', actualWarnings)}]");
        }

        if (!string.Equals(result.Sha256, fixture.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{fixture.Id}: sha256 mismatch");
        }

        if (!before.SequenceEqual(after))
        {
            failures.Add($"{fixture.Id}: input file changed");
        }

        parsedCount++;
        Console.WriteLine($"{fixture.Id}: type={result.DocumentType} encoding={result.Encoding} headings={result.Headings.Count} tables={result.Tables.Count} issues={result.Issues.Count}");
    }
    catch (Exception exception)
    {
        failures.Add($"{fixture.Id}: unhandled {exception.GetType().Name}: {exception.Message}");
    }
}

Console.WriteLine($"fixtures={parsedCount}/{manifest.Fixtures.Count}");
if (failures.Count > 0)
{
    Console.Error.WriteLine($"FAIL failures={failures.Count}");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine(failure);
    }

    return 1;
}

Console.WriteLine("PASS all golden fixtures; inputs unchanged");
return 0;
