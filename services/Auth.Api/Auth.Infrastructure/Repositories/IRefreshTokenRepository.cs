using Auth.Infrastructure.Identity;

namespace Auth.Infrastructure.Repositories;


public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> FindByHashAsync(string tokenHash);
}