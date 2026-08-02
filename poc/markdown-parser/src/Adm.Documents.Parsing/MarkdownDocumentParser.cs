using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Adm.Documents.Parsing;

public sealed class MarkdownDocumentParser
{
    private static readonly HashSet<string> KnownFrontMatterKeys = new(StringComparer.Ordinal)
    {
        "document_type", "schema_version", "ticket_id", "status", "title",
        "test_case_id", "execution_id", "adr_id", "attachments", "link"
    };

    private static readonly string[] ExpectedTestCaseColumns = ["item_id", "category", "content", "steps", "expected"];
    private static readonly Regex LinkPattern = new(@"!?(?:\[[^\]]*\])\(([^)]+)\)", RegexOptions.Compiled);

    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public ParsedDocument Parse(string filePath, string rootPath)
    {
        var bytes = File.ReadAllBytes(filePath);
        var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var (encodingName, text) = Decode(bytes);
        var relativePath = Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
        var issues = new List<ParseIssue>();
        var frontMatter = new Dictionary<string, object?>(StringComparer.Ordinal);
        var unknownKeys = new List<string>();
        var body = text;
        var frontMatterPresent = StartsWithFrontMatter(text);
        var frontMatterValid = true;

        if (frontMatterPresent)
        {
            var extraction = ExtractFrontMatter(text);
            body = extraction.Body;
            if (extraction.Content is null)
            {
                frontMatterValid = false;
                issues.Add(new(ParseSeverity.Fatal, "front_matter_invalid_yaml", "Front Matter区間を確定できません。"));
            }
            else
            {
                try
                {
                    frontMatter = ParseYamlMapping(extraction.Content);
                    unknownKeys.AddRange(frontMatter.Keys.Where(key => !KnownFrontMatterKeys.Contains(key)));
                    if (unknownKeys.Count > 0)
                    {
                        issues.Add(new(ParseSeverity.Warning, "front_matter_unknown_key", string.Join(", ", unknownKeys)));
                    }

                    if (TryGetInt(frontMatter, "schema_version", out var schemaVersion) && schemaVersion < 1)
                    {
                        issues.Add(new(ParseSeverity.Warning, "schema_version_old", $"schema_version={schemaVersion}"));
                    }
                }
                catch (Exception exception) when (exception is YamlException or InvalidOperationException)
                {
                    frontMatterValid = false;
                    issues.Add(new(ParseSeverity.Fatal, "front_matter_invalid_yaml", exception.Message));
                }
            }
        }
        else
        {
            issues.Add(new(ParseSeverity.Warning, "front_matter_missing", "Front Matterがありません。"));
        }

        if (encodingName != "utf-8" && encodingName != "utf-8-bom")
        {
            issues.Add(new(ParseSeverity.Warning, "encoding_non_utf8", encodingName));
        }

        var document = Markdown.Parse(body, _pipeline);
        var headings = document.Descendants<HeadingBlock>()
            .Select(heading => ExtractInlineText(heading.Inline))
            .Where(value => value.Length > 0)
            .ToArray();
        var tables = document.Descendants<Table>()
            .Select(ParseTable)
            .ToArray();
        var documentType = frontMatter.TryGetValue("document_type", out var typeValue)
            ? Convert.ToString(typeValue, System.Globalization.CultureInfo.InvariantCulture) ?? "unknown"
            : "unknown";

        foreach (var table in tables)
        {
            if (string.Equals(documentType, "test_case", StringComparison.Ordinal))
            {
                foreach (var missing in ExpectedTestCaseColumns.Except(table.Columns, StringComparer.Ordinal))
                {
                    issues.Add(new(ParseSeverity.Warning, $"table_column_missing:{missing}", $"列がありません: {missing}"));
                }

                foreach (var extra in table.Columns.Except(ExpectedTestCaseColumns, StringComparer.Ordinal))
                {
                    issues.Add(new(ParseSeverity.Warning, $"table_column_unknown:{extra}", $"未知の列です: {extra}"));
                }
            }

            if (table.Rows.SelectMany(row => row).Any(value => value.Length > 128))
            {
                issues.Add(new(ParseSeverity.Warning, "table_cell_large", "128文字を超える表セルがあります。"));
            }
        }

        var attachments = new List<string>();
        if (frontMatter.TryGetValue("attachments", out var attachmentValue))
        {
            AddAttachmentValues(attachmentValue, attachments);
        }

        attachments.AddRange(LinkPattern.Matches(body)
            .Select(match => match.Groups[1].Value.Trim())
            .Where(target => !Uri.TryCreate(target, UriKind.Absolute, out _))
            .Where(target => !attachments.Contains(target, StringComparer.Ordinal)));
        attachments = attachments.Distinct(StringComparer.Ordinal).ToList();

        foreach (var attachment in attachments)
        {
            if (attachment.Contains("..", StringComparison.Ordinal))
            {
                issues.Add(new(ParseSeverity.Warning, "relative_path_outside_root", attachment));
                continue;
            }

            if (!File.Exists(Path.Combine(Path.GetDirectoryName(filePath)!, attachment.Replace('/', Path.DirectorySeparatorChar))))
            {
                issues.Add(new(ParseSeverity.Warning, "attachment_missing", attachment));
            }
        }

        return new(
            relativePath,
            sha256,
            encodingName,
            frontMatterPresent,
            frontMatterValid,
            documentType,
            frontMatter,
            unknownKeys,
            headings,
            body,
            tables,
            attachments,
            issues);
    }

    private static (string EncodingName, string Text) Decode(byte[] bytes)
    {
        if (bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }))
        {
            return ("utf-8-bom", Encoding.UTF8.GetString(bytes[3..]));
        }

        try
        {
            var utf8 = new UTF8Encoding(false, true);
            return ("utf-8", utf8.GetString(bytes));
        }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return ("shift_jis", Encoding.GetEncoding(932).GetString(bytes));
        }
    }

    private static bool StartsWithFrontMatter(string text) =>
        text.StartsWith("---\n", StringComparison.Ordinal) || text.StartsWith("---\r\n", StringComparison.Ordinal);

    private static (string? Content, string Body) ExtractFrontMatter(string text)
    {
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var closing = Array.FindIndex(lines, 1, line => line.Trim() == "---");
        if (closing < 0)
        {
            return (null, text);
        }

        var content = string.Join('\n', lines[1..closing]);
        var body = string.Join('\n', lines[(closing + 1)..]).TrimStart('\n');
        return (content, body);
    }

    private static Dictionary<string, object?> ParseYamlMapping(string yaml)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(yaml));
        if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is not YamlMappingNode mapping)
        {
            throw new InvalidOperationException("Front Matterはmappingでなければなりません。");
        }

        return mapping.Children.ToDictionary(
            pair => ((YamlScalarNode)pair.Key).Value ?? string.Empty,
            pair => ConvertYamlNode(pair.Value),
            StringComparer.Ordinal);
    }

    private static object? ConvertYamlNode(YamlNode node) => node switch
    {
        YamlScalarNode scalar => scalar.Value,
        YamlSequenceNode sequence => sequence.Children.Select(ConvertYamlNode).ToArray(),
        YamlMappingNode mapping => mapping.Children.ToDictionary(
            pair => ((YamlScalarNode)pair.Key).Value ?? string.Empty,
            pair => ConvertYamlNode(pair.Value),
            StringComparer.Ordinal),
        _ => null
    };

    private static bool TryGetInt(IReadOnlyDictionary<string, object?> values, string key, out int result)
    {
        if (values.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value), out result))
        {
            return true;
        }

        result = default;
        return false;
    }

    private static TableResult ParseTable(Table table)
    {
        var rows = table.OfType<TableRow>().Select(row => row.OfType<TableCell>().Select(ExtractInlineText).ToArray()).ToArray();
        return rows.Length == 0 ? new([], []) : new(rows[0], rows.Skip(1).ToArray());
    }

    private static string ExtractInlineText(ContainerInline? inline) => inline is null
        ? string.Empty
        : string.Concat(
            inline.Descendants<LiteralInline>().Select(literal => literal.Content.ToString())
                .Concat(inline.Descendants<CodeInline>().Select(code => code.Content.ToString()))).Trim();

    private static string ExtractInlineText(MarkdownObject node) => string.Concat(
        node.Descendants<LiteralInline>().Select(literal => literal.Content.ToString())
            .Concat(node.Descendants<CodeInline>().Select(code => code.Content.ToString()))).Trim();

    private static void AddAttachmentValues(object? value, ICollection<string> attachments)
    {
        switch (value)
        {
            case string path:
                attachments.Add(path);
                break;
            case IEnumerable<object?> values:
                foreach (var child in values)
                {
                    AddAttachmentValues(child, attachments);
                }
                break;
        }
    }
}
