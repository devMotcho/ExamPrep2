
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace Auth.Infrastructure.Security;

public class RsaKeyProvider
{
    public RSA PrivateKey { get; }
    public RSA PublicKey { get; }
    public string KeyId { get; } = "authapi-key-1"; // bump rotation

    public RsaKeyProvider(IConfiguration config)
    {
        PrivateKey = RSA.Create();
        PrivateKey.ImportFromPem(File.ReadAllText(config["Jwt:PrivateKeyPath"]!));

        PublicKey = RSA.Create();
        PublicKey.ImportFromPem(File.ReadAllText(config["Jwt:PublicKeyPath"]!));
    }
}