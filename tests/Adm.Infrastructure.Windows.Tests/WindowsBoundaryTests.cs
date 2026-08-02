namespace Adm.Infrastructure.Windows.Tests;

using Adm.Infrastructure.Windows.Hosting;
using Adm.Wpf.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public sealed class WindowsBoundaryTests
{
    [Fact]
    [Trait("Category", "Windows")]
    public void WindowsTestRequiresAWindowsRuntime()
    {
        Assert.True(OperatingSystem.IsWindows(), "This test requires a Windows runtime.");
    }

    [Theory]
    [InlineData("console", WindowsHostLaunchMode.Console)]
    [InlineData("manual", WindowsHostLaunchMode.Manual)]
    [InlineData("service", WindowsHostLaunchMode.Service)]
    [InlineData("tray", WindowsHostLaunchMode.Tray)]
    public void LaunchModeIsResolvedWithoutDuplicatingHostLogic(string value, WindowsHostLaunchMode expected)
    {
        var configuration = WindowsServiceHostAdapter.Resolve([$"{WindowsServiceHostAdapter.StartupModeArgument}={value}"]);

        Assert.Equal(expected, configuration.Mode);
        Assert.Equal(value, configuration.StartupMode);
    }

    [Fact]
    public void ServiceAdapterConfiguresAReusableHostBuilder()
    {
        var builder = Host.CreateDefaultBuilder();
        var configuration = new WindowsHostLaunchConfiguration(WindowsHostLaunchMode.Service);

        var configured = WindowsServiceHostAdapter.Configure(builder, configuration);

        Assert.Same(builder, configured);
        using var host = configured.Build();
        Assert.NotNull(host.Services.GetService<IHostLifetime>());
    }

    [Theory]
    [InlineData("http://127.0.0.1:5181/", "http://127.0.0.1:5181/projects/demo", true)]
    [InlineData("http://localhost:5181/", "http://localhost:5181/health/ready", true)]
    [InlineData("http://127.0.0.1:5181/", "http://localhost:5181/projects/demo", false)]
    [InlineData("http://127.0.0.1:5181/", "https://127.0.0.1:5181/projects/demo", false)]
    [InlineData("http://127.0.0.1:5181/", "http://127.0.0.1:5182/projects/demo", false)]
    [InlineData("http://127.0.0.1:5181/", "https://example.com/", false)]
    public void NavigationPolicyKeepsWebViewInsideTheConfiguredServerOrigin(string server, string candidate, bool expected)
    {
        Assert.Equal(expected, ShellNavigationPolicy.IsAllowed(new Uri(server), new Uri(candidate)));
    }

    [Fact]
    public void ServerConnectionOptionsRejectsNonLocalServerUrls()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ServerConnectionOptions.FromArguments(["--server-url=https://example.com/"]));

        Assert.Contains("localhost", exception.Message, StringComparison.Ordinal);
    }
}
