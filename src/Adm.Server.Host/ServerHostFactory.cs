using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Adm.Infrastructure.Windows.Hosting;
using Adm.Application.Foundation;
using Adm.Server.Host.Api;
using Adm.Server.Host.Configuration;
using Adm.Server.Host.Errors;
using Adm.Server.Host.Health;
using Adm.Server.Host.Logging;
using Adm.Server.Host.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Adm.Server.Host;

public static class ServerHostFactory
{
    public static WebApplication Create(
        string[]? args = null,
        int? port = null,
        string startupMode = "console",
        Action<IHostBuilder>? configureHost = null,
        string? webAssetsRoot = null)
    {
        if (port is < 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, "ポート番号は0から65535の範囲で指定してください。");
        }

        var builder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());
        configureHost?.Invoke(builder.Host);
        builder.Services.AddServerConfiguration(builder.Configuration);
        builder.Services.AddSingleton<IGetFoundationStatusUseCase>(_ => new GetFoundationStatusUseCase("v1", "server"));
        builder.Services.AddAdmHealth();
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
        var resolvedWebAssetsRoot = webAssetsRoot ?? WebAssetHosting.GetDefaultWebRoot();
        WebAssetHosting.UseWebAssets(app, resolvedWebAssetsRoot);
        app.UseAdmErrorHandling();
        app.UseAdmRequestTracing();
        app.MapOpenApi("/openapi/{documentName}.json");
        app.MapAdmApiV1();
        var buildVersion = typeof(ServerHostFactory).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "unknown";
        app.MapAdmHealthEndpoints(startupMode, buildVersion);
        var lifecycleLogger = app.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Adm.Server.Host.Lifecycle");
        app.Lifetime.ApplicationStarted.Register(() => lifecycleLogger.ServerStarted(
            startupMode,
            app.Environment.EnvironmentName,
            buildVersion));
        app.Lifetime.ApplicationStopping.Register(() => lifecycleLogger.ServerStopping(
            startupMode,
            WindowsServiceHostAdapter.DefaultStopTimeout));
        WebAssetHosting.MapSpaFallback(app, resolvedWebAssetsRoot);

        return app;
    }
}
