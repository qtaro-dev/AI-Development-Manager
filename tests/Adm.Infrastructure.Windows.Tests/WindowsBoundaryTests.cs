namespace Adm.Infrastructure.Windows.Tests;

public sealed class WindowsBoundaryTests
{
    [Fact]
    [Trait("Category", "Windows")]
    public void WindowsTestRequiresAWindowsRuntime()
    {
        Assert.True(OperatingSystem.IsWindows(), "This test requires a Windows runtime.");
    }
}
