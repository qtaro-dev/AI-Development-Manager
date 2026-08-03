namespace Adm.Application.Tests;

public sealed class ApplicationFoundationTests
{
    [Fact]
    public void TestProjectIsLoadableFromTheFixedToolchain()
    {
        Assert.NotNull(typeof(Adm.Application.Class1).Assembly);
    }

    [Fact]
    public async Task FoundationStatusUseCaseReturnsLocalProductStatus()
    {
        var result = await new Adm.Application.Foundation.GetFoundationStatusUseCase()
            .ExecuteAsync();

        Assert.Equal("ready", result.State);
        Assert.Equal("local", result.ApiVersion);
        Assert.Equal("1.0", result.ContractVersion);
        Assert.Equal("AI Development Manager", result.ProductName);
        Assert.Equal("local", result.ExecutionMode);
    }

    [Fact]
    public async Task FoundationStatusUseCaseHonorsShutdownCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new Adm.Application.Foundation.GetFoundationStatusUseCase()
                .ExecuteAsync(cancellation.Token));
    }
}
