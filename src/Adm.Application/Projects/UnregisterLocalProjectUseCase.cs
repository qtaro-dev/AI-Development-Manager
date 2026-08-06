using Adm.Core.Projects;

namespace Adm.Application.Projects;

public sealed class UnregisterLocalProjectUseCase
{
    private readonly IRegisteredProjectCatalog catalog;

    public UnregisterLocalProjectUseCase(IRegisteredProjectCatalog catalog)
    {
        this.catalog = catalog;
    }

    public async Task<UnregisterProjectResult> UnregisterAsync(
        UnregisterProjectInput input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await catalog.ReadAsync(cancellationToken);
            if (!snapshot.Projects.Any(project => project.Id == input.ProjectId))
            {
                return new(
                    input.ProjectId,
                    new ProjectError(ProjectErrorCode.ProjectNotRegistered));
            }

            var remainingProjects = snapshot.Projects
                .Where(project => project.Id != input.ProjectId)
                .ToArray();
            var selectedProjectId = snapshot.SelectedProjectId == input.ProjectId
                ? null
                : snapshot.SelectedProjectId;

            await catalog.SaveAsync(
                new RegisteredProjectCatalogSnapshot(remainingProjects, selectedProjectId),
                cancellationToken);

            return new(input.ProjectId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new(
                input.ProjectId,
                new ProjectError(ProjectErrorCode.PersistenceFailed));
        }
    }
}
