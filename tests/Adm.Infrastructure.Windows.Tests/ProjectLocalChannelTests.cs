using System.Text.Json;
using Adm.Application.Projects;
using Adm.Core.Projects;
using Adm.Wpf.Composition;
using Adm.Wpf.LocalChannel;

namespace Adm.Infrastructure.Windows.Tests;

public sealed class ProjectLocalChannelTests
{
    private const string LocalSource = "https://app.ai-development-manager.local/index.html";

    [Fact]
    public async Task ProjectOperationsRoundTripThroughExplicitLocalChannel()
    {
        var catalog = new FakeCatalog();
        using var composition = CreateComposition(catalog);

        var register = await composition.DispatchAsync(
            Request("project.register", "{\"root\":\"C:\\\\Projects\\\\Sample\",\"displayName\":\"Sample\"}"),
            LocalSource);
        using var registerDocument = JsonDocument.Parse(register);
        var projectId = registerDocument.RootElement.GetProperty("result")
            .GetProperty("project").GetProperty("id").GetString();
        Assert.NotNull(projectId);

        var list = await composition.DispatchAsync(Request("project.list", "{}"), LocalSource);
        using var listDocument = JsonDocument.Parse(list);
        Assert.Equal("response", listDocument.RootElement.GetProperty("kind").GetString());
        Assert.Equal(projectId, listDocument.RootElement.GetProperty("result")
            .GetProperty("projects")[0].GetProperty("id").GetString());

        var select = await composition.DispatchAsync(
            Request("project.select", $"{{\"projectId\":\"{projectId}\"}}"),
            LocalSource);
        Assert.Contains("\"selectedProjectId\":\"" + projectId + "\"", select);

        var unregister = await composition.DispatchAsync(
            Request("project.unregister", $"{{\"projectId\":\"{projectId}\"}}"),
            LocalSource);
        Assert.Contains("\"projectId\":\"" + projectId + "\"", unregister);
        Assert.Empty(catalog.Snapshot.Projects);
    }

    [Fact]
    public async Task ProjectPayloadUnknownFieldIsRejectedByTheHandlerBoundary()
    {
        using var composition = CreateComposition(new FakeCatalog());

        var response = await composition.DispatchAsync(
            Request("project.list", "{\"extra\":true}"),
            LocalSource);

        Assert.Contains("\"code\":\"invalid_request\"", response);
        Assert.DoesNotContain("extra", response);
    }

    [Fact]
    public async Task UnregisteredProjectMapsToSafeOperationError()
    {
        using var composition = CreateComposition(new FakeCatalog());

        var response = await composition.DispatchAsync(
            Request("project.select", "{\"projectId\":\"missing\"}"),
            LocalSource);

        Assert.Contains("\"code\":\"operation_not_allowed\"", response);
        Assert.DoesNotContain("missing", response);
    }

    [Fact]
    public async Task CatalogFailureDoesNotExposeInternalDetails()
    {
        using var composition = CreateComposition(new FakeCatalog
        {
            ReadException = new IOException("C:\\private\\catalog-token.json")
        });

        var response = await composition.DispatchAsync(Request("project.list", "{}"), LocalSource);

        Assert.Contains("\"code\":\"handler_failed\"", response);
        Assert.DoesNotContain("catalog-token", response);
        Assert.DoesNotContain("IOException", response);
    }

    private static LocalCompositionRoot CreateComposition(FakeCatalog catalog)
    {
        var validator = new FakeRootValidator();
        var register = new RegisterLocalProjectUseCase(
            validator,
            catalog,
            new FixedClock(),
            new FixedIdGenerator());
        return new LocalCompositionRoot(
            registerProject: register,
            unregisterProject: new UnregisterLocalProjectUseCase(catalog),
            listProjects: new ListLocalProjectsUseCase(catalog, validator),
            selectProject: new SelectLocalProjectUseCase(catalog));
    }

    private static string Request(string operation, string payload) =>
        $"{{\"version\":1,\"kind\":\"request\",\"requestId\":\"request-001\",\"operation\":\"{operation}\",\"payload\":{payload}}}";

    private sealed class FakeRootValidator : IProjectRootValidator
    {
        public Task<ProjectRootValidationResult> ValidateAsync(
            ProjectRootInput root,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProjectRootValidationResult.Valid(new ValidatedProjectRoot(root.Value)));
        }
    }

    private sealed class FixedClock : IProjectClock
    {
        public DateTimeOffset UtcNow => new(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class FixedIdGenerator : IProjectIdGenerator
    {
        public ProjectId Create() => new("project-001");
    }

    private sealed class FakeCatalog : IRegisteredProjectCatalog
    {
        public RegisteredProjectCatalogSnapshot Snapshot { get; private set; } = new([], null);
        public Exception? ReadException { get; init; }

        public Task<RegisteredProjectCatalogSnapshot> ReadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ReadException is not null)
                throw ReadException;
            return Task.FromResult(Snapshot);
        }

        public Task SaveAsync(RegisteredProjectCatalogSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }
}
