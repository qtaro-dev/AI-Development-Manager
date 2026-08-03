using System.IO;

namespace Adm.Wpf.Shell;

public static class UserDataFolderResolver
{
    public static string GetLocalModeFolder() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AI Development Manager",
        "WebView2",
        "Local");
}
