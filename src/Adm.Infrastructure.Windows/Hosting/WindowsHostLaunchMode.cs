namespace Adm.Infrastructure.Windows.Hosting;

public enum WindowsHostLaunchMode
{
    Console,
    Manual,
    Service,
    Tray
}

public sealed record WindowsHostLaunchConfiguration(WindowsHostLaunchMode Mode)
{
    public string StartupMode => Mode.ToString().ToLowerInvariant();
}
