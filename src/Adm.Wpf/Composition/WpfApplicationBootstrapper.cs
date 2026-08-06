using Adm.Infrastructure.Windows.Projects;
using Adm.Application.ExecutionProfiles;
using Adm.Wpf.Configuration;

namespace Adm.Wpf.Composition;

public sealed class WpfApplicationBootstrapper : IDisposable
{
    private readonly ExecutionProfileService executionProfiles;
    private readonly LocalCompositionRoot localCompositionRoot;
    private bool disposed;

    public WpfApplicationBootstrapper()
    {
        var commandLineArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
        var allowLoopbackHttp = commandLineArgs.Contains(
            "--allow-loopback-http",
            StringComparer.OrdinalIgnoreCase);

        ProjectCatalog = new WindowsRegisteredProjectCatalog();
        ProjectRootValidator = new WindowsProjectRootValidator();
        executionProfiles = new ExecutionProfileService(
            new JsonExecutionProfileStore(),
            allowLoopbackHttp);
        localCompositionRoot = new LocalCompositionRoot(executionProfiles);
    }

    internal WindowsRegisteredProjectCatalog ProjectCatalog { get; }

    internal WindowsProjectRootValidator ProjectRootValidator { get; }

    public MainWindow CreateMainWindow()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return new MainWindow(executionProfiles, localCompositionRoot);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        localCompositionRoot.Dispose();
        GC.SuppressFinalize(this);
    }
}
