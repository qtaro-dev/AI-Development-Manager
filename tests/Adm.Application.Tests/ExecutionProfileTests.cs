using Adm.Application.ExecutionProfiles;

namespace Adm.Application.Tests;

public sealed class ExecutionProfileTests
{
    [Fact]
    public async Task MissingProfileDefaultsToLocal()
    {
        var service = new ExecutionProfileService(new MemoryStore());

        var result = await service.GetAsync();

        Assert.Equal(ExecutionProfileMode.Local, result.Profile.Mode);
        Assert.False(result.UsedLocalFallback);
        Assert.Null(result.WarningCode);
    }

    [Fact]
    public async Task ValidHttpsServerProfileIsNormalizedAndReadBack()
    {
        var store = new MemoryStore();
        var service = new ExecutionProfileService(store);

        var saved = await service.UpdateAsync(new ExecutionProfileUpdate(ExecutionProfileMode.Server, "https://server.example.test:8443"));
        var result = await service.GetAsync();

        Assert.Equal("https://server.example.test:8443/", saved.ServerUri);
        Assert.Equal(saved, result.Profile);
        Assert.Contains("\"mode\":\"server\"", store.Json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidContentFallsBackToLocal()
    {
        var store = new MemoryStore("{\"schemaVersion\":99,\"mode\":\"server\",\"serverUri\":\"http://server\"}");
        var result = await new ExecutionProfileService(store).GetAsync();

        Assert.Equal(ExecutionProfileMode.Local, result.Profile.Mode);
        Assert.True(result.UsedLocalFallback);
        Assert.Equal("profile_recovered_local", result.WarningCode);
    }

    [Fact]
    public async Task LanHttpRequiresExplicitLoopbackOverride()
    {
        var service = new ExecutionProfileService(new MemoryStore());

        await Assert.ThrowsAsync<ExecutionProfileValidationException>(() =>
            service.UpdateAsync(new ExecutionProfileUpdate(ExecutionProfileMode.Server, "http://server.example.test:8080")));

        var loopback = new ExecutionProfileService(new MemoryStore(), allowLoopbackHttp: true);
        var saved = await loopback.UpdateAsync(new ExecutionProfileUpdate(ExecutionProfileMode.Server, "http://127.0.0.1:5181"));
        Assert.Equal("http://127.0.0.1:5181/", saved.ServerUri);
    }

    [Fact]
    public async Task UnknownFieldsAndSecretsAreNotAcceptedOrPersisted()
    {
        var service = new ExecutionProfileService(new MemoryStore());

        await Assert.ThrowsAsync<ExecutionProfileValidationException>(() =>
            Task.FromResult(service.ParseAndValidate("{\"schemaVersion\":1,\"mode\":\"local\",\"serverUri\":null,\"token\":\"secret\"}")));

        var store = new MemoryStore();
        await new ExecutionProfileService(store).UpdateAsync(new ExecutionProfileUpdate(ExecutionProfileMode.Local, null));
        Assert.DoesNotContain("token", store.Json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", store.Json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("privateKey", store.Json, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class MemoryStore(string? json = null) : IExecutionProfileStore
    {
        public string? Json { get; private set; } = json;
        public Task<string?> ReadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Json);
        public Task WriteAsync(string json, CancellationToken cancellationToken = default)
        {
            Json = json;
            return Task.CompletedTask;
        }
    }
}
