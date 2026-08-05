using Adm.Wpf.Shell;

namespace Adm.Infrastructure.Windows.Tests;

public sealed class WpfShellTests
{
    [Fact]
    public void NewAttemptCancelsPreviousAttemptAndTracksLatestGeneration()
    {
        using var lifecycle = new WindowLifecycleCoordinator();
        using var first = lifecycle.BeginAttempt();
        using var second = lifecycle.BeginAttempt();

        Assert.True(first.Token.IsCancellationRequested);
        Assert.False(lifecycle.IsCurrent(first));
        Assert.True(lifecycle.IsCurrent(second));
        Assert.True(second.Generation > first.Generation);
    }

    [Fact]
    public void DisposingLifecycleCancelsCurrentAttemptAndIsIdempotent()
    {
        using var lifecycle = new WindowLifecycleCoordinator();
        using var attempt = lifecycle.BeginAttempt();

        lifecycle.Dispose();
        lifecycle.Dispose();

        Assert.True(lifecycle.LifetimeToken.IsCancellationRequested);
        Assert.True(attempt.Token.IsCancellationRequested);
        Assert.False(lifecycle.IsCurrent(attempt));
    }

    [Fact]
    public void NoArgumentsSelectLocalMode()
    {
        var options = ServerConnectionOptions.FromArguments([]);

        Assert.True(options.IsLocal);
        Assert.Null(options.ServerUri);
    }

    [Fact]
    public void ServerArgumentSelectsServerMode()
    {
        var options = ServerConnectionOptions.FromArguments(["--server-url=http://127.0.0.1:5181"]);

        Assert.False(options.IsLocal);
        Assert.Equal(new Uri("http://127.0.0.1:5181/"), options.ServerUri);
    }

    [Fact]
    public void ServerArgumentRejectsExternalHost()
    {
        Assert.Throws<ArgumentException>(() =>
            ServerConnectionOptions.FromArguments(["--server-url=https://example.com"]));
    }

    [Fact]
    public void LocalOriginAllowsOnlyTheFixedVirtualOrigin()
    {
        Assert.True(LocalWebViewPolicy.IsAllowedNavigation(new Uri("https://app.ai-development-manager.local/index.html")));
        Assert.True(LocalWebViewPolicy.IsAllowedNavigation(new Uri("https://app.ai-development-manager.local/index.html?settings=1")));
        Assert.False(LocalWebViewPolicy.IsAllowedNavigation(new Uri("https://example.com/")));
        Assert.False(LocalWebViewPolicy.IsAllowedNavigation(new Uri("https://app.ai-development-manager.local/index.html?settings=2")));
        Assert.False(LocalWebViewPolicy.IsAllowedNavigation(new Uri("file:///C:/secret.txt")));
        Assert.False(LocalWebViewPolicy.IsAllowedResource(new Uri("http://127.0.0.1:5181/")));
    }

    [Fact]
    public void AssetResolverRequiresIndexAndSupportsSpacesInPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "AI Development Manager P1-032", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "WebAssets"));
        try
        {
            Assert.False(WebAssetResolver.TryResolve(root, out _));
            File.WriteAllText(Path.Combine(root, "WebAssets", "index.html"), "<!doctype html>");

            Assert.True(WebAssetResolver.TryResolve(root, out var resolution));
            Assert.NotNull(resolution);
            Assert.Equal(Path.Combine(root, "WebAssets"), resolution!.RootDirectory);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LocalUserDataFolderIsDedicatedToTheLocalProfile()
    {
        var folder = UserDataFolderResolver.GetLocalModeFolder();

        Assert.EndsWith(Path.Combine("AI Development Manager", "WebView2", "Local"), folder, StringComparison.OrdinalIgnoreCase);
    }
}
