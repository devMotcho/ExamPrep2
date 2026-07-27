using Auth.Application.Models;

namespace Auth.Application.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> FindByEmailAsync(string email);
    Task<AppUser?> FindByIdAsync(string userId);
    Task<AppUser?> FindByEmailOrUsernameAsync(string emailOrUsername);
    Task<CreateUserResult> CreateAsync(string email, string password);
    Task<bool> CheckPasswordAsync(string userId, string password);
}
