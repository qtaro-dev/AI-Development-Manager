using Adm.Application.Projects;
using Adm.Core.Projects;
using Adm.Infrastructure.Windows.Projects;

namespace Adm.Infrastructure.Windows.Tests;

public sealed class RegisteredProjectCatalogTests
{
    [Fact]
    public async Task MissingCatalogReturnsEmptySnapshot()
    {
        using var fixture = new CatalogFixture();

        var snapshot = await fixture.Catalog.ReadAsync();

        Assert.Empty(snapshot.Projects);
        Assert.Null(snapshot.SelectedProjectId);
    }

    [Fact]
    public async Task SaveReloadAndClearSelectionRoundTripsDeterministically()
    {
        using var fixture = new CatalogFixture();
        var first = fixture.CreateProject("project-b", "B");
        var second = fixture.CreateProject("project-a", "A");

        await fixture.Catalog.SaveAsync(new RegisteredProjectCatalogSnapshot([first, second], first.Id));

        var json = await File.ReadAllTextAsync(fixture.Path);
        Assert.Equal(
            "{\"schemaVersion\":1,\"projects\":[{\"id\":\"project-a\",\"displayName\":\"A\",\"root\":\"" +
            second.Root.CanonicalPath.Replace("\\", "\\\\") +
            "\",\"registeredAtUtc\":\"2026-01-02T00:00:00+00:00\"},{\"id\":\"project-b\",\"displayName\":\"B\",\"root\":\"" +
            first.Root.CanonicalPath.Replace("\\", "\\\\") +
            "\",\"registeredAtUtc\":\"2026-01-02T00:00:00+00:00\"}],\"selectedProjectId\":\"project-b\"}",
            json);

        var restored = await fixture.Catalog.ReadAsync();
        Assert.Equal(["project-a", "project-b"], restored.Projects.Select(project => project.Id.Value));
        Assert.Equal(first.Id, restored.SelectedProjectId);
        Assert.True(restored.Projects.Single(project => project.Id == first.Id).IsSelected);

        await fixture.Catalog.SaveAsync(new RegisteredProjectCatalogSnapshot(restored.Projects, null));
        var cleared = await fixture.Catalog.ReadAsync();
        Assert.Null(cleared.SelectedProjectId);
        Assert.All(cleared.Projects, project => Assert.False(project.IsSelected));
    }

    [Fact]
    public async Task DuplicateIdAndRootAreRejectedWithoutReplacingExistingCatalog()
    {
        using var fixture = new CatalogFixture();
        var original = fixture.CreateProject("project-001", "Original");
        await fixture.Catalog.SaveAsync(new RegisteredProjectCatalogSnapshot([original], original.Id));
        var originalJson = await File.ReadAllTextAsync(fixture.Path);

        var duplicateId = fixture.CreateProject("project-001", "Duplicate ID");
        var idException = await Assert.ThrowsAsync<ProjectCatalogException>(() =>
            fixture.Catalog.SaveAsync(new RegisteredProjectCatalogSnapshot([original, duplicateId], original.Id)));
        Assert.Equal("duplicate_project_id", idException.Code);

        var duplicateRoot = new RegisteredProject(
            new ProjectId("project-002"),
            "Duplicate Root",
            new ValidatedProjectRoot(original.Root.CanonicalPath.ToUpperInvariant()),
            original.RegisteredAtUtc);
        var rootException = await Assert.ThrowsAsync<ProjectCatalogException>(() =>
            fixture.Catalog.SaveAsync(new RegisteredProjectCatalogSnapshot([original, duplicateRoot], original.Id)));
        Assert.Equal("duplicate_project_root", rootException.Code);
        Assert.Equal(originalJson, await File.ReadAllTextAsync(fixture.Path));
    }

    [Fact]
    public async Task CorruptAndUnknownSchemaAreSafeErrorsAndPreserveEvidence()
    {
        using var fixture = new CatalogFixture();
        await File.WriteAllTextAsync(fixture.Path, "{not-json");
        var corruptException = await Assert.ThrowsAsync<ProjectCatalogException>(() => fixture.Catalog.ReadAsync());
        Assert.Equal("catalog_corrupt", corruptException.Code);
        Assert.Equal("{not-json", await File.ReadAllTextAsync(fixture.Path));

        await File.WriteAllTextAsync(fixture.Path, "{\"schemaVersion\":99,\"projects\":[],\"selectedProjectId\":null}");
        var schemaException = await Assert.ThrowsAsync<ProjectCatalogException>(() => fixture.Catalog.ReadAsync());
        Assert.Equal("catalog_schema_unsupported", schemaException.Code);
        Assert.Contains("\"schemaVersion\":99", await File.ReadAllTextAsync(fixture.Path));
    }

    [Fact]
    public async Task InvalidSelectedProjectIsRejected()
    {
        using var fixture = new CatalogFixture();
        var project = fixture.CreateProject("project-001", "Project");

        var exception = await Assert.ThrowsAsync<ProjectCatalogException>(() =>
            fixture.Catalog.SaveAsync(new RegisteredProjectCatalogSnapshot([project], new ProjectId("missing"))));

        Assert.Equal("selected_project_missing", exception.Code);
        Assert.False(File.Exists(fixture.Path));
    }

    [Fact]
    public async Task WriteFailureDoesNotCreateCatalogOrLeakTemporaryFile()
    {
        using var fixture = new CatalogFixture(createCatalogParent: false);
        System.IO.Directory.CreateDirectory(fixture.Root);
        var blocker = Path.Combine(fixture.Root, "blocker");
        await File.WriteAllTextAsync(blocker, "not a directory");
        var blockedPath = Path.Combine(blocker, "registered-projects.json");
        var catalog = new WindowsRegisteredProjectCatalog(blockedPath);

        var exception = await Assert.ThrowsAsync<ProjectCatalogException>(() =>
            catalog.SaveAsync(new RegisteredProjectCatalogSnapshot([fixture.CreateProject("project-001", "Project")], null)));

        Assert.Equal("catalog_write_failed", exception.Code);
        Assert.Empty(Directory.EnumerateFiles(fixture.Root, ".adm-tmp-*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task CancellationIsPropagatedAndDoesNotCreateCatalog()
    {
        using var fixture = new CatalogFixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Catalog.SaveAsync(
            new RegisteredProjectCatalogSnapshot([fixture.CreateProject("project-001", "Project")], null),
            cancellation.Token));

        Assert.False(File.Exists(fixture.Path));
    }

    [Fact]
    public async Task ConcurrentSavesLeaveOneCompleteCatalogAndNoTemporaryFiles()
    {
        using var fixture = new CatalogFixture();
        var saves = Enumerable.Range(1, 12).Select(index =>
            fixture.Catalog.SaveAsync(new RegisteredProjectCatalogSnapshot(
                [fixture.CreateProject($"project-{index:000}", $"Project {index}")], null)));

        await Task.WhenAll(saves);

        var snapshot = await fixture.Catalog.ReadAsync();
        Assert.Single(snapshot.Projects);
        Assert.Empty(Directory.EnumerateFiles(fixture.Directory, ".adm-tmp-*", SearchOption.TopDirectoryOnly));
    }

    private sealed class CatalogFixture : IDisposable
    {
        public CatalogFixture(bool createCatalogParent = true)
        {
            Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Adm P2-003 {Guid.NewGuid():N}");
            Directory = System.IO.Path.Combine(Root, "Config");
            if (createCatalogParent)
                System.IO.Directory.CreateDirectory(Directory);
            Path = System.IO.Path.Combine(Directory, "registered-projects.json");
            Catalog = new WindowsRegisteredProjectCatalog(Path);
        }

        public string Root { get; }
        public string Directory { get; }
        public string Path { get; }
        public WindowsRegisteredProjectCatalog Catalog { get; }

        public RegisteredProject CreateProject(string id, string displayName) => new(
            new ProjectId(id),
            displayName,
            new ValidatedProjectRoot(System.IO.Path.Combine(Root, id)),
            new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero));

        public void Dispose()
        {
            if (System.IO.Directory.Exists(Root))
                System.IO.Directory.Delete(Root, recursive: true);
        }
    }
}
