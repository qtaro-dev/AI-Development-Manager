namespace Adm.Infrastructure.Windows.Tests;

using Adm.Infrastructure.Windows.Hosting;
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
}
