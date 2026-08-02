namespace Adm.Application.Tests;

public sealed class ApplicationFoundationTests
{
    [Fact]
    public void TestProjectIsLoadableFromTheFixedToolchain()
    {
        Assert.NotNull(typeof(Adm.Application.Class1).Assembly);
    }
}
