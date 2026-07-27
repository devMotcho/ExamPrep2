namespace Auth.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(string userId, string tokenHash, DateTime expiresAt);
}
