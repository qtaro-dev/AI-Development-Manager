using System.Text.Json;
using Adm.Wpf.Composition;
using Adm.Wpf.LocalChannel;

namespace Adm.Infrastructure.Windows.Tests;

public sealed class LocalChannelTests
{
    private static readonly string FixtureRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "local-channel");
    private static readonly string LocalSource = "https://app.ai-development-manager.local/index.html";

    [Fact]
    public void ValidRequestFixtureParses()
    {
        var request = LocalChannelProtocol.ParseRequest(ReadFixture("valid-request.json"), LocalSource);

        Assert.Equal("request-001", request.RequestId);
        Assert.Equal("test.echo", request.Operation);
        Assert.Equal("fixture", request.Payload.GetProperty("value").GetString());
    }

    [Fact]
    public void ResponseAndErrorFixturesParseWithTheSameContract()
    {
        var response = LocalChannelProtocol.ParseMessage(ReadFixture("valid-response.json"));
        var error = LocalChannelProtocol.ParseMessage(ReadFixture("valid-error.json"));

        Assert.IsType<LocalChannelResponse>(response);
        var parsedError = Assert.IsType<LocalChannelError>(error);
        Assert.Equal("handler_failed", parsedError.Code);
        Assert.Equal("errors.localChannel.handlerFailed", parsedError.MessageKey);
    }

    [Fact]
    public async Task TestHandlerRoundTripReturnsResponse()
    {
        var registry = LocalChannelOperationRegistry.FromHandlers(new Dictionary<string, LocalChannelHandler>(StringComparer.Ordinal)
        {
            ["test.echo"] = (request, _) => Task.FromResult<object?>(new { value = request.Payload.GetProperty("value").GetString() }),
        });
        var dispatcher = new LocalChannelDispatcher(registry);

        var response = await dispatcher.DispatchAsync(ReadFixture("valid-request.json"), LocalSource);

        Assert.Contains("\"kind\":\"response\"", response);
        Assert.Contains("\"requestId\":\"request-001\"", response);
        Assert.Contains("\"value\":\"fixture\"", response);
    }

    [Fact]
    public async Task UnknownOperationReturnsSafeError()
    {
        var dispatcher = new LocalChannelDispatcher(LocalChannelOperationRegistry.Empty);

        var response = await dispatcher.DispatchAsync(ReadFixture("valid-request.json"), LocalSource);

        Assert.Contains("\"code\":\"operation_not_allowed\"", response);
        Assert.Contains("\"messageKey\":\"errors.localChannel.operationNotAllowed\"", response);
    }

    [Fact]
    public async Task HandlerExceptionDoesNotExposeInternalDetails()
    {
        var registry = LocalChannelOperationRegistry.FromHandlers(new Dictionary<string, LocalChannelHandler>(StringComparer.Ordinal)
        {
            ["test.echo"] = (_, _) => throw new InvalidOperationException("secret path C:\\private\\token.txt"),
        });
        var dispatcher = new LocalChannelDispatcher(registry);

        var response = await dispatcher.DispatchAsync(ReadFixture("valid-request.json"), LocalSource);

        Assert.Contains("\"code\":\"handler_failed\"", response);
        Assert.DoesNotContain("secret path", response);
        Assert.DoesNotContain("token.txt", response);
        Assert.DoesNotContain("InvalidOperationException", response);
    }

    [Fact]
    public void CommonInvalidFixtureRejectsUnknownField()
    {
        var exception = Assert.Throws<LocalChannelProtocolException>(() =>
            LocalChannelProtocol.ParseRequest(ReadFixture("invalid-unknown-field.json"), LocalSource));

        Assert.Equal("invalid_request", exception.Code);
    }

    [Theory]
    [InlineData("https://example.com/index.html")]
    [InlineData("https://app.ai-development-manager.local/other.html")]
    [InlineData("file:///C:/private.txt")]
    public void NonTopLevelLocalOriginIsRejected(string source)
    {
        var exception = Assert.Throws<LocalChannelProtocolException>(() =>
            LocalChannelProtocol.ParseRequest(ReadFixture("valid-request.json"), source));

        Assert.Equal("invalid_request", exception.Code);
    }

    [Fact]
    public void MessageOverOneMiBIsRejected()
    {
        var payload = new string('x', LocalChannelProtocol.MaxMessageBytes);
        var json = "{\"version\":1,\"kind\":\"request\",\"requestId\":\"request-001\",\"operation\":\"test.echo\",\"payload\":{\"value\":\"" + payload + "\"}}";

        var exception = Assert.Throws<LocalChannelProtocolException>(() =>
            LocalChannelProtocol.ParseRequest(json, LocalSource));

        Assert.Equal("message_too_large", exception.Code);
    }

    [Fact]
    public async Task BridgeOperationIsNotPartOfLocalChannelRegistry()
    {
        var dispatcher = new LocalChannelDispatcher(LocalChannelOperationRegistry.Empty);

        var response = await dispatcher.DispatchAsync(
            "{\"version\":1,\"kind\":\"request\",\"requestId\":\"request-002\",\"operation\":\"getHostInfo\",\"payload\":{}}",
            LocalSource);

        Assert.Contains("\"operation_not_allowed\"", response);
    }

    [Fact]
    public async Task LocalCompositionRootReturnsFoundationStatusWithoutServer()
    {
        using var composition = new LocalCompositionRoot();

        var response = await composition.DispatchAsync(
            "{\"version\":1,\"kind\":\"request\",\"requestId\":\"request-local-001\",\"operation\":\"getFoundationStatus\",\"payload\":{}}",
            LocalSource);
        using var document = JsonDocument.Parse(response);

        Assert.Equal("response", document.RootElement.GetProperty("kind").GetString());
        Assert.Equal("ready", document.RootElement.GetProperty("result").GetProperty("state").GetString());
        Assert.Equal("local", document.RootElement.GetProperty("result").GetProperty("apiVersion").GetString());
        Assert.Equal("local", document.RootElement.GetProperty("result").GetProperty("executionMode").GetString());
    }

    [Fact]
    public async Task LocalCompositionRootRejectsRequestsAfterShutdown()
    {
        var composition = new LocalCompositionRoot();
        composition.Dispose();

        var response = await composition.DispatchAsync(
            "{\"version\":1,\"kind\":\"request\",\"requestId\":\"request-local-002\",\"operation\":\"getFoundationStatus\",\"payload\":{}}",
            LocalSource);

        Assert.Contains("\"code\":\"channel_unavailable\"", response);
    }

    [Fact]
    public async Task HostShutdownCancellationReturnsChannelUnavailable()
    {
        using var cancellation = new CancellationTokenSource();
        var dispatcher = new LocalChannelDispatcher(
            LocalChannelOperationRegistry.FromHandlers(new Dictionary<string, LocalChannelHandler>(StringComparer.Ordinal)
            {
                ["test.wait"] = async (_, token) =>
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    return null;
                },
            }));
        cancellation.Cancel();

        var response = await dispatcher.DispatchAsync(
            "{\"version\":1,\"kind\":\"request\",\"requestId\":\"request-shutdown\",\"operation\":\"test.wait\",\"payload\":{}}",
            LocalSource,
            cancellation.Token);

        Assert.Contains("\"code\":\"channel_unavailable\"", response);
        Assert.DoesNotContain("TaskCanceledException", response);
    }

    private static string ReadFixture(string name) => File.ReadAllText(Path.Combine(FixtureRoot, name));
}
