using Adm.Application.Projects;
using Adm.Core.Projects;

namespace Adm.Application.Tests;

public sealed class ProjectContractTests
{
    [Fact]
    public async Task CatalogContractCarriesProjectsAndSelectionWithoutFilesystemOperations()
    {
        var project = new RegisteredProject(
            new ProjectId("project-001"),
            "Sample",
            new ValidatedProjectRoot("C:\\Projects\\sample"),
            DateTimeOffset.UtcNow);
        var catalog = new FakeCatalog(new RegisteredProjectCatalogSnapshot(
            [project],
            project.Id));

        var snapshot = await catalog.ReadAsync();

        Assert.Single(snapshot.Projects);
        Assert.Equal(project.Id, snapshot.SelectedProjectId);
        Assert.Equal(0, catalog.SaveCallCount);
    }

    [Fact]
    public void ErrorContractContainsOnlyStableProjectErrorMeaning()
    {
        var error = new ProjectError(ProjectErrorCode.UnsupportedFileSystem);

        Assert.Equal(ProjectErrorCode.UnsupportedFileSystem, error.Code);
    }

    private sealed class FakeCatalog(RegisteredProjectCatalogSnapshot snapshot) : IRegisteredProjectCatalog
    {
        public int SaveCallCount { get; private set; }

        public Task<RegisteredProjectCatalogSnapshot> ReadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshot);

        public Task SaveAsync(RegisteredProjectCatalogSnapshot value, CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            return Task.CompletedTask;
        }
    }
}
