using Auth.Application.Models;

namespace Auth.Application.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> FindByEmailAsync(string email);
    Task<CreateUserResult> CreateAsync(string email, string password);
    Task<bool> CheckPasswordAsync(string userId, string password);
}
