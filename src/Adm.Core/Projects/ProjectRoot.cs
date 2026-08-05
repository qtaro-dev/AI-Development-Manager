namespace Adm.Core.Projects;

public readonly record struct ProjectRootInput
{
    public ProjectRootInput(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Project root is required.", nameof(value));

        Value = value;
    }

    public string Value { get; }
}

public readonly record struct ValidatedProjectRoot
{
    public ValidatedProjectRoot(string canonicalPath)
    {
        if (string.IsNullOrWhiteSpace(canonicalPath))
            throw new ArgumentException("Validated project root is required.", nameof(canonicalPath));

        CanonicalPath = canonicalPath;
    }

    public string CanonicalPath { get; }
}
