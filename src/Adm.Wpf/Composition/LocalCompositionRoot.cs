using Adm.Application.Foundation;
using Adm.Application.ExecutionProfiles;
using Adm.Application.Projects;
using Adm.Core.Projects;
using Adm.Wpf.Configuration;
using Adm.Wpf.LocalChannel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adm.Wpf.Composition;

public sealed class LocalCompositionRoot : IDisposable
{
    private static readonly JsonSerializerOptions ProfileJsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
    private readonly CancellationTokenSource shutdown = new();
    private readonly CancellationToken shutdownToken;
    private readonly LocalChannelDispatcher dispatcher;
    private readonly ExecutionProfileService executionProfiles;
    private readonly RegisterLocalProjectUseCase? registerProject;
    private readonly UnregisterLocalProjectUseCase? unregisterProject;
    private readonly ListLocalProjectsUseCase? listProjects;
    private readonly SelectLocalProjectUseCase? selectProject;

    public LocalCompositionRoot(
        ExecutionProfileService? executionProfiles = null,
        RegisterLocalProjectUseCase? registerProject = null,
        UnregisterLocalProjectUseCase? unregisterProject = null,
        ListLocalProjectsUseCase? listProjects = null,
        SelectLocalProjectUseCase? selectProject = null)
    {
        shutdownToken = shutdown.Token;
        this.executionProfiles = executionProfiles ?? new ExecutionProfileService(new JsonExecutionProfileStore());
        this.registerProject = registerProject;
        this.unregisterProject = unregisterProject;
        this.listProjects = listProjects;
        this.selectProject = selectProject;
        var foundationStatus = new GetFoundationStatusUseCase();
        var handlers = new Dictionary<string, LocalChannelHandler>(StringComparer.Ordinal)
        {
            ["getFoundationStatus"] = (request, cancellationToken) =>
                ExecuteFoundationStatusAsync(foundationStatus, cancellationToken),
            ["executionProfile.get"] = (request, cancellationToken) =>
                GetExecutionProfileAsync(this.executionProfiles, cancellationToken),
            ["executionProfile.update"] = UpdateExecutionProfileAsync,
        };

        if (registerProject is not null && unregisterProject is not null &&
            listProjects is not null && selectProject is not null)
        {
            handlers["project.list"] = HandleProjectListAsync;
            handlers["project.register"] = HandleProjectRegisterAsync;
            handlers["project.unregister"] = HandleProjectUnregisterAsync;
            handlers["project.select"] = HandleProjectSelectAsync;
        }

        dispatcher = new LocalChannelDispatcher(LocalChannelOperationRegistry.FromHandlers(handlers));
    }

    private static async Task<object?> ExecuteFoundationStatusAsync(
        GetFoundationStatusUseCase useCase,
        CancellationToken cancellationToken) =>
        await useCase.ExecuteAsync(cancellationToken);

    private async Task<object?> UpdateExecutionProfileAsync(LocalChannelRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var update = JsonSerializer.Deserialize<ExecutionProfileUpdate>(request.Payload.GetRawText(), ProfileJsonOptions)
                ?? throw new ExecutionProfileValidationException("invalid_profile");
            return await executionProfiles.UpdateAsync(update, cancellationToken);
        }
        catch (ExecutionProfileValidationException)
        {
            throw new LocalChannelProtocolException("invalid_request", "errors.localChannel.invalidRequest", request.RequestId);
        }
        catch (ExecutionProfileStorageException)
        {
            throw new LocalChannelProtocolException("handler_failed", "errors.localChannel.handlerFailed", request.RequestId);
        }
    }

    private static async Task<object?> GetExecutionProfileAsync(ExecutionProfileService service, CancellationToken cancellationToken) =>
        await service.GetAsync(cancellationToken);

    private async Task<object?> HandleProjectListAsync(
        LocalChannelRequest request,
        CancellationToken cancellationToken)
    {
        RequirePayloadObject(request, []);
        var result = await listProjects!.ListAsync(cancellationToken);
        ThrowForProjectError(result.Error, request.RequestId);
        return new
        {
            projects = result.Projects.Select(project => ToProjectDto(project, result.SelectedProjectId)).ToArray(),
            selectedProjectId = result.SelectedProjectId?.Value,
            warnings = result.Warnings.Select(warning => new
            {
                projectId = warning.ProjectId.Value,
                code = ToProjectErrorCode(warning.Code),
            }).ToArray(),
        };
    }

    private async Task<object?> HandleProjectRegisterAsync(
        LocalChannelRequest request,
        CancellationToken cancellationToken)
    {
        var payload = DeserializePayload<ProjectRegisterPayload>(request, ["root", "displayName"]);
        if (string.IsNullOrWhiteSpace(payload.Root))
            throw InvalidProjectRequest(request.RequestId);

        var result = await registerProject!.RegisterAsync(
            new RegisterProjectInput(new ProjectRootInput(payload.Root), payload.DisplayName),
            cancellationToken);
        ThrowForProjectError(result.Error, request.RequestId);
        return new { project = ToProjectDto(result.Project!, null) };
    }

    private async Task<object?> HandleProjectUnregisterAsync(
        LocalChannelRequest request,
        CancellationToken cancellationToken)
    {
        var payload = DeserializePayload<ProjectIdPayload>(request, ["projectId"]);
        var projectId = ParseProjectId(payload.ProjectId, request.RequestId);
        var result = await unregisterProject!.UnregisterAsync(
            new UnregisterProjectInput(projectId),
            cancellationToken);
        ThrowForProjectError(result.Error, request.RequestId);
        return new { projectId = result.ProjectId.Value };
    }

    private async Task<object?> HandleProjectSelectAsync(
        LocalChannelRequest request,
        CancellationToken cancellationToken)
    {
        var payload = DeserializePayload<ProjectSelectPayload>(request, ["projectId"]);
        var projectId = string.IsNullOrWhiteSpace(payload.ProjectId)
            ? (ProjectId?)null
            : ParseProjectId(payload.ProjectId, request.RequestId);
        var result = await selectProject!.SelectAsync(
            new SelectProjectInput(projectId),
            cancellationToken);
        ThrowForProjectError(result.Error, request.RequestId);
        return new { selectedProjectId = result.SelectedProjectId?.Value };
    }

    private static T DeserializePayload<T>(LocalChannelRequest request, string[] expectedProperties)
    {
        RequirePayloadObject(request, expectedProperties);
        try
        {
            return JsonSerializer.Deserialize<T>(request.Payload.GetRawText(), ProjectJsonOptions)
                ?? throw InvalidProjectRequest(request.RequestId);
        }
        catch (JsonException)
        {
            throw InvalidProjectRequest(request.RequestId);
        }
    }

    private static void RequirePayloadObject(LocalChannelRequest request, string[] expectedProperties)
    {
        if (request.Payload.ValueKind != JsonValueKind.Object)
            throw InvalidProjectRequest(request.RequestId);

        var properties = request.Payload.EnumerateObject().Select(property => property.Name).ToArray();
        if (properties.Length != expectedProperties.Length ||
            properties.Distinct(StringComparer.Ordinal).Count() != properties.Length ||
            expectedProperties.Any(property => !properties.Contains(property, StringComparer.Ordinal)))
        {
            throw InvalidProjectRequest(request.RequestId);
        }
    }

    private static ProjectId ParseProjectId(string? value, string requestId)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw InvalidProjectRequest(requestId);

        try
        {
            return new ProjectId(value);
        }
        catch (ArgumentException)
        {
            throw InvalidProjectRequest(requestId);
        }
    }

    private static void ThrowForProjectError(ProjectError? error, string requestId)
    {
        if (error is null)
            return;

        var operationError = error.Code == ProjectErrorCode.ProjectNotRegistered
            ? "operation_not_allowed"
            : "handler_failed";
        var messageKey = error.Code == ProjectErrorCode.ProjectNotRegistered
            ? "errors.localChannel.operationNotAllowed"
            : "errors.localChannel.handlerFailed";
        throw new LocalChannelProtocolException(operationError, messageKey, requestId);
    }

    private static LocalChannelProtocolException InvalidProjectRequest(string requestId) =>
        new("invalid_request", "errors.localChannel.invalidRequest", requestId);

    private static object ToProjectDto(RegisteredProject project, ProjectId? selectedProjectId) => new
    {
        id = project.Id.Value,
        displayName = project.DisplayName,
        root = project.Root.CanonicalPath,
        registeredAtUtc = project.RegisteredAtUtc,
        isSelected = selectedProjectId.HasValue && project.Id == selectedProjectId.Value,
    };

    private static string ToProjectErrorCode(ProjectErrorCode code) => code switch
    {
        ProjectErrorCode.AccessDenied => "access_denied",
        ProjectErrorCode.UnsupportedFileSystem => "unsupported_filesystem",
        ProjectErrorCode.InvalidRoot => "invalid_root",
        _ => "invalid_root",
    };

    private sealed record ProjectRegisterPayload(string? Root, string? DisplayName);
    private sealed record ProjectIdPayload(string? ProjectId);
    private sealed record ProjectSelectPayload(string? ProjectId);

    private static readonly JsonSerializerOptions ProjectJsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public Task<string> DispatchAsync(string json, string? source) =>
        dispatcher.DispatchAsync(json, source, shutdownToken);

    public void Dispose()
    {
        shutdown.Cancel();
        shutdown.Dispose();
    }
}
