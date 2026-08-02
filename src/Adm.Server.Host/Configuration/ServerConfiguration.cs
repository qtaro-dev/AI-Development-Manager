using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Adm.Server.Host.Configuration;

public static class ServerConfiguration
{
    public static IServiceCollection AddServerConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ServerOptions>()
            .Bind(configuration.GetSection(ServerOptions.SectionName))
            .Validate(options => options.BindAddress is "127.0.0.1" or "localhost", "Server:BindAddressは127.0.0.1またはlocalhostに限定されます。")
            .Validate(options => options.Port is >= 0 and <= 65535, "Server:Portは0から65535の範囲で指定してください。")
            .ValidateOnStart();

        var inlineSecretKeys = configuration
            .GetSection(SecretReferenceOptions.SectionName)
            .GetChildren()
            .Where(section => section.Value is not null && section.Key is not nameof(SecretReferenceOptions.ApiTokenReference) and not nameof(SecretReferenceOptions.CertificateReference))
            .Select(section => section.Key)
            .ToArray();

        services.AddOptions<SecretReferenceOptions>()
            .Bind(configuration.GetSection(SecretReferenceOptions.SectionName))
            .Validate(options => string.IsNullOrWhiteSpace(options.ApiTokenReference) || !options.ApiTokenReference.Contains('='), "Secrets:ApiTokenReferenceは参照名だけを指定してください。")
            .Validate(options => string.IsNullOrWhiteSpace(options.CertificateReference) || !options.CertificateReference.Contains('='), "Secrets:CertificateReferenceは参照名だけを指定してください。")
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<SecretReferenceOptions>>(
            new InlineSecretOptionsValidator(inlineSecretKeys));

        return services;
    }

    private sealed class InlineSecretOptionsValidator(string[] inlineSecretKeys) : IValidateOptions<SecretReferenceOptions>
    {
        public ValidateOptionsResult Validate(string? name, SecretReferenceOptions options)
        {
            return inlineSecretKeys.Length == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail($"Secretsに直接保存できない設定キーがあります: {string.Join(", ", inlineSecretKeys)}");
        }
    }
}
