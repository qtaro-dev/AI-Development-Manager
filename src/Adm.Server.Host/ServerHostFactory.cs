using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Adm.Server.Host.Api;
using Adm.Server.Host.Configuration;
using Adm.Server.Host.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Adm.Server.Host;

public static class ServerHostFactory
{
    public static WebApplication Create(string[]? args = null, int? port = null, string startupMode = "console")
    {
        if (port is < 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, "ポート番号は0から65535の範囲で指定してください。");
        }

        var builder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());
        builder.Services.AddServerConfiguration(builder.Configuration);
        builder.Services.AddOpenApi("v1", options => options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0);
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddProvider(new AdmJsonLoggerProvider());
        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            var configuredPort = context.Configuration.GetValue<int>($"{ServerOptions.SectionName}:{nameof(ServerOptions.Port)}");
            options.Listen(IPAddress.Loopback, port ?? configuredPort);
        });

        var app = builder.Build();
        app.UseAdmRequestTracing();
        app.MapOpenApi("/openapi/{documentName}.json");
        app.MapAdmApiV1();
        app.MapGet("/", () => Results.Ok(new
        {
            service = "AI Development Manager Server",
            status = "running"
        })).ExcludeFromDescription();

        var buildVersion = typeof(ServerHostFactory).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "unknown";
        var lifecycleLogger = app.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Adm.Server.Host.Lifecycle");
        app.Lifetime.ApplicationStarted.Register(() => lifecycleLogger.ServerStarted(
            startupMode,
            app.Environment.EnvironmentName,
            buildVersion));

        return app;
    }
}
