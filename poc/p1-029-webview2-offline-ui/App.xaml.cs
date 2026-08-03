using System.Windows;
using System.IO;
using System.Text;

namespace Adm.Poc.P1029;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var options = PocOptions.Parse(e.Args);
            DispatcherUnhandledException += (_, args) => WriteCrash(options, args.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                WriteCrash(options, args.ExceptionObject as Exception ?? new InvalidOperationException(args.ExceptionObject?.ToString()));
            var window = new MainWindow(options);
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            var evidence = e.Args
                .Select(argument => argument.Split('=', 2))
                .FirstOrDefault(parts => parts.Length == 2 && parts[0].Equals("--evidence", StringComparison.OrdinalIgnoreCase))?
                .ElementAtOrDefault(1);
            if (!string.IsNullOrWhiteSpace(evidence))
            {
                Directory.CreateDirectory(evidence);
                WriteCrash(new PocOptions(string.Empty, evidence, null, 0), exception);
            }

            Shutdown(1);
        }
    }

    private static void WriteCrash(PocOptions options, Exception exception)
    {
        try
        {
            Directory.CreateDirectory(options.EvidencePath);
            File.WriteAllText(
                Path.Combine(options.EvidencePath, "crash.txt"),
                exception.ToString(),
                Encoding.UTF8);
        }
        catch
        {
            // Preserve the original process failure if evidence cannot be written.
        }
    }
}
