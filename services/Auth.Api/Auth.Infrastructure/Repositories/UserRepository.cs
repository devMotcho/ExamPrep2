using Auth.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Auth.Infrastructure.Repositories;

public class UserRepository(UserManager<User> userManager) : IUserRepository
{

    public Task<bool> CheckPasswordAsync(User user, string password) =>
        userManager.CheckPasswordAsync(user, password);

    public Task<IdentityResult> CreateAsync(User user, string password) =>
        userManager.CreateAsync(user, password);

    public Task<User?> FindByEmailAsync(string email) =>
        userManager.FindByEmailAsync(email);
}