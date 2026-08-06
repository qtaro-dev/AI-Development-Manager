using Adm.Core.Projects;

namespace Adm.Application.Projects;

public enum ProjectErrorCode
{
    DuplicateProject,
    ProjectNotRegistered,
    InvalidRoot,
    UnsupportedFileSystem,
    AccessDenied,
    PersistenceFailed,
}

public sealed record ProjectError(ProjectErrorCode Code);

public sealed class ProjectCatalogException : Exception
{
    public ProjectCatalogException(string code)
        : base("Project catalog storage failed.")
    {
        Code = code;
    }

    public ProjectCatalogException(string code, Exception innerException)
        : base("Project catalog storage failed.", innerException)
    {
        Code = code;
    }

    public string Code { get; }
}

public sealed record RegisteredProjectCatalogSnapshot(
    IReadOnlyList<RegisteredProject> Projects,
    ProjectId? SelectedProjectId);

public interface IRegisteredProjectCatalog
{
    public Task<RegisteredProjectCatalogSnapshot> ReadAsync(CancellationToken cancellationToken = default);

    public Task SaveAsync(
        RegisteredProjectCatalogSnapshot snapshot,
        CancellationToken cancellationToken = default);
}

public interface IProjectRootValidator
{
    public Task<ProjectRootValidationResult> ValidateAsync(
        ProjectRootInput root,
        CancellationToken cancellationToken = default);
}

public sealed record ProjectRootValidationResult(
    ValidatedProjectRoot? Root,
    ProjectError? Error)
{
    public bool IsValid => Root is not null && Error is null;

    public static ProjectRootValidationResult Valid(ValidatedProjectRoot root) => new(root, null);

    public static ProjectRootValidationResult Invalid(ProjectErrorCode code) =>
        new(null, new ProjectError(code));
}

public interface IProjectClock
{
    public DateTimeOffset UtcNow { get; }
}

public interface IProjectIdGenerator
{
    public ProjectId Create();
}

public sealed record RegisterProjectInput(ProjectRootInput Root, string? DisplayName);

public sealed record RegisterProjectResult(RegisteredProject? Project, ProjectError? Error)
{
    public RegisterProjectResult(RegisteredProject project)
        : this(project, null)
    {
    }

    public bool IsSuccess => Project is not null && Error is null;
}

public sealed record UnregisterProjectInput(ProjectId ProjectId);

public sealed record UnregisterProjectResult(ProjectId ProjectId);

public sealed record ListProjectsResult(
    IReadOnlyList<RegisteredProject> Projects,
    ProjectId? SelectedProjectId);

public sealed record SelectProjectInput(ProjectId? ProjectId);

public sealed record SelectProjectResult(ProjectId? SelectedProjectId);
