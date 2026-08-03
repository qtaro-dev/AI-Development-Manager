using System.IO;

namespace Adm.Wpf.Configuration;

public static class ExecutionProfilePathResolver
{
    public static string GetPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AI Development Manager",
        "Config",
        "execution-profile.json");
}
