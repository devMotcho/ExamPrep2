using Auth.Application.Interfaces;
using Auth.Infrastructure.Identity;
using Auth.Infrastructure.Persistence;

namespace Auth.Infrastructure.Repositories;

public class RefreshTokenRepository(AuthDbContext db) : IRefreshTokenRepository
{
    public Task AddAsync(string userId, string tokenHash, DateTime expiresAt)
    {
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        });
        return Task.CompletedTask;
    }
}