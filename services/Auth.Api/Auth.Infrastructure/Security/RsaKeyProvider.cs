using System.Security.Cryptography;
using Auth.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Auth.Infrastructure.Security;

public class RsaKeyProvider : IJwksProvider
{
    public RSA PrivateKey { get; }
    public RSA PublicKey { get; }
    public string KeyId => "authapi-key-1"; // bump on key rotation

    public RsaKeyProvider(IConfiguration config)
    {
        PrivateKey = RSA.Create();
        PrivateKey.ImportFromPem(File.ReadAllText(config["Jwt:PrivateKeyPath"]!));

        PublicKey = RSA.Create();
        PublicKey.ImportFromPem(File.ReadAllText(config["Jwt:PublicKeyPath"]!));
    }
}