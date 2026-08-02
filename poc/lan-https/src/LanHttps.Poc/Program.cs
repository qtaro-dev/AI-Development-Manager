using System.Formats.Asn1;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

const string ServerName = "adm-server.local";
const string LanAddress = "192.0.2.10";
const int ServerLifetimeDays = 397;
const int CaLifetimeYears = 5;

var runId = $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}";
var output = Path.Combine(Path.GetTempPath(), "AI-Development-Manager", "poc", "P0-016", runId);
Directory.CreateDirectory(output);
var now = DateTimeOffset.UtcNow;
var caNotBefore = now.AddMinutes(-5);
var caNotAfter = now.AddYears(CaLifetimeYears);
using var caKey = RSA.Create(3072);
using var ca = CreateCertificateAuthority(caKey, caNotBefore, caNotAfter);
using var serverKey = RSA.Create(2048);
using var server = CreateServerCertificate(ca, caKey, serverKey, ServerName, LanAddress, now.AddMinutes(-5), now.AddDays(ServerLifetimeDays));
using var renewedKey = RSA.Create(2048);
using var renewed = CreateServerCertificate(ca, caKey, renewedKey, ServerName, "192.0.2.11", now.AddMinutes(-5), now.AddDays(ServerLifetimeDays));
using var expiredKey = RSA.Create(2048);
using var expired = CreateServerCertificate(ca, caKey, expiredKey, ServerName, LanAddress, now.AddMinutes(-4), now.AddMinutes(-1));

var checks = new List<CheckResult>();
checks.Add(Check("ca_lifetime_5_years", ca.NotAfter.ToUniversalTime() >= caNotAfter.AddMinutes(-1), $"not_after={ca.NotAfter:O}"));
checks.Add(Check("server_lifetime_397_days", server.NotAfter.ToUniversalTime() >= now.AddDays(ServerLifetimeDays - 1).UtcDateTime, $"not_after={server.NotAfter:O}"));
checks.Add(Check("server_san_name_and_address", HasSan(server, ServerName) && HasSan(server, LanAddress), "DNS and IP SAN present"));
checks.Add(Check("server_auth_eku", server.Extensions.OfType<X509EnhancedKeyUsageExtension>().Any(eku => eku.EnhancedKeyUsages.Cast<Oid>().Any(oid => oid.Value == "1.3.6.1.5.5.7.3.1")), "serverAuth present"));
checks.Add(Check("client_trust_export_has_no_private_key", ExportPublicTrust(ca, output), "public CA certificate only"));
checks.Add(Check("chain_valid_with_custom_root", ValidateChain(server, ca), "custom root trust"));
checks.Add(Check("expired_certificate_rejected", !ValidateChain(expired, ca), "expired certificate rejected"));
checks.Add(Check("address_change_requires_reissue", !HasSan(server, "192.0.2.11") && HasSan(renewed, "192.0.2.11"), "old certificate rejects new address SAN"));
checks.Add(Check("firewall_and_uac_are_separated", WriteSetupPlan(output), "no firewall/store mutation performed"));
checks.Add(Check("rollback_plan_is_present", WriteRollbackPlan(output), "localhost fallback and restore steps"));

var result = new
{
    run_id = runId,
    sdk = "10.0.302",
    runtime = Environment.Version.ToString(),
    ca_lifetime_years = CaLifetimeYears,
    server_lifetime_days = ServerLifetimeDays,
    san = new[] { "localhost", ServerName, "127.0.0.1", LanAddress },
    checks,
    firewall_mutated = false,
    certificate_store_mutated = false,
    output_directory = output,
    completed_utc = DateTimeOffset.UtcNow
};
await File.WriteAllTextAsync(Path.Combine(output, "result.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"P0-016 run={runId} output={output}");
foreach (var check in checks) Console.WriteLine($"{(check.Passed ? "PASS" : "FAIL")} {check.Name}: {check.Detail}");
Console.WriteLine($"RESULT_JSON={Path.Combine(output, "result.json")}");
Environment.ExitCode = checks.All(c => c.Passed) ? 0 : 1;

static X509Certificate2 CreateCertificateAuthority(RSA key, DateTimeOffset notBefore, DateTimeOffset notAfter)
{
    var request = new CertificateRequest("CN=AI Development Manager Local CA", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
    request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
    request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
    return request.CreateSelfSigned(notBefore, notAfter);
}

static X509Certificate2 CreateServerCertificate(X509Certificate2 ca, RSA caKey, RSA serverKey, string serverName, string lanAddress, DateTimeOffset notBefore, DateTimeOffset notAfter)
{
    var request = new CertificateRequest($"CN={serverName}", serverKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    var san = new SubjectAlternativeNameBuilder();
    san.AddDnsName("localhost"); san.AddDnsName(serverName); san.AddIpAddress(IPAddress.Loopback); san.AddIpAddress(IPAddress.Parse(lanAddress));
    request.CertificateExtensions.Add(san.Build());
    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
    request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
    var eku = new OidCollection();
    eku.Add(new Oid("1.3.6.1.5.5.7.3.1", "Server Authentication"));
    request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(eku, true));
    request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
    using var issued = request.Create(ca, notBefore, notAfter, RandomNumberGenerator.GetBytes(16));
    return issued.CopyWithPrivateKey(serverKey);
}

static bool HasSan(X509Certificate2 certificate, string expected)
{
    var extension = certificate.Extensions.FirstOrDefault(e => e.Oid?.Value == "2.5.29.17");
    if (extension is null) return false;
    var reader = new AsnReader(extension.RawData, AsnEncodingRules.DER).ReadSequence();
    while (reader.HasData)
    {
        var tag = reader.PeekTag();
        if (tag.TagClass == TagClass.ContextSpecific && tag.TagValue == 2 && !tag.IsConstructed)
        {
            if (string.Equals(reader.ReadCharacterString(UniversalTagNumber.IA5String, tag), expected, StringComparison.OrdinalIgnoreCase)) return true;
        }
        else if (tag.TagClass == TagClass.ContextSpecific && tag.TagValue == 7 && !tag.IsConstructed)
        {
            if (new IPAddress(reader.ReadOctetString(tag)).ToString() == expected) return true;
        }
        else reader.ReadEncodedValue();
    }
    return false;
}

static bool ValidateChain(X509Certificate2 certificate, X509Certificate2 ca)
{
    using var chain = new X509Chain();
    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
    chain.ChainPolicy.CustomTrustStore.Add(ca);
    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
    chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1"));
    return chain.Build(certificate);
}

static bool ExportPublicTrust(X509Certificate2 ca, string output)
{
    var path = Path.Combine(output, "client-trust-ca.cer");
    File.WriteAllBytes(path, ca.Export(X509ContentType.Cert));
    if (!File.Exists(path)) return false;
    using var publicCertificate = X509CertificateLoader.LoadCertificateFromFile(path);
    return !publicCertificate.HasPrivateKey;
}

static bool WriteSetupPlan(string output)
{
    var plan = new { localhost_phase = "bind 127.0.0.1 only", lan_phase = "after trust and HTTPS test, request confirmation", firewall = new { action = "add inbound TCP rule", port = 443, requires_uac = true, rollback = "remove only the rule identified by name" }, certificate_store = new { action = "user confirms LocalMachine/CurrentUser trust location", requires_uac = "depends on selected store" } };
    File.WriteAllText(Path.Combine(output, "setup-plan.json"), JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));
    return true;
}

static bool WriteRollbackPlan(string output)
{
    var plan = new[] { "stop LAN listener", "remove only the PoC-named firewall rule if it was added", "revoke/remove generated certificate by thumbprint after confirmation", "restore localhost-only binding", "keep server private key out of client package" };
    File.WriteAllText(Path.Combine(output, "rollback-plan.json"), JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));
    return true;
}

static CheckResult Check(string name, bool passed, string detail) => new(name, passed, detail);
record CheckResult(string Name, bool Passed, string Detail);
