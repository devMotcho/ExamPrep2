using Auth.Application.Models;
using Microsoft.AspNetCore.Identity;

namespace Auth.Application.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> FindByEmailAsync(string email);
    Task<AppUser?> FindByIdAsync(string userId);
    Task<AppUser?> FindByEmailOrUsernameAsync(string emailOrUsername);
    Task<CreateUserResult> CreateAsync(string email, string password, bool emailConfirmed = false, string? partnerId = null);
    Task<CreateUserResult> CreateWithoutPasswordAsync(string email);
    Task<bool> CheckPasswordAsync(string userId, string password);
    Task<(bool Succeeded, IEnumerable<string> Errors)> ValidatePasswordAsync(string password);

    /// <summary>
    /// Replaces the user's password hash. Returns the Identity error descriptions
    /// on failure, or an empty collection on success.
    /// </summary>
    Task<IEnumerable<string>> SetPasswordAsync(string userId, string newPassword);

    /// <summary>Checks if the user's email has been verified.</summary>
    Task<bool> IsEmailConfirmedAsync(string userId);

    /// <summary>Marks the user's email as verified.</summary>
    Task<bool> ConfirmEmailAsync(string userId);

    /// <summary>Finds a user by an external login provider.</summary>
    Task<AppUser?> FindByLoginAsync(string provider, string providerKey);

    /// <summary>Links an external login to the user account.</summary>
    Task AddLoginAsync(string userId, string provider, string providerKey);

    /// <summary>Checks if the user is locked out.</summary>
    Task<bool> IsLockedOutAsync(string userId);

    /// <summary>Records a failed access attempt.</summary>
    Task AccessFailedAsync(string userId);

    /// <summary>Resets the failed access attempt count.</summary>
    Task ResetAccessFailedCountAsync(string userId);

    // Profile additions
    Task<IdentityResult> UpdateProfileAsync(string userId, string? firstName, string? lastName, string? phoneNumber);
    Task<IdentityResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<IdentityResult> DeactivateAsync(string userId);

    // Admin additions
    Task<(IReadOnlyList<AppUser> Users, int TotalCount)> SearchUsersAsync(string? searchTerm, int page, int pageSize);
    Task AddToRoleAsync(string userId, string role);
    Task RemoveFromRoleAsync(string userId, string role);
    Task<int> CountUsersInRoleAsync(string role);
}
