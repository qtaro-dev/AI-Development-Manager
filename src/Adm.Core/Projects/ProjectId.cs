namespace Adm.Core.Projects;

public readonly record struct ProjectId
{
    private const int MaxLength = 64;

    public ProjectId(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength ||
            value.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new ArgumentException("Project ID must contain only ASCII letters, digits, '-' or '_'.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
