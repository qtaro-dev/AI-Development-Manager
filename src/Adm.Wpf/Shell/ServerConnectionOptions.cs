using Adm.Application.ExecutionProfiles;

namespace Adm.Wpf.Shell;

public enum WpfExecutionMode
{
    Local,
    Server,
}

public sealed record ServerConnectionOptions(WpfExecutionMode Mode, Uri? ServerUri)
{
    public const string ServerUrlArgument = "--server-url";
    public static readonly Uri DefaultServerUri = new("http://127.0.0.1:5181/");
    public bool IsLocal => Mode == WpfExecutionMode.Local;

    public static ServerConnectionOptions FromArguments(string[]? args)
    {
        var value = args?
            .Select(argument => argument.Split('=', 2))
            .Where(parts => parts.Length == 2 && string.Equals(parts[0], ServerUrlArgument, StringComparison.OrdinalIgnoreCase))
            .Select(parts => parts[1])
            .LastOrDefault();

        if (string.IsNullOrWhiteSpace(value))
        {
            return new ServerConnectionOptions(WpfExecutionMode.Local, null);
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !ShellNavigationPolicy.IsLocalHttpUri(uri))
        {
            throw new ArgumentException("Server URLはlocalhostのhttp://またはhttps://で指定してください。", nameof(args));
        }

        return new ServerConnectionOptions(WpfExecutionMode.Server, EnsureTrailingSlash(uri));
    }

    public static bool HasServerUrlArgument(string[]? args) => args?.Any(argument =>
        argument.StartsWith(ServerUrlArgument + "=", StringComparison.OrdinalIgnoreCase)) == true;

    public static ServerConnectionOptions FromProfile(ExecutionProfile profile) =>
        profile.Mode == ExecutionProfileMode.Server && Uri.TryCreate(profile.ServerUri, UriKind.Absolute, out var uri)
            ? new(WpfExecutionMode.Server, EnsureTrailingSlash(uri))
            : new(WpfExecutionMode.Local, null);

    private static Uri EnsureTrailingSlash(Uri uri) =>
        uri.AbsolutePath.EndsWith('/')
            ? uri
            : new Uri(uri + "/");
}
