using System.Text.Json;
using Adm.Application.ExecutionProfiles;
using Adm.Wpf.Configuration;

namespace Adm.Infrastructure.Windows.Tests;

public sealed class ExecutionProfileTests
{
    [Fact]
    public async Task FileStoreWritesValidJsonAndLeavesNoTemporaryFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "adm-profile-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "execution-profile.json");
        try
        {
            var store = new JsonExecutionProfileStore(path);
            await store.WriteAsync("{\"schemaVersion\":1,\"mode\":\"local\",\"serverUri\":null}");

            Assert.Equal("{\"schemaVersion\":1,\"mode\":\"local\",\"serverUri\":null}", await store.ReadAsync());
            Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LocalChannelSupportsProfileRoundTripAndRejectsLanHttp()
    {
        var service = new ExecutionProfileService(new InMemoryStore());
        using var root = new Adm.Wpf.Composition.LocalCompositionRoot(service);
        const string source = "https://app.ai-development-manager.local/index.html";

        var update = await root.DispatchAsync("{\"version\":1,\"kind\":\"request\",\"requestId\":\"p1\",\"operation\":\"executionProfile.update\",\"payload\":{\"mode\":\"server\",\"serverUri\":\"https://server.example.test\"}}", source);
        Assert.True(update.Contains("\"mode\":\"server\"", StringComparison.Ordinal), update);
        using var response = JsonDocument.Parse(update);
        Assert.Equal("response", response.RootElement.GetProperty("kind").GetString());
        Assert.Equal("p1", response.RootElement.GetProperty("requestId").GetString());

        var rejected = await root.DispatchAsync("{\"version\":1,\"kind\":\"request\",\"requestId\":\"p2\",\"operation\":\"executionProfile.update\",\"payload\":{\"mode\":\"server\",\"serverUri\":\"http://server.example.test\"}}", source);
        Assert.Contains("\"code\":\"invalid_request\"", rejected, StringComparison.Ordinal);
    }

    private sealed class InMemoryStore : IExecutionProfileStore
    {
        private string? json;
        public Task<string?> ReadAsync(CancellationToken cancellationToken = default) => Task.FromResult(json);
        public Task WriteAsync(string value, CancellationToken cancellationToken = default) { json = value; return Task.CompletedTask; }
    }
}
