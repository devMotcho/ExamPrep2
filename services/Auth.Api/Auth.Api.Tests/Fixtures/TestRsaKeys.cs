using System.Security.Cryptography;

namespace Auth.Api.Tests.Fixtures;

public static class TestRsaKeys
{
    public static (string PrivateKeyPath, string PublicKeyPath) GenerateTempKeyPair()
    {
        using var rsa = RSA.Create(2048);

        var privateKeyPath = Path.Combine(Path.GetTempPath(), $"test-private-{Guid.NewGuid()}.pem");
        var publicKeyPath = Path.Combine(Path.GetTempPath(), $"test-public-{Guid.NewGuid()}.pem");

        File.WriteAllText(privateKeyPath, rsa.ExportPkcs8PrivateKeyPem());
        File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());

        return (privateKeyPath, publicKeyPath);
    }

    public static void Cleanup(string privateKeyPath, string publicKeyPath)
    {
        if (File.Exists(privateKeyPath)) File.Delete(privateKeyPath);
        if (File.Exists(publicKeyPath)) File.Delete(publicKeyPath);
    }
}