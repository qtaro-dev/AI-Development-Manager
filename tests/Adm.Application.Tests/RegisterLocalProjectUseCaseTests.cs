using Adm.Application.Projects;
using Adm.Core.Projects;

namespace Adm.Application.Tests;

public sealed class RegisterLocalProjectUseCaseTests
{
    private static readonly DateTimeOffset RegisteredAt =
        new(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ValidRootIsRegisteredAfterValidationAndPersisted()
    {
        var root = new ValidatedProjectRoot("C:\\Projects\\日本語 Project");
        var catalog = new FakeCatalog();
        var useCase = CreateUseCase(root, catalog);

        var result = await useCase.RegisterAsync(new RegisterProjectInput(
            new ProjectRootInput(root.CanonicalPath), "表示名"));

        Assert.True(result.IsSuccess);
        Assert.Equal("project-001", result.Project!.Id.Value);
        Assert.Equal("表示名", result.Project.DisplayName);
        Assert.Equal(root, result.Project.Root);
        Assert.Single(catalog.SavedSnapshot!.Projects);
        Assert.Equal(["validate", "read", "id", "clock", "save"], catalog.Events);
    }

    [Fact]
    public async Task MissingDisplayNameUsesTheCanonicalRootLeaf()
    {
        var root = new ValidatedProjectRoot("C:\\Projects\\日本語 Project\\");
        var result = await CreateUseCase(root).RegisterAsync(new RegisterProjectInput(
            new ProjectRootInput(root.CanonicalPath), "  "));

        Assert.True(result.IsSuccess);
        Assert.Equal("日本語 Project", result.Project!.DisplayName);
    }

    [Fact]
    public async Task DuplicateRootIsRejectedIgnoringCaseAndTrailingSeparator()
    {
        var existing = CreateProject("existing", "C:\\Projects\\Sample\\");
        var catalog = new FakeCatalog([existing]);
        var useCase = CreateUseCase(new ValidatedProjectRoot("c:\\projects\\sample"), catalog);

        var result = await useCase.RegisterAsync(new RegisterProjectInput(
            new ProjectRootInput("c:\\projects\\sample"), null));

        Assert.False(result.IsSuccess);
        Assert.Equal(ProjectErrorCode.DuplicateProject, result.Error!.Code);
        Assert.Equal(["validate", "read"], catalog.Events);
        Assert.Null(catalog.SavedSnapshot);
    }

    [Fact]
    public async Task DuplicateGeneratedIdIsRejectedWithoutSaving()
    {
        var existing = CreateProject("project-001", "C:\\Projects\\Existing");
        var catalog = new FakeCatalog([existing]);
        var useCase = CreateUseCase(new ValidatedProjectRoot("C:\\Projects\\New"), catalog);

        var result = await useCase.RegisterAsync(new RegisterProjectInput(
            new ProjectRootInput("C:\\Projects\\New"), null));

        Assert.Equal(ProjectErrorCode.DuplicateProject, result.Error!.Code);
        Assert.Null(catalog.SavedSnapshot);
    }

    [Fact]
    public async Task InvalidRootIsReturnedWithoutReadingOrSaving()
    {
        var catalog = new FakeCatalog();
        var useCase = CreateUseCase(
            new ValidatedProjectRoot("C:\\ignored"),
            catalog,
            ProjectRootValidationResult.Invalid(ProjectErrorCode.AccessDenied));

        var result = await useCase.RegisterAsync(new RegisterProjectInput(
            new ProjectRootInput("C:\\input"), null));

        Assert.False(result.IsSuccess);
        Assert.Equal(ProjectErrorCode.AccessDenied, result.Error!.Code);
        Assert.Equal(["validate"], catalog.Events);
    }

    [Fact]
    public async Task UnsupportedRootErrorIsPreserved()
    {
        var catalog = new FakeCatalog();
        var useCase = CreateUseCase(
            new ValidatedProjectRoot("C:\\ignored"),
            catalog,
            ProjectRootValidationResult.Invalid(ProjectErrorCode.UnsupportedFileSystem));

        var result = await useCase.RegisterAsync(new RegisterProjectInput(
            new ProjectRootInput("C:\\input"), null));

        Assert.Equal(ProjectErrorCode.UnsupportedFileSystem, result.Error!.Code);
    }

    [Fact]
    public async Task CatalogFailureReturnsSafeErrorAndDoesNotReportRegistration()
    {
        var catalog = new FakeCatalog { ReadException = new ProjectCatalogException("catalog_corrupt") };
        var useCase = CreateUseCase(new ValidatedProjectRoot("C:\\Projects\\Sample"), catalog);

        var result = await useCase.RegisterAsync(new RegisterProjectInput(
            new ProjectRootInput("C:\\Projects\\Sample"), null));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Project);
        Assert.Equal(ProjectErrorCode.PersistenceFailed, result.Error!.Code);
        Assert.DoesNotContain("catalog_corrupt", result.Error.ToString());
    }

    [Fact]
    public async Task SaveFailureDoesNotReportRegistrationOrMutateInputCatalog()
    {
        var existing = CreateProject("existing", "C:\\Projects\\Existing");
        var catalog = new FakeCatalog([existing])
        {
            SaveException = new IOException("private path")
        };
        var useCase = CreateUseCase(new ValidatedProjectRoot("C:\\Projects\\New"), catalog);

        var result = await useCase.RegisterAsync(new RegisterProjectInput(
            new ProjectRootInput("C:\\Projects\\New"), null));

        Assert.Equal(ProjectErrorCode.PersistenceFailed, result.Error!.Code);
        Assert.Null(result.Project);
        Assert.Single(catalog.InitialSnapshot.Projects);
        Assert.Null(catalog.SavedSnapshot);
    }

    [Fact]
    public async Task CancellationDuringValidationIsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var useCase = CreateUseCase(new ValidatedProjectRoot("C:\\Projects\\Sample"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.RegisterAsync(
            new RegisterProjectInput(new ProjectRootInput("C:\\Projects\\Sample"), null),
            cancellation.Token));
    }

    [Fact]
    public async Task CancellationDuringSaveIsPropagatedAndSaveReceivesToken()
    {
        var catalog = new FakeCatalog { SaveException = new OperationCanceledException() };
        var useCase = CreateUseCase(new ValidatedProjectRoot("C:\\Projects\\Sample"), catalog);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.RegisterAsync(
            new RegisterProjectInput(new ProjectRootInput("C:\\Projects\\Sample"), null)));

        Assert.True(catalog.SaveWasCalled);
    }

    private static RegisterLocalProjectUseCase CreateUseCase(
        ValidatedProjectRoot root,
        FakeCatalog? catalog = null,
        ProjectRootValidationResult? validation = null) =>
        new(
            new FakeRootValidator(validation ?? ProjectRootValidationResult.Valid(root), catalog),
            catalog ?? new FakeCatalog(),
            new FakeClock(RegisteredAt, catalog),
            new FakeIdGenerator(new ProjectId("project-001"), catalog));

    private static RegisteredProject CreateProject(string id, string root) => new(
        new ProjectId(id),
        id,
        new ValidatedProjectRoot(root),
        RegisteredAt);

    private sealed class FakeRootValidator(
        ProjectRootValidationResult result,
        FakeCatalog? catalog) : IProjectRootValidator
    {
        public Task<ProjectRootValidationResult> ValidateAsync(
            ProjectRootInput root,
            CancellationToken cancellationToken = default)
        {
            catalog?.Events.Add("validate");
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(result);
        }
    }

    private sealed class FakeCatalog : IRegisteredProjectCatalog
    {
        public FakeCatalog(IReadOnlyList<RegisteredProject>? projects = null)
        {
            InitialSnapshot = new RegisteredProjectCatalogSnapshot(projects ?? [], null);
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
            if (ReadException is not null) throw ReadException;
            return Task.FromResult(InitialSnapshot);
        }

        public Task SaveAsync(RegisteredProjectCatalogSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            Events.Add("save");
            SaveWasCalled = true;
            cancellationToken.ThrowIfCancellationRequested();
            if (SaveException is not null) throw SaveException;
            SavedSnapshot = snapshot;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeClock(DateTimeOffset value, FakeCatalog? catalog) : IProjectClock
    {
        public DateTimeOffset UtcNow
        {
            get
            {
                catalog?.Events.Add("clock");
                return value;
            }
        }
    }

    private sealed class FakeIdGenerator(ProjectId value, FakeCatalog? catalog) : IProjectIdGenerator
    {
        public ProjectId Create()
        {
            catalog?.Events.Add("id");
            return value;
        }
    }
}
