namespace Adm.Wpf.Shell;

public sealed record ServerConnectionOptions(Uri ServerUri)
{
    public const string ServerUrlArgument = "--server-url";
    public static readonly Uri DefaultServerUri = new("http://127.0.0.1:5181/");

    public static ServerConnectionOptions FromArguments(string[]? args)
    {
        var value = args?
            .Select(argument => argument.Split('=', 2))
            .Where(parts => parts.Length == 2 && string.Equals(parts[0], ServerUrlArgument, StringComparison.OrdinalIgnoreCase))
            .Select(parts => parts[1])
            .LastOrDefault();

        if (string.IsNullOrWhiteSpace(value))
        {
            return new ServerConnectionOptions(DefaultServerUri);
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !ShellNavigationPolicy.IsLocalHttpUri(uri))
        {
            throw new ArgumentException("Server URLはlocalhostのhttp://またはhttps://で指定してください。", nameof(args));
        }

        return new ServerConnectionOptions(EnsureTrailingSlash(uri));
    }

    private static Uri EnsureTrailingSlash(Uri uri) =>
        uri.AbsolutePath.EndsWith('/')
            ? uri
            : new Uri(uri + "/");
}
