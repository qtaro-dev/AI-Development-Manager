using System.Net;
using System.Net.Sockets;
using Adm.Server.Host;
using Adm.Server.Host.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Adm.Server.IntegrationTests;

public sealed class TestServerFoundationTests
{
    [Fact]
    public async Task TestServerStartsAndStopsWithoutBindingARealPort()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.MapGet("/", () => Results.NoContent());

        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task HostFactoryServesLocalhostOnlyAndStopsCleanly()
    {
        await using var app = ServerHostFactory.Create(port: 0);

        await app.StartAsync();
        var address = app.Urls.Single();
        var uri = new Uri(address);

        Assert.True(uri.Host is "127.0.0.1" or "localhost");
        Assert.DoesNotContain("0.0.0.0", address, StringComparison.Ordinal);

        using var client = new HttpClient { BaseAddress = uri };
        using var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AI Development Manager Server", content, StringComparison.Ordinal);

        await app.StopAsync();
    }

    [Fact]
    public async Task StartingTwoHostsOnTheSamePortFailsExplicitly()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        await using var first = ServerHostFactory.Create(port: port);
        await first.StartAsync();

        await using var second = ServerHostFactory.Create(port: port);
        await Assert.ThrowsAsync<IOException>(() => second.StartAsync());

        await first.StopAsync();
    }

    [Fact]
    public void CommandLineConfigurationOverridesEnvironmentConfiguration()
    {
        const string environmentKey = "Server__Port";
        var originalValue = Environment.GetEnvironmentVariable(environmentKey);

        try
        {
            Environment.SetEnvironmentVariable(environmentKey, "41001");
            using var app = ServerHostFactory.Create(["--Server:Port=41002"]);

            var options = app.Services.GetRequiredService<IOptions<ServerOptions>>().Value;

            Assert.Equal(41002, options.Port);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentKey, originalValue);
        }
    }

    [Fact]
    public async Task InvalidBindAddressPreventsStartup()
    {
        await using var app = ServerHostFactory.Create(["--Server:BindAddress=0.0.0.0"]);

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => app.StartAsync());

        Assert.Contains("127.0.0.1", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InlineSecretIsRejectedWithoutEchoingTheSecret()
    {
        const string secret = "do-not-print-this";
        await using var app = ServerHostFactory.Create([$"--Secrets:ApiToken={secret}"]);

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => app.StartAsync());

        Assert.DoesNotContain(secret, exception.Message, StringComparison.Ordinal);
        Assert.Contains("Secrets", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigurationCatalogDescribesOnlySupportedConfiguration()
    {
        var portEntry = ConfigurationCatalog.Entries.Single(entry => entry.Key == "Server:Port");
        var secretEntry = ConfigurationCatalog.Entries.Single(entry => entry.Key == "Secrets:ApiTokenReference");

        Assert.Equal("0", portEntry.DefaultValue);
        Assert.True(portEntry.UserChangeable);
        Assert.True(portEntry.RequiresRestart);
        Assert.True(secretEntry.IsSecretReference);
        Assert.DoesNotContain(ConfigurationCatalog.Entries, entry => entry.Key.Contains("ApiToken", StringComparison.Ordinal) && !entry.IsSecretReference);
    }
}
