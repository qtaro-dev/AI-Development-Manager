using Adm.Application.Projects;
using Adm.Core.Projects;

namespace Adm.Application.Tests;

public sealed class UnregisterLocalProjectUseCaseTests
{
    private static readonly DateTimeOffset RegisteredAt =
        new(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UnregistersOnlyTheRequestedNonSelectedProject()
    {
        var target = CreateProject("target", "C:\\Projects\\Target");
        var selected = CreateProject("selected", "C:\\Projects\\Selected", isSelected: true);
        var other = CreateProject("other", "C:\\Projects\\Other");
        var catalog = new FakeCatalog([target, selected, other], selected.Id);

        var result = await new UnregisterLocalProjectUseCase(catalog).UnregisterAsync(
            new UnregisterProjectInput(target.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(target.Id, result.ProjectId);
        Assert.Equal(["read", "save"], catalog.Events);
        Assert.Equal([selected.Id, other.Id], catalog.SavedSnapshot!.Projects.Select(project => project.Id));
        Assert.Equal(selected.Id, catalog.SavedSnapshot.SelectedProjectId);
    }

    [Fact]
    public async Task UnregisteringSelectedProjectClearsSelectionInTheSameSave()
    {
        var selected = CreateProject("selected", "C:\\Projects\\Selected", isSelected: true);
        var other = CreateProject("other", "C:\\Projects\\Other");
        var catalog = new FakeCatalog([selected, other], selected.Id);

        var result = await new UnregisterLocalProjectUseCase(catalog).UnregisterAsync(
            new UnregisterProjectInput(selected.Id));

        Assert.True(result.IsSuccess);
        Assert.Single(catalog.SavedSnapshot!.Projects);
        Assert.Equal(other.Id, catalog.SavedSnapshot.Projects[0].Id);
        Assert.Null(catalog.SavedSnapshot.SelectedProjectId);
    }

    [Fact]
    public async Task MissingProjectIsRejectedWithoutSaving()
    {
        var existing = CreateProject("existing", "C:\\Projects\\Existing");
        var catalog = new FakeCatalog([existing], existing.Id);

        var result = await new UnregisterLocalProjectUseCase(catalog).UnregisterAsync(
            new UnregisterProjectInput(new ProjectId("missing")));

        Assert.False(result.IsSuccess);
        Assert.Equal(ProjectErrorCode.ProjectNotRegistered, result.Error!.Code);
        Assert.Equal(["read"], catalog.Events);
        Assert.Null(catalog.SavedSnapshot);
    }

    [Fact]
    public async Task SaveFailureDoesNotReportSuccessOrChangeTheInputSnapshot()
    {
        var target = CreateProject("target", "C:\\Projects\\Target");
        var other = CreateProject("other", "C:\\Projects\\Other");
        var catalog = new FakeCatalog([target, other], target.Id)
        {
            SaveException = new IOException("private path")
        };

        var result = await new UnregisterLocalProjectUseCase(catalog).UnregisterAsync(
            new UnregisterProjectInput(target.Id));

        Assert.False(result.IsSuccess);
        Assert.Equal(ProjectErrorCode.PersistenceFailed, result.Error!.Code);
        Assert.Null(catalog.SavedSnapshot);
        Assert.Equal(2, catalog.InitialSnapshot.Projects.Count);
        Assert.Equal(target.Id, catalog.InitialSnapshot.SelectedProjectId);
    }

    [Fact]
    public async Task CatalogFailureIsConvertedToSafeError()
    {
        var target = CreateProject("target", "C:\\Projects\\Target");
        var catalog = new FakeCatalog([target], target.Id)
        {
            ReadException = new ProjectCatalogException("catalog_corrupt")
        };

        var result = await new UnregisterLocalProjectUseCase(catalog).UnregisterAsync(
            new UnregisterProjectInput(target.Id));

        Assert.Equal(ProjectErrorCode.PersistenceFailed, result.Error!.Code);
        Assert.DoesNotContain("catalog_corrupt", result.Error.ToString());
        Assert.Null(catalog.SavedSnapshot);
    }

    [Fact]
    public async Task CancellationDuringReadIsPropagated()
    {
        var catalog = new FakeCatalog
        {
            ReadException = new OperationCanceledException()
        };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new UnregisterLocalProjectUseCase(catalog).UnregisterAsync(
                new UnregisterProjectInput(new ProjectId("target")), cancellation.Token));

        Assert.Null(catalog.SavedSnapshot);
    }

    [Fact]
    public async Task CancellationDuringSaveIsPropagated()
    {
        var target = CreateProject("target", "C:\\Projects\\Target");
        var catalog = new FakeCatalog([target], target.Id)
        {
            SaveException = new OperationCanceledException()
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new UnregisterLocalProjectUseCase(catalog).UnregisterAsync(
                new UnregisterProjectInput(target.Id)));

        Assert.True(catalog.SaveWasCalled);
    }

    [Fact]
    public async Task RootValueIsKeptAsCatalogDataAndNoFilesystemDeletionIsAttempted()
    {
        var target = CreateProject("target", "C:\\Projects\\Target");
        var catalog = new FakeCatalog([target], null);

        var result = await new UnregisterLocalProjectUseCase(catalog).UnregisterAsync(
            new UnregisterProjectInput(target.Id));

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("C:\\Projects\\Target", catalog.Events);
        Assert.Empty(catalog.SavedSnapshot!.Projects);
    }

    private static RegisteredProject CreateProject(
        string id,
        string root,
        bool isSelected = false) => new(
            new ProjectId(id),
            id,
            new ValidatedProjectRoot(root),
            RegisteredAt,
            isSelected);

    private sealed class FakeCatalog : IRegisteredProjectCatalog
    {
        public FakeCatalog(IReadOnlyList<RegisteredProject> projects, ProjectId? selectedProjectId)
        {
            InitialSnapshot = new RegisteredProjectCatalogSnapshot(projects, selectedProjectId);
        }

        public FakeCatalog()
            : this([], null)
        {
        }

        public RegisteredProjectCatalogSnapshot InitialSnapshot { get; }
        public RegisteredProjectCatalogSnapshot? SavedSnapshot { get; private set; }
        public List<string> Events { get; } = [];
        public Exception? ReadException { get; init; }
        public Exception? SaveException { get; init; }
        public bool SaveWasCalled { get; private set; }

        public Task<RegisteredProjectCatalogSnapshot> ReadAsync(CancellationToken cancellationToken = default)
        {
            Events.Add("read");
            cancellationToken.ThrowIfCancellationRequested();
            if (ReadException is not null)
                throw ReadException;
            return Task.FromResult(InitialSnapshot);
        }

        public Task SaveAsync(
            RegisteredProjectCatalogSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            Events.Add("save");
            SaveWasCalled = true;
            cancellationToken.ThrowIfCancellationRequested();
            if (SaveException is not null)
                throw SaveException;
            SavedSnapshot = snapshot;
            return Task.CompletedTask;
        }
    }
}
