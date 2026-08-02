using Adm.Core;
using Microsoft.Extensions.Hosting;

namespace Adm.Infrastructure.Windows;

public static class WindowsServiceAdapter
{
    public static void Configure(IHostApplicationBuilder builder)
    {
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "AI Development Manager PoC Server";
        });
    }

    public static string Describe(HostingMode mode) =>
        mode == HostingMode.WindowsService
            ? "Windows Service adapter configured"
            : "Windows Service adapter not selected";
}
