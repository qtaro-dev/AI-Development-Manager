using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Adm.Application.Errors;
using Adm.Server.Host;
using Adm.Server.Host.Configuration;
using Adm.Server.Host.Errors;
using Adm.Server.Host.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        client.DefaultRequestHeaders.Add(TraceId.HeaderName, "client-trace-123");
        using var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AI Development Manager Server", content, StringComparison.Ordinal);
        Assert.Equal("client-trace-123", response.Headers.GetValues(TraceId.HeaderName).Single());

        await app.StopAsync();
    }

    [Fact]
    public async Task ApiV1VersionUsesThePublishedJsonContract()
    {
        await using var app = ServerHostFactory.Create(port: 0);
        await app.StartAsync();
        var uri = new Uri(app.Urls.Single());
        using var client = new HttpClient { BaseAddress = uri };

        using var response = await client.GetAsync("/api/v1/version");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var json = document.RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("v1", json.GetProperty("apiVersion").GetString());
        Assert.Equal("1.0", json.GetProperty("contractVersion").GetString());
        Assert.Equal("ready", json.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.String, json.GetProperty("serverTimeUtc").ValueKind);
        Assert.DoesNotContain("resourceId", json.EnumerateObject().Select(property => property.Name));
        Assert.Equal(TimeSpan.Zero, DateTimeOffset.Parse(json.GetProperty("serverTimeUtc").GetString()!, CultureInfo.InvariantCulture).Offset);
        Assert.Equal("/api/v1/version", response.RequestMessage?.RequestUri?.AbsolutePath);

        await app.StopAsync();
    }

    [Fact]
    public async Task OpenApiDocumentDescribesOnlyTheVersionFoundationEndpoint()
    {
        await using var app = ServerHostFactory.Create(port: 0);
        await app.StartAsync();
        var uri = new Uri(app.Urls.Single());
        using var client = new HttpClient { BaseAddress = uri };

        using var response = await client.GetAsync("/openapi/v1.json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var paths = root.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("3.", root.GetProperty("openapi").GetString(), StringComparison.Ordinal);
        Assert.True(paths.TryGetProperty("/api/v1/version", out var versionPath));
        Assert.True(versionPath.TryGetProperty("get", out _));
        Assert.Single(paths.EnumerateObject());

        await app.StopAsync();
    }

    [Fact]
    public async Task ApiRouteIsSeparatedFromTheRootRoute()
    {
        await using var app = ServerHostFactory.Create(port: 0);
        await app.StartAsync();
        var uri = new Uri(app.Urls.Single());
        using var client = new HttpClient { BaseAddress = uri };

        using var rootResponse = await client.GetAsync("/");
        using var unknownApiResponse = await client.GetAsync("/api/v1/unknown");
        using var problemDocument = JsonDocument.Parse(await unknownApiResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, rootResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownApiResponse.StatusCode);
        Assert.Equal("application/problem+json", unknownApiResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal("not_found", problemDocument.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            unknownApiResponse.Headers.GetValues(TraceId.HeaderName).Single(),
            problemDocument.RootElement.GetProperty("traceId").GetString());

        await app.StopAsync();
    }

    [Fact]
    public async Task ExceptionsBecomeSafeProblemDetailsWithTraceCorrelation()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        var app = builder.Build();
        app.UseAdmErrorHandling();
        app.UseAdmRequestTracing();
        app.MapGet("/test/validation", () => ThrowValidation());
        app.MapGet("/test/unexpected", () => ThrowUnexpected());

        await app.StartAsync();
        using var client = app.GetTestClient();
        using var validationResponse = await client.GetAsync("/test/validation");
        using var validationDocument = JsonDocument.Parse(await validationResponse.Content.ReadAsStringAsync());
        using var unexpectedResponse = await client.GetAsync("/test/unexpected");
        using var unexpectedDocument = JsonDocument.Parse(await unexpectedResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.BadRequest, validationResponse.StatusCode);
        Assert.Equal("application/problem+json", validationResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal("validation_failed", validationDocument.RootElement.GetProperty("code").GetString());
        Assert.True(validationDocument.RootElement.GetProperty("inputRetained").GetBoolean());
        Assert.False(validationDocument.RootElement.GetProperty("retryable").GetBoolean());
        Assert.Equal(
            validationResponse.Headers.GetValues(TraceId.HeaderName).Single(),
            validationDocument.RootElement.GetProperty("traceId").GetString());

        Assert.Equal(HttpStatusCode.InternalServerError, unexpectedResponse.StatusCode);
        Assert.Equal("internal_error", unexpectedDocument.RootElement.GetProperty("code").GetString());
        Assert.DoesNotContain("secret-from-exception", await unexpectedResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", await unexpectedResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        await app.StopAsync();
    }

    private static void ThrowValidation() => throw new AdmValidationException();

    private static void ThrowUnexpected() => throw new InvalidOperationException("secret-from-exception");

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

    [Fact]
    public void TraceIdRejectsUntrustedInputAndGeneratesSafeValue()
    {
        Assert.Equal("client-trace-123", TraceId.GetOrCreate("client-trace-123"));

        var generated = TraceId.GetOrCreate("contains spaces");

        Assert.StartsWith("adm-", generated, StringComparison.Ordinal);
        Assert.DoesNotContain(" ", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonLoggerProducesParseableOutputWithoutSensitiveValues()
    {
        const string secret = "secret-token-value";
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        using var provider = new AdmJsonLoggerProvider(writer);
        var logger = provider.CreateLogger("Adm.Server.IntegrationTests");

        using (logger.BeginScope(new Dictionary<string, object?> { ["trace_id"] = "adm-trace-001" }))
        {
            var state = new[]
            {
                new KeyValuePair<string, object?>("Authorization", $"Bearer {secret}"),
                new KeyValuePair<string, object?>("Operation", "test")
            };
            logger.Log(
                LogLevel.Information,
                new EventId(1000),
                state,
                null,
                (values, _) => $"received Bearer {secret}");
        }

        using var document = JsonDocument.Parse(writer.ToString());
        var json = document.RootElement;
        var properties = json.GetProperty("properties");

        Assert.Equal("Information", json.GetProperty("level").GetString());
        Assert.Equal("adm-trace-001", properties.GetProperty("trace_id").GetString());
        Assert.Equal("[REDACTED]", properties.GetProperty("Authorization").GetString());
        Assert.DoesNotContain(secret, writer.ToString(), StringComparison.Ordinal);
        Assert.Equal("received Bearer [REDACTED]", json.GetProperty("message").GetString());
        Assert.Equal("?token=[REDACTED]", LogRedaction.RedactText($"?token={secret}"));
    }
}
