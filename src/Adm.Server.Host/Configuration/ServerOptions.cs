namespace Adm.Server.Host.Configuration;

public sealed class ServerOptions
{
    public const string SectionName = "Server";

    public string BindAddress { get; set; } = "127.0.0.1";

    public int Port { get; set; }
}
