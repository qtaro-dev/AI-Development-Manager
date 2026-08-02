using System.Net;
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
}
