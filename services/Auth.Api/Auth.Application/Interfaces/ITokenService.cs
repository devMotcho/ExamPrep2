using Auth.Application.Models;

namespace Auth.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(AppUser user);
    (string RawToken, string TokenHash) GenerateRefreshToken();
    string? HashRefreshToken(string rawToken);

    // ── Password Reset ────────────────────────────────────────────────────────
    
    /// <summary>Generates a cryptographically random 8-digit numeric code.</summary>
    string GenerateOtpCode();

    /// <summary>Generates a cryptographically random 64-byte URL-safe ticket string.</summary>
    string GenerateResetTicket();

    /// <summary>SHA-256 of the UTF-8 encoded code string, returned as base64.</summary>
    string HashOtpCode(string code);

    /// <summary>
    /// SHA-256 of the base64-decoded ticket bytes, returned as base64.
    /// Returns an empty string (sentinel) if the input is not valid base64.
    /// </summary>
    string HashResetTicket(string rawTicket);
}
