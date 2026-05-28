using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Convy.API.Services;

public static class McpJwtKeyLoader
{
    public static RsaSecurityKey? LoadPublicKey(IConfiguration configuration)
    {
        var pem = ReadConfiguredKey(configuration, "McpAuth:PublicKeyPem", "McpAuth:PublicKeyPemBase64", "McpAuth:PublicKeyPath");
        if (string.IsNullOrWhiteSpace(pem))
            return null;

        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return new RsaSecurityKey(rsa);
    }

    public static RsaSecurityKey? LoadPrivateKey(IConfiguration configuration)
    {
        var pem = ReadConfiguredKey(configuration, "McpAuth:PrivateKeyPem", "McpAuth:PrivateKeyPemBase64", "McpAuth:PrivateKeyPath");
        if (string.IsNullOrWhiteSpace(pem))
            return null;

        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return new RsaSecurityKey(rsa);
    }

    private static string? ReadConfiguredKey(
        IConfiguration configuration,
        string inlineKey,
        string base64Key,
        string pathKey)
    {
        var inline = configuration[inlineKey];
        if (!string.IsNullOrWhiteSpace(inline))
            return inline.Replace("\\n", "\n", StringComparison.Ordinal);

        var encoded = configuration[base64Key];
        if (!string.IsNullOrWhiteSpace(encoded))
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));

        var path = configuration[pathKey];
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path)
            ? File.ReadAllText(path)
            : null;
    }
}
