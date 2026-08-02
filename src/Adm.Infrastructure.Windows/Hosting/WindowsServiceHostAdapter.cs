using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace Adm.Infrastructure.Windows.Hosting;

public static class WindowsServiceHostAdapter
{
    public const string StartupModeArgument = "--adm-startup-mode";
    public const string ServiceName = "AI Development Manager Server";
    public static readonly TimeSpan DefaultStopTimeout = TimeSpan.FromSeconds(30);

    public static WindowsHostLaunchConfiguration Resolve(string[]? args)
    {
        var value = args?
            .Select(argument => argument.Split('=', 2))
            .Where(parts => parts.Length == 2 && string.Equals(parts[0], StartupModeArgument, StringComparison.OrdinalIgnoreCase))
            .Select(parts => parts[1])
            .LastOrDefault();

        var mode = value?.ToLowerInvariant() switch
        {
            null or "" when OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService() => WindowsHostLaunchMode.Service,
            null or "" or "console" => WindowsHostLaunchMode.Console,
            "manual" => WindowsHostLaunchMode.Manual,
            "service" => WindowsHostLaunchMode.Service,
            "tray" => WindowsHostLaunchMode.Tray,
            _ => throw new ArgumentException("起動モードはconsole、manual、service、trayのいずれかを指定してください。", nameof(args))
        };

        return new WindowsHostLaunchConfiguration(mode);
    }

    public static IHostBuilder Configure(IHostBuilder hostBuilder, WindowsHostLaunchConfiguration configuration)
    {
        if (configuration.Mode != WindowsHostLaunchMode.Service)
        {
            return hostBuilder;
        }

        return hostBuilder
            .UseWindowsService(options => options.ServiceName = ServiceName)
            .ConfigureServices(services => services.Configure<HostOptions>(options =>
            {
                options.ShutdownTimeout = DefaultStopTimeout;
            }));
    }
}
