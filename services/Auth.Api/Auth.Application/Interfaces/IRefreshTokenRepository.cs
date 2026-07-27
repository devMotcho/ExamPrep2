using Auth.Application.Models;

namespace Auth.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(string userId, string tokenHash, DateTime expiresAt);
    Task<RefreshTokenModel?> FindByHashAsync(string tokenHash);
    Task RevokeAsync(Guid tokenId);
}
