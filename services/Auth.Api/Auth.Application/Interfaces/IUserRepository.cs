using Auth.Application.Models;

namespace Auth.Application.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> FindByEmailAsync(string email);
    Task<AppUser?> FindByIdAsync(string userId);
    Task<AppUser?> FindByEmailOrUsernameAsync(string emailOrUsername);
    Task<CreateUserResult> CreateAsync(string email, string password);
    Task<CreateUserResult> CreateWithoutPasswordAsync(string email);
    Task<bool> CheckPasswordAsync(string userId, string password);

    /// <summary>
    /// Replaces the user's password hash. Returns the Identity error descriptions
    /// on failure, or an empty collection on success.
    /// </summary>
    Task<IEnumerable<string>> SetPasswordAsync(string userId, string newPassword);
}
