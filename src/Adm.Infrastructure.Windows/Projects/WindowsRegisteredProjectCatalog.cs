using System.Text.Json;
using System.Text.Json.Serialization;
using Adm.Application.Projects;
using Adm.Core.Projects;

namespace Adm.Infrastructure.Windows.Projects;

public sealed class WindowsRegisteredProjectCatalog
    : IRegisteredProjectCatalog
{
    public const int CurrentSchemaVersion = 1;

    private static readonly SemaphoreSlim AccessGate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = false,
    };

    private readonly string catalogPath;

    public WindowsRegisteredProjectCatalog(string? catalogPath = null)
    {
        this.catalogPath = string.IsNullOrWhiteSpace(catalogPath)
            ? GetDefaultPath()
            : Path.GetFullPath(catalogPath);
    }

    public static string GetDefaultPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AI Development Manager",
        "Config",
        "registered-projects.json");

    public async Task<RegisteredProjectCatalogSnapshot> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        await AccessGate.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(catalogPath))
                return new([], null);

            var json = await File.ReadAllTextAsync(catalogPath, cancellationToken);
            return Deserialize(json);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ProjectCatalogException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new ProjectCatalogException("catalog_read_failed", exception);
        }
        finally
        {
            AccessGate.Release();
        }
    }

    public async Task SaveAsync(
        RegisteredProjectCatalogSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        var document = CreateDocument(snapshot);
        await AccessGate.WaitAsync(cancellationToken);
        string? temporaryPath = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directory = Path.GetDirectoryName(catalogPath)
                ?? throw new ProjectCatalogException("catalog_path_invalid");
            Directory.CreateDirectory(directory);

            var fileName = Path.GetFileNameWithoutExtension(catalogPath);
            temporaryPath = Path.Combine(
                directory,
                $".adm-tmp-{fileName}-{Guid.NewGuid():N}.tmp");

            var originalExists = File.Exists(catalogPath);
            var originalAttributes = originalExists ? File.GetAttributes(catalogPath) : (FileAttributes?)null;

            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                options: FileOptions.SequentialScan))
            {
                await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (originalExists)
            {
                var backupPath = catalogPath + ".bak";
                File.Replace(temporaryPath, catalogPath, backupPath, ignoreMetadataErrors: true);
                temporaryPath = null;
                if (originalAttributes.HasValue)
                    File.SetAttributes(catalogPath, originalAttributes.Value);
            }
            else
            {
                File.Move(temporaryPath, catalogPath);
                temporaryPath = null;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ProjectCatalogException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw new ProjectCatalogException("catalog_write_failed", exception);
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            AccessGate.Release();
        }
    }

    private static CatalogDocument CreateDocument(RegisteredProjectCatalogSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var projects = snapshot.Projects ?? throw new ProjectCatalogException("catalog_invalid");
        var sortedProjects = projects
            .OrderBy(project => project.Id.Value, StringComparer.Ordinal)
            .ToArray();
        ValidateProjects(sortedProjects, snapshot.SelectedProjectId);

        return new CatalogDocument
        {
            SchemaVersion = CurrentSchemaVersion,
            Projects = sortedProjects.Select(project => new CatalogProject
            {
                Id = project.Id.Value,
                DisplayName = project.DisplayName,
                Root = project.Root.CanonicalPath,
                RegisteredAtUtc = project.RegisteredAtUtc.ToUniversalTime(),
            }).ToArray(),
            SelectedProjectId = snapshot.SelectedProjectId?.Value,
        };
    }

    private static RegisteredProjectCatalogSnapshot Deserialize(string json)
    {
        CatalogDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<CatalogDocument>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ProjectCatalogException("catalog_corrupt", exception);
        }

        if (document is null)
            throw new ProjectCatalogException("catalog_corrupt");
        if (document.SchemaVersion != CurrentSchemaVersion)
            throw new ProjectCatalogException("catalog_schema_unsupported");
        if (document.Projects is null)
            throw new ProjectCatalogException("catalog_invalid");

        try
        {
            var projects = document.Projects.Select(project => new RegisteredProject(
                new ProjectId(project.Id),
                project.DisplayName,
                new ValidatedProjectRoot(project.Root),
                project.RegisteredAtUtc,
                false)).ToArray();
            ProjectId? selectedId = string.IsNullOrWhiteSpace(document.SelectedProjectId)
                ? null
                : new ProjectId(document.SelectedProjectId!);
            ValidateProjects(projects, selectedId);

            return new RegisteredProjectCatalogSnapshot(
                projects.Select(project => new RegisteredProject(
                    project.Id,
                    project.DisplayName,
                    project.Root,
                    project.RegisteredAtUtc,
                    selectedId.HasValue && project.Id == selectedId.Value)).ToArray(),
                selectedId);
        }
        catch (ProjectCatalogException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentNullException or FormatException)
        {
            throw new ProjectCatalogException("catalog_invalid", exception);
        }
    }

    private static void ValidateProjects(
        IReadOnlyList<RegisteredProject> projects,
        ProjectId? selectedProjectId)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in projects)
        {
            if (!ids.Add(project.Id.Value))
                throw new ProjectCatalogException("duplicate_project_id");

            var root = Path.TrimEndingDirectorySeparator(project.Root.CanonicalPath);
            if (!roots.Add(root))
                throw new ProjectCatalogException("duplicate_project_root");
        }

        if (selectedProjectId.HasValue && !ids.Contains(selectedProjectId.Value.Value))
            throw new ProjectCatalogException("selected_project_missing");
    }

    private sealed class CatalogDocument
    {
        public int SchemaVersion { get; init; }
        public CatalogProject[]? Projects { get; init; }
        public string? SelectedProjectId { get; init; }
    }

    private sealed class CatalogProject
    {
        public string Id { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Root { get; init; } = string.Empty;
        public DateTimeOffset RegisteredAtUtc { get; init; }
    }
}
