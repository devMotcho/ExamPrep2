namespace Auth.Application.Models;

/// <summary>Application-level view of a refresh token returned by IRefreshTokenRepository.
/// Never exposes EF entity types to the Application layer.</summary>
public record RefreshTokenModel(
    Guid Id,
    string UserId,
    string TokenHash,
    DateTime ExpiresAt,
    bool IsRevoked);
