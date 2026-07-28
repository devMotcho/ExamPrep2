using Auth.Application.Interfaces;
using Auth.Application.Models;
using Auth.Infrastructure.Identity;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

    public async Task<RefreshTokenModel?> FindByHashAsync(string tokenHash)
    {
        var token = await db.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash);

        return token is null ? null
            : new RefreshTokenModel(token.Id, token.UserId, token.TokenHash, token.ExpiresAt, token.IsRevoked);
    }

    public async Task RevokeAsync(Guid tokenId)
    {
        var token = await db.RefreshTokens.FindAsync(tokenId);
        if (token is not null)
            token.IsRevoked = true;
    }
}