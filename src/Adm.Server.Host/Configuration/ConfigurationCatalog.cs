namespace Adm.Server.Host.Configuration;

public sealed record ConfigurationCatalogEntry(
    string Key,
    string DefaultValue,
    bool UserChangeable,
    bool RequiresRestart,
    bool IsSecretReference);

public static class ConfigurationCatalog
{
    public static IReadOnlyList<ConfigurationCatalogEntry> Entries { get; } =
    [
        new("Server:BindAddress", "127.0.0.1", false, true, false),
        new("Server:Port", "0", true, true, false),
        new("Secrets:ApiTokenReference", "未設定", false, true, true),
        new("Secrets:CertificateReference", "未設定", false, true, true)
    ];
}
