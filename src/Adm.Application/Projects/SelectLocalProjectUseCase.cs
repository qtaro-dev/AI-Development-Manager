using Adm.Core.Projects;

namespace Adm.Application.Projects;

public sealed class SelectLocalProjectUseCase
{
    private readonly IRegisteredProjectCatalog catalog;

    public SelectLocalProjectUseCase(IRegisteredProjectCatalog catalog)
    {
        this.catalog = catalog;
    }

    public async Task<SelectProjectResult> SelectAsync(
        SelectProjectInput input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await catalog.ReadAsync(cancellationToken);
            if (input.ProjectId.HasValue &&
                !snapshot.Projects.Any(project => project.Id == input.ProjectId.Value))
            {
                return new(
                    input.ProjectId,
                    new ProjectError(ProjectErrorCode.ProjectNotRegistered));
            }

            await catalog.SaveAsync(
                new RegisteredProjectCatalogSnapshot(snapshot.Projects, input.ProjectId),
                cancellationToken);

            return new(input.ProjectId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new(input.ProjectId, new ProjectError(ProjectErrorCode.PersistenceFailed));
        }
    }
}
