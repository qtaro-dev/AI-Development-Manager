namespace Adm.Wpf.Shell;

public static class ShellNavigationPolicy
{
    public static bool IsAllowed(Uri serverUri, Uri candidateUri) =>
        IsSupportedServerUri(serverUri) &&
        IsSupportedServerUri(candidateUri) &&
        string.Equals(serverUri.Scheme, candidateUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(serverUri.Host, candidateUri.Host, StringComparison.OrdinalIgnoreCase) &&
        serverUri.Port == candidateUri.Port;

    public static bool IsLocalHttpUri(Uri uri) =>
        (uri.Scheme is "http" or "https") &&
        (uri.Host is "127.0.0.1" or "localhost" or "::1");

    public static bool IsSupportedServerUri(Uri uri) =>
        uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) || IsLocalHttpUri(uri);
}
