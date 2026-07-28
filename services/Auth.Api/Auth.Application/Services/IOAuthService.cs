using Auth.Application.Results;

namespace Auth.Application.Services;

/// <summary>
/// Orchestrates third-party OAuth authentication flows.
/// Each supported provider (e.g. Google) is registered as an
/// <see cref="Auth.Application.Interfaces.IExternalAuthProvider"/> implementation
/// and resolved by provider name at runtime.
/// </summary>
public interface IOAuthService
{
    /// <summary>
    /// Validates an external provider token and signs the user in, creating
    /// a passwordless account on first use.
    /// </summary>
    /// <param name="provider">
    /// Case-insensitive provider name (e.g. <c>"google"</c>). Must match the
    /// <see cref="Auth.Application.Interfaces.IExternalAuthProvider.ProviderName"/>
    /// of a registered provider.
    /// </param>
    /// <param name="token">
    /// The ID token or access token issued by the external provider and passed
    /// from the client after the OAuth redirect.
    /// </param>
    /// <returns>
    /// <see cref="LoginResult"/> indicating success with access and refresh tokens,
    /// or <see cref="LoginResult.InvalidCredentials()"/> when the provider is
    /// unknown, the token is invalid, or account creation fails.
    /// </returns>
    Task<LoginResult> LoginAsync(string provider, string token);
}
