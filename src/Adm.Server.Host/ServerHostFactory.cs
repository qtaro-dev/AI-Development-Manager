using System.Net;
using Adm.Server.Host.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Adm.Server.Host;

public static class ServerHostFactory
{
    public static WebApplication Create(string[]? args = null, int? port = null)
    {
        if (port is < 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, "ポート番号は0から65535の範囲で指定してください。");
        }

        var builder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());
        builder.Services.AddServerConfiguration(builder.Configuration);
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            var configuredPort = context.Configuration.GetValue<int>($"{ServerOptions.SectionName}:{nameof(ServerOptions.Port)}");
            options.Listen(IPAddress.Loopback, port ?? configuredPort);
        });

        var app = builder.Build();
        app.MapGet("/", () => Results.Ok(new
        {
            service = "AI Development Manager Server",
            status = "running"
        }));

        return app;
    }
}
