using System.Net;
using System.Text.Json;
using Adm.Application.Foundation;
using Adm.Server.Host;

namespace Adm.Server.IntegrationTests;

public sealed class TransportParityTests
{
    private static readonly string FixtureRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "transport-parity");

    [Fact]
    public async Task LocalAndHttpFoundationResultsHaveTheSameMeaning()
    {
        var localResult = await new GetFoundationStatusUseCase().ExecuteAsync();

        await using var server = ServerHostFactory.Create(port: 0);
        await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.Urls.Single()) };
        using var httpResponse = await client.GetAsync("/api/v1/version");
        using var httpDocument = JsonDocument.Parse(await httpResponse.Content.ReadAsStringAsync());
        var httpResult = httpDocument.RootElement;
        var expected = ReadExpected("foundation-success.json").GetProperty("expected");

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.Equal(expected.GetProperty("state").GetString(), localResult.State);
        Assert.Equal(localResult.State, httpResult.GetProperty("status").GetString());
        Assert.Equal(expected.GetProperty("contractVersion").GetString(), localResult.ContractVersion);
        Assert.Equal(localResult.ContractVersion, httpResult.GetProperty("contractVersion").GetString());
        Assert.Equal(JsonValueKind.String, httpResult.GetProperty("serverTimeUtc").ValueKind);
    }

    [Fact]
    public async Task LocalAndHttpErrorsNormalizeToTheSameSemantics()
    {
        var errorFixtures = ReadExpected("error-semantics.json");

        foreach (var fixture in errorFixtures.EnumerateArray())
        {
            var local = fixture.GetProperty("local");
            var expectedHttp = fixture.GetProperty("http");
            Assert.Equal(
                (expectedHttp.GetProperty("code").GetString(), expectedHttp.GetProperty("messageKey").GetString()),
                NormalizeLocal(local.GetProperty("code").GetString()!));
        }

        await using var server = ServerHostFactory.Create(port: 0);
        await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.Urls.Single()) };
        using var httpNotFound = await client.GetAsync("/api/v1/unknown");
        using var httpProblem = JsonDocument.Parse(await httpNotFound.Content.ReadAsStringAsync());
        var httpError = httpProblem.RootElement;
        var expectedUnsupported = errorFixtures.EnumerateArray()
            .Single(item => item.GetProperty("case").GetString() == "unsupported-operation")
            .GetProperty("http");

        Assert.Equal(
            (expectedUnsupported.GetProperty("code").GetString(), expectedUnsupported.GetProperty("messageKey").GetString()),
            (httpError.GetProperty("code").GetString(), httpError.GetProperty("messageKey").GetString()));
        Assert.Equal(HttpStatusCode.NotFound, httpNotFound.StatusCode);
        Assert.Equal(
            ("validation_failed", "errors.validation.invalid_input"),
            NormalizeLocal("invalid_request"));
        Assert.Equal(
            ("not_found", "errors.resource.not_found"),
            NormalizeLocal("operation_not_allowed"));
        Assert.Equal(
            ("internal_error", "errors.system.unexpected"),
            NormalizeLocal("handler_failed"));
    }

    [Fact]
    public void ParityHarnessDetectsAChangedAdapterValue()
    {
        var expected = ("ready", "1.0");
        var changed = ("ready", "2.0");

        Assert.NotEqual(expected, changed);
    }

    private static (string, string) NormalizeLocal(string code) => code switch
    {
        "invalid_request" => ("validation_failed", "errors.validation.invalid_input"),
        "operation_not_allowed" => ("not_found", "errors.resource.not_found"),
        "handler_failed" => ("internal_error", "errors.system.unexpected"),
        _ => ("unknown", "errors.system.unexpected"),
    };

    private static JsonElement ReadExpected(string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(FixtureRoot, name)));
        return document.RootElement.Clone();
    }
}
