using System.Net;
using System.Net.Sockets;
using Adm.Server.Host;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

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
}
