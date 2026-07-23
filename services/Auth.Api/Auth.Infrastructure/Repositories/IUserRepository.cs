using Auth.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Auth.Infrastructure.Repositories;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task<IdentityResult> CreateAsync(User user, string password);
    Task<bool> CheckPasswordAsync(User user, string password);
}