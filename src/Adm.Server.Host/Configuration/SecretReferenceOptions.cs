namespace Adm.Server.Host.Configuration;

public sealed class SecretReferenceOptions
{
    public const string SectionName = "Secrets";

    public string? ApiTokenReference { get; set; }

    public string? CertificateReference { get; set; }
}
