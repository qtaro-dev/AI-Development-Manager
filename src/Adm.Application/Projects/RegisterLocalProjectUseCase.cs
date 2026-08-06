using Adm.Core.Projects;

namespace Adm.Application.Projects;

public sealed class RegisterLocalProjectUseCase
{
    private readonly IProjectRootValidator rootValidator;
    private readonly IRegisteredProjectCatalog catalog;
    private readonly IProjectClock clock;
    private readonly IProjectIdGenerator idGenerator;

    public RegisterLocalProjectUseCase(
        IProjectRootValidator rootValidator,
        IRegisteredProjectCatalog catalog,
        IProjectClock clock,
        IProjectIdGenerator idGenerator)
    {
        this.rootValidator = rootValidator;
        this.catalog = catalog;
        this.clock = clock;
        this.idGenerator = idGenerator;
    }

    public async Task<RegisterProjectResult> RegisterAsync(
        RegisterProjectInput input,
        CancellationToken cancellationToken = default)
    {
        var validation = await rootValidator.ValidateAsync(input.Root, cancellationToken);
        if (!validation.IsValid)
            return new(null, validation.Error ?? new ProjectError(ProjectErrorCode.InvalidRoot));

        try
        {
            var snapshot = await catalog.ReadAsync(cancellationToken);
            if (snapshot.Projects.Any(project => RootsEqual(project.Root, validation.Root!.Value)))
                return new(null, new ProjectError(ProjectErrorCode.DuplicateProject));

            var projectId = idGenerator.Create();
            if (snapshot.Projects.Any(project => project.Id == projectId))
                return new(null, new ProjectError(ProjectErrorCode.DuplicateProject));

            var project = new RegisteredProject(
                projectId,
                ResolveDisplayName(input.DisplayName, validation.Root!.Value),
                validation.Root.Value,
                clock.UtcNow);
            var projects = snapshot.Projects.Append(project).ToArray();

            await catalog.SaveAsync(
                new RegisteredProjectCatalogSnapshot(projects, snapshot.SelectedProjectId),
                cancellationToken);

            return new(project);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new(null, new ProjectError(ProjectErrorCode.PersistenceFailed));
        }
    }

    private static bool RootsEqual(ValidatedProjectRoot left, ValidatedProjectRoot right) =>
        StringComparer.OrdinalIgnoreCase.Equals(
            TrimEndingSeparator(left.CanonicalPath),
            TrimEndingSeparator(right.CanonicalPath));

    private static string ResolveDisplayName(string? displayName, ValidatedProjectRoot root)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName.Trim();

        var canonicalPath = TrimEndingSeparator(root.CanonicalPath);
        var separator = canonicalPath.LastIndexOfAny(['\\', '/']);
        return separator >= 0 && separator < canonicalPath.Length - 1
            ? canonicalPath[(separator + 1)..]
            : canonicalPath;
    }

    private static string TrimEndingSeparator(string path) =>
        path.Length > 1 ? path.TrimEnd('\\', '/') : path;
}
