namespace Adm.Core;

public enum HostingMode
{
    Console,
    WindowsService,
    Manual,
    Tray
}

public static class HostingModeParser
{
    public static HostingMode Parse(string? value) => value?.ToLowerInvariant() switch
    {
        "console" => HostingMode.Console,
        "service" => HostingMode.WindowsService,
        "manual" => HostingMode.Manual,
        "tray" => HostingMode.Tray,
        _ => throw new ArgumentException("Mode must be console, service, manual, or tray.", nameof(value))
    };
}
