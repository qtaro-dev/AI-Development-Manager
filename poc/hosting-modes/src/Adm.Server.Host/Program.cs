using Adm.Core;
using Adm.Infrastructure.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

var mode = ParseMode(args);
var probe = args.Any(argument => string.Equals(argument, "--probe", StringComparison.OrdinalIgnoreCase));
var builder = CreateServerHost(mode);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    mode = mode.ToString(),
    host = app.GetType().FullName
}));

if (probe)
{
    Console.WriteLine($"mode={mode}");
    Console.WriteLine($"host={app.GetType().FullName}");
    Console.WriteLine("endpoint=/health");
    Console.WriteLine(WindowsServiceAdapter.Describe(mode));
    return;
}

await app.RunAsync();

static WebApplicationBuilder CreateServerHost(HostingMode mode)
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls("http://127.0.0.1:5099");
    builder.Services.AddSingleton(typeof(HostingMode), mode);

    if (mode == HostingMode.WindowsService)
    {
        WindowsServiceAdapter.Configure(builder);
    }

    return builder;
}

static HostingMode ParseMode(string[] args)
{
    var modeIndex = Array.FindIndex(args, argument => string.Equals(argument, "--mode", StringComparison.OrdinalIgnoreCase));
    var value = modeIndex >= 0 && modeIndex + 1 < args.Length ? args[modeIndex + 1] : "console";
    return HostingModeParser.Parse(value);
}
