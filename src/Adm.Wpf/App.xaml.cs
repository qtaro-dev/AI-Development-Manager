using System.Windows;
using Adm.Wpf.Composition;

namespace Adm.Wpf;

public partial class App : System.Windows.Application, IDisposable
{
    private WpfApplicationBootstrapper? bootstrapper;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            bootstrapper = new WpfApplicationBootstrapper();
            MainWindow = bootstrapper.CreateMainWindow();
            MainWindow.Show();
        }
        catch (Exception)
        {
            bootstrapper?.Dispose();
            bootstrapper = null;
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Dispose();
        base.OnExit(e);
    }

    public void Dispose()
    {
        bootstrapper?.Dispose();
        bootstrapper = null;
        GC.SuppressFinalize(this);
    }
}
