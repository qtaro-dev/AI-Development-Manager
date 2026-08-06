using Adm.Core.Projects;

namespace Adm.Application.Projects;

public sealed class ListLocalProjectsUseCase
{
    private readonly IRegisteredProjectCatalog catalog;
    private readonly IProjectRootValidator rootValidator;

    public ListLocalProjectsUseCase(
        IRegisteredProjectCatalog catalog,
        IProjectRootValidator rootValidator)
    {
        this.catalog = catalog;
        this.rootValidator = rootValidator;
    }

    public async Task<ListProjectsResult> ListAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await catalog.ReadAsync(cancellationToken);
            var projects = snapshot.Projects
                .OrderBy(project => project.Id.Value, StringComparer.Ordinal)
                .ToArray();
            var warnings = new List<ProjectWarning>();

            foreach (var project in projects)
            {
                ProjectRootValidationResult validation;
                try
                {
                    validation = await rootValidator.ValidateAsync(
                        new ProjectRootInput(project.Root.CanonicalPath),
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    validation = ProjectRootValidationResult.Invalid(ProjectErrorCode.InvalidRoot);
                }

                if (!validation.IsValid)
                {
                    warnings.Add(new(
                        project.Id,
                        validation.Error?.Code ?? ProjectErrorCode.InvalidRoot));
                }
            }

            return new(projects, snapshot.SelectedProjectId, warnings, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new([], null, [], new ProjectError(ProjectErrorCode.PersistenceFailed));
        }
    }
}
