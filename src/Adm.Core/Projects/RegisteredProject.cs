namespace Adm.Core.Projects;

public sealed record RegisteredProject
{
    public RegisteredProject(
        ProjectId id,
        string displayName,
        ValidatedProjectRoot root,
        DateTimeOffset registeredAtUtc,
        bool isSelected = false)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Project display name is required.", nameof(displayName));

        Id = id;
        DisplayName = displayName;
        Root = root;
        RegisteredAtUtc = registeredAtUtc.ToUniversalTime();
        IsSelected = isSelected;
    }

    public ProjectId Id { get; }
    public string DisplayName { get; }
    public ValidatedProjectRoot Root { get; }
    public DateTimeOffset RegisteredAtUtc { get; }
    public bool IsSelected { get; }
}
