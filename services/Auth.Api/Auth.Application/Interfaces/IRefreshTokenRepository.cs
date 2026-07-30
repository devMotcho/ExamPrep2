using Auth.Application.Models;

namespace Auth.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(string userId, string tokenHash, DateTime expiresAt);
    Task<RefreshTokenModel?> FindByHashAsync(string tokenHash);
    Task RevokeAsync(Guid tokenId);

    /// <summary>
    /// Revokes every active refresh token belonging to <paramref name="userId"/>.
    /// Called after a successful password reset to invalidate all existing sessions.
    /// </summary>
    Task RevokeAllForUserAsync(string userId);
}

