using Auth.Infrastructure.Identity;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories;


public class RefreshTokenRepository(AuthDbContext db) : IRefreshTokenRepository
{
    public Task AddAsync(RefreshToken token)
    {
        db.RefreshTokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> FindByHashAsync(string tokenHash) =>
        db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash);
}