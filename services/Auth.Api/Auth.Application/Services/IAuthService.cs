using Auth.Application.Results;

namespace Auth.Application.Services;

/// <summary>
/// Orchestrates credential-based authentication flows: registration, login,
/// access-token refresh, and logout.
/// </summary>
public interface IAuthService
{
    Task<EmailVerificationRequestResult> RequestEmailVerificationAsync(string email);

    Task<EmailVerificationVerifyResult> VerifyEmailAsync(string email, string code);

    /// <summary>
    /// Creates a new user account with the given credentials.
    /// </summary>
    /// <param name="email">The email address to register. Must be unique.</param>
    /// <param name="code">The verification code sent to the email.</param>
    /// <param name="password">The plain-text password; hashed before persistence.</param>
    /// <param name="partnerEmail">Optional partner email linking the user for life.</param>
    /// <returns>
    /// <see cref="RegisterResult"/> indicating success, a duplicate-email conflict,
    /// or ASP.NET Identity validation failures.
    /// </returns>
    Task<RegisterResult> RegisterAsync(string email, string code, string password, string? partnerEmail = null);

    /// <summary>
    /// Exchanges a valid refresh token for a new access token and a rotated
    /// refresh token.
    /// </summary>
    /// <param name="rawRefreshToken">
    /// The raw (un-hashed) refresh token value read from the HTTP-only cookie.
    /// </param>
    /// <returns>
    /// <see cref="RefreshResult"/> indicating success with new tokens, or a
    /// failure if the token is missing, expired, or already revoked.
    /// </returns>
    Task<RefreshResult> RefreshAsync(string rawRefreshToken);

    /// <summary>
    /// Validates credentials and, on success, issues a new access token and
    /// a refresh token.
    /// </summary>
    /// <param name="emailOrUsername">Email address or username; lookup is case-insensitive.</param>
    /// <param name="password">The plain-text password to verify.</param>
    /// <returns>
    /// <see cref="LoginResult"/> indicating success or invalid credentials.
    /// Both a wrong password and an unknown user map to the same failure status
    /// to prevent user-enumeration attacks.
    /// </returns>
    Task<LoginResult> LoginAsync(string emailOrUsername, string password);

    /// <summary>
    /// Revokes the refresh token identified by <paramref name="rawRefreshToken"/>.
    /// The caller should always clear the HTTP-only cookie regardless of the returned
    /// status, preventing token-probing via response differentiation.
    /// </summary>
    /// <param name="rawRefreshToken">
    /// The raw (un-hashed) refresh token read from the HTTP-only cookie.
    /// </param>
    /// <returns>
    /// <see cref="LogoutResult"/> indicating success, a missing/malformed token,
    /// or a token that does not exist in the store (already revoked or forged).
    /// </returns>
    Task<LogoutResult> LogoutAsync(string rawRefreshToken);
}
