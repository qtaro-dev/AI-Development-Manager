using System.Text.RegularExpressions;
using Adm.Documents.Parsing;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Adm.Documents.Classification;

public sealed record ClassificationEvidence(string Code, string Signal, string Detail, string? DocumentType = null, double? Confidence = null);

public sealed record ClassificationResult(
    string AutomaticType,
    double AutomaticConfidence,
    IReadOnlyList<ClassificationEvidence> AutomaticEvidence,
    string EffectiveType,
    double EffectiveConfidence,
    string? ManualOverride,
    IReadOnlyList<ClassificationEvidence> EffectiveEvidence);

public sealed class ClassifierRule
{
    public string Code { get; set; } = string.Empty;
    public string Signal { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

public sealed class ClassifierRules
{
    public int Version { get; set; }
    public double MinimumConfidence { get; set; }
    public List<ClassifierRule> Rules { get; set; } = [];

    public static ClassifierRules Load(string path) => new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build()
        .Deserialize<ClassifierRules>(File.ReadAllText(path));
}

public sealed class DocumentClassifier
{
    private static readonly string[] SignalOrder = ["filename", "folder", "heading", "table"];
    private readonly ClassifierRules _rules;

    public DocumentClassifier(ClassifierRules rules) => _rules = rules;

    public ClassificationResult Classify(ParsedDocument document, string? manualOverride = null)
    {
        var frontMatterType = document.FrontMatter.TryGetValue("document_type", out var value)
            ? Convert.ToString(value)
            : null;

        ClassificationResult automatic;
        if (!string.IsNullOrWhiteSpace(frontMatterType) && !string.Equals(frontMatterType, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            automatic = BuildResult(frontMatterType!, 1.0,
                [new("front_matter.document_type", "front_matter", frontMatterType!)], manualOverride);
        }
        else
        {
            var candidates = new List<ClassificationEvidence>();
            var fileName = Path.GetFileNameWithoutExtension(document.RelativePath);
            var folder = Path.GetDirectoryName(document.RelativePath)?.Replace('\\', '/') ?? string.Empty;
            var headings = string.Join(" ", document.Headings);

            foreach (var rule in _rules.Rules)
            {
                var source = rule.Signal switch
                {
                    "filename" => fileName,
                    "folder" => folder,
                    "heading" => headings,
                    "table" => string.Join(" ", document.Tables.SelectMany(table => table.Columns)),
                    _ => string.Empty
                };

                if (source.Length > 0 && Regex.IsMatch(source, rule.Pattern))
                {
                    candidates.Add(new(rule.Code, rule.Signal, rule.DocumentType, rule.DocumentType, rule.Confidence));
                }
            }

            automatic = SelectCandidate(candidates);
        }

        return manualOverride is null
            ? automatic
            : automatic with
            {
                EffectiveType = manualOverride,
                EffectiveConfidence = 1.0,
                ManualOverride = manualOverride,
                EffectiveEvidence = [new("manual_override", "manual", manualOverride)]
            };
    }

    private ClassificationResult SelectCandidate(IReadOnlyList<ClassificationEvidence> candidates)
    {
        foreach (var signal in SignalOrder)
        {
            var current = candidates.Where(candidate => candidate.Signal == signal).ToArray();
            if (current.Length == 0)
            {
                continue;
            }

            var typeGroups = current.GroupBy(candidate => candidate.DocumentType ?? string.Empty, StringComparer.Ordinal).ToArray();
            if (typeGroups.Length != 1)
            {
                return BuildResult("unknown", 0.0,
                    [new("classification.conflict", signal, string.Join(", ", typeGroups.Select(group => group.Key)))], null);
            }

            var selected = current[0];
            return BuildResult(selected.DocumentType ?? "unknown", selected.Confidence ?? 0.0, current, null);
        }

        return BuildResult("unknown", 0.0, [], null);
    }

    private static ClassificationResult BuildResult(string type, double confidence, IReadOnlyList<ClassificationEvidence> evidence, string? manualOverride) =>
        new(type, confidence, evidence, manualOverride ?? type, manualOverride is null ? confidence : 1.0,
            manualOverride, manualOverride is null ? evidence : [new("manual_override", "manual", manualOverride)]);
}
