namespace Adm.Wpf.Shell;

public static class LocalWebViewPolicy
{
    public const string VirtualHostName = "app.ai-development-manager.local";
    public static readonly Uri Origin = new($"https://{VirtualHostName}/");
    public static readonly Uri StartUri = new(Origin, "index.html");

    public static bool IsAllowedNavigation(Uri candidate) => IsAllowedTopLevelDocument(candidate);

    public static bool IsAllowedResource(Uri candidate) => IsFixedOrigin(candidate);

    public static bool IsAllowedTopLevelDocument(Uri candidate) =>
        IsFixedOrigin(candidate) &&
        string.Equals(candidate.AbsolutePath, StartUri.AbsolutePath, StringComparison.Ordinal) &&
        (string.IsNullOrEmpty(candidate.Query) || string.Equals(candidate.Query, "?settings=1", StringComparison.Ordinal)) &&
        string.IsNullOrEmpty(candidate.Fragment);

    private static bool IsFixedOrigin(Uri candidate) =>
        candidate.Scheme.Equals(Origin.Scheme, StringComparison.OrdinalIgnoreCase) &&
        candidate.Host.Equals(Origin.Host, StringComparison.OrdinalIgnoreCase) &&
        candidate.Port == Origin.Port;
}
