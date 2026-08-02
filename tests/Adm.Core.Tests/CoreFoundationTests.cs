using Adm.Testing;

namespace Adm.Core.Tests;

public sealed class CoreFoundationTests
{
    [Fact]
    public void TestScopeCreatesAndCleansAPrivateDirectory()
    {
        string rootPath;
        string traceId;

        using (var scope = new TestScope())
        {
            rootPath = scope.RootPath;
            traceId = scope.TraceId;

            Assert.True(Directory.Exists(rootPath));
            Assert.False(string.IsNullOrWhiteSpace(traceId));
        }

        Assert.False(Directory.Exists(rootPath));
    }
}
