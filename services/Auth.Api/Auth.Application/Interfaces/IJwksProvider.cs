using System.Security.Cryptography;

namespace Auth.Application.Interfaces;

/// <summary>Provides the RSA public key for JWKS exposure.
/// Implemented in Auth.Infrastructure; consumed by both
/// Auth.Api (JwksController) and the JWT validation PostConfigure.</summary>
public interface IJwksProvider
{
    RSA PublicKey { get; }
    string KeyId { get; }
}
