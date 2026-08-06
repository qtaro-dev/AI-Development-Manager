using Adm.Application.Projects;
using Adm.Core.Projects;

namespace Adm.Application.Tests;

public sealed class ListSelectLocalProjectUseCaseTests
{
    private static readonly DateTimeOffset RegisteredAt =
        new(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EmptyCatalogReturnsEmptyDeterministicList()
    {
        var catalog = new FakeCatalog();
        var result = await new ListLocalProjectsUseCase(catalog, new FakeRootValidator()).ListAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Projects);
        Assert.Null(result.SelectedProjectId);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task ListReturnsProjectsInDeterministicIdOrderAndPreservesSelection()
    {
        var projectB = CreateProject("project-b", "C:\\Projects\\B");
        var projectA = CreateProject("project-a", "C:\\Projects\\A");
        var catalog = new FakeCatalog([projectB, projectA], projectB.Id);
        var validator = new FakeRootValidator();
        var result = await new ListLocalProjectsUseCase(catalog, validator).ListAsync();

        Assert.Equal([projectA.Id, projectB.Id], result.Projects.Select(project => project.Id));
        Assert.Equal(projectB.Id, result.SelectedProjectId);
        Assert.Equal(2, validator.ValidationCount);
    }

    [Fact]
    public async Task MissingRootIsReportedAsWarningWithoutRemovingTheProject()
    {
        var missing = CreateProject("missing", "C:\\Projects\\Missing");
        var catalog = new FakeCatalog([missing], missing.Id);
        var validator = new FakeRootValidator(ProjectErrorCode.InvalidRoot);

        var result = await new ListLocalProjectsUseCase(catalog, validator).ListAsync();

        Assert.Single(result.Projects);
        Assert.Equal(missing.Id, result.Projects[0].Id);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(missing.Id, warning.ProjectId);
        Assert.Equal(ProjectErrorCode.InvalidRoot, warning.Code);
        Assert.Null(catalog.SavedSnapshot);
    }

    [Fact]
    public async Task AccessDeniedRootIsReportedAsWarningWithoutRemovingTheProject()
    {
        var denied = CreateProject("denied", "C:\\Projects\\Denied");
        var catalog = new FakeCatalog([denied], null);
        var result = await new ListLocalProjectsUseCase(
            catalog,
            new FakeRootValidator(ProjectErrorCode.AccessDenied)).ListAsync();

        Assert.Equal(ProjectErrorCode.AccessDenied, Assert.Single(result.Warnings).Code);
        Assert.Single(result.Projects);
        Assert.Null(catalog.SavedSnapshot);
    }

    [Fact]
    public async Task CatalogReadFailureReturnsSafeErrorWithoutExposingDetails()
    {
        var catalog = new FakeCatalog
        {
            ReadException = new ProjectCatalogException("catalog_corrupt")
        };

        var result = await new ListLocalProjectsUseCase(catalog, new FakeRootValidator()).ListAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ProjectErrorCode.PersistenceFailed, result.Error!.Code);
        Assert.DoesNotContain("catalog_corrupt", result.Error.ToString());
    }

    [Fact]
    public async Task ListCancellationIsPropagated()
    {
        var catalog = new FakeCatalog
        {
            ReadException = new OperationCanceledException()
        };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ListLocalProjectsUseCase(catalog, new FakeRootValidator()).ListAsync(cancellation.Token));
    }

    [Fact]
    public async Task SelectingProjectPersistsSelectionWithoutChangingProjectList()
    {
        var first = CreateProject("first", "C:\\Projects\\First");
        var second = CreateProject("second", "C:\\Projects\\Second");
        var catalog = new FakeCatalog([first, second], first.Id);

        var result = await new SelectLocalProjectUseCase(catalog).SelectAsync(
            new SelectProjectInput(second.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(second.Id, result.SelectedProjectId);
        Assert.Equal([first.Id, second.Id], catalog.SavedSnapshot!.Projects.Select(project => project.Id));
        Assert.Equal(second.Id, catalog.SavedSnapshot.SelectedProjectId);
    }

    [Fact]
    public async Task SelectingNullClearsSelection()
    {
        var selected = CreateProject("selected", "C:\\Projects\\Selected");
        var catalog = new FakeCatalog([selected], selected.Id);

        var result = await new SelectLocalProjectUseCase(catalog).SelectAsync(
            new SelectProjectInput(null));

        Assert.True(result.IsSuccess);
        Assert.Null(result.SelectedProjectId);
        Assert.Null(catalog.SavedSnapshot!.SelectedProjectId);
    }

    [Fact]
    public async Task SelectingUnregisteredProjectIsRejectedWithoutSaving()
    {
        var existing = CreateProject("existing", "C:\\Projects\\Existing");
        var catalog = new FakeCatalog([existing], existing.Id);

        var result = await new SelectLocalProjectUseCase(catalog).SelectAsync(
            new SelectProjectInput(new ProjectId("missing")));

        Assert.False(result.IsSuccess);
        Assert.Equal(ProjectErrorCode.ProjectNotRegistered, result.Error!.Code);
        Assert.Null(catalog.SavedSnapshot);
    }

    [Fact]
    public async Task SelectionSaveFailureReturnsSafeErrorAndKeepsExistingCatalog()
    {
        var existing = CreateProject("existing", "C:\\Projects\\Existing");
        var catalog = new FakeCatalog([existing], null)
        {
            SaveException = new IOException("private path")
        };

        var result = await new SelectLocalProjectUseCase(catalog).SelectAsync(
            new SelectProjectInput(existing.Id));

        Assert.False(result.IsSuccess);
        Assert.Equal(ProjectErrorCode.PersistenceFailed, result.Error!.Code);
        Assert.Null(catalog.SavedSnapshot);
        Assert.Null(catalog.InitialSnapshot.SelectedProjectId);
    }

    [Fact]
    public async Task SelectionCancellationIsPropagatedToSave()
    {
        var existing = CreateProject("existing", "C:\\Projects\\Existing");
        var catalog = new FakeCatalog([existing], null)
        {
            SaveException = new OperationCanceledException()
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new SelectLocalProjectUseCase(catalog).SelectAsync(
                new SelectProjectInput(existing.Id)));

        Assert.True(catalog.SaveWasCalled);
    }

    private static RegisteredProject CreateProject(string id, string root) => new(
        new ProjectId(id),
        id,
        new ValidatedProjectRoot(root),
        RegisteredAt);

    private sealed class FakeRootValidator(ProjectErrorCode? error = null) : IProjectRootValidator
    {
        public int ValidationCount { get; private set; }

        public Task<ProjectRootValidationResult> ValidateAsync(
            ProjectRootInput root,
            CancellationToken cancellationToken = default)
        {
            ValidationCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(error.HasValue
                ? ProjectRootValidationResult.Invalid(error.Value)
                : ProjectRootValidationResult.Valid(new ValidatedProjectRoot(root.Value)));
        }
    }

    private sealed class FakeCatalog : IRegisteredProjectCatalog
    {
        public FakeCatalog(
            IReadOnlyList<RegisteredProject>? projects = null,
            ProjectId? selectedProjectId = null)
        {
            InitialSnapshot = new RegisteredProjectCatalogSnapshot(projects ?? [], selectedProjectId);
        }

        public RegisteredProjectCatalogSnapshot InitialSnapshot { get; }
        public RegisteredProjectCatalogSnapshot? SavedSnapshot { get; private set; }
        public Exception? ReadException { get; init; }
        public Exception? SaveException { get; init; }
        public bool SaveWasCalled { get; private set; }

        public Task<RegisteredProjectCatalogSnapshot> ReadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ReadException is not null)
                throw ReadException;
            return Task.FromResult(InitialSnapshot);
        }

        public Task SaveAsync(
            RegisteredProjectCatalogSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            SaveWasCalled = true;
            cancellationToken.ThrowIfCancellationRequested();
            if (SaveException is not null)
                throw SaveException;
            SavedSnapshot = snapshot;
            return Task.CompletedTask;
        }
    }
}
