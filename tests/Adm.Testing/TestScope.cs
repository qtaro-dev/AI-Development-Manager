namespace Adm.Testing;

public sealed class TestScope : IDisposable
{
    public TestScope()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "adm-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
        TraceId = Guid.NewGuid().ToString("N");
    }

    public string RootPath { get; }

    public string TraceId { get; }

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}
