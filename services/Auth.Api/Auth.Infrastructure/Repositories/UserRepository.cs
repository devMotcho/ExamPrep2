using Auth.Application.Interfaces;
using Auth.Application.Models;
using Auth.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Auth.Infrastructure.Repositories;

public class UserRepository(UserManager<User> userManager) : IUserRepository
{
    public async Task<AppUser?> FindByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : Map(user);
    }

    public async Task<CreateUserResult> CreateAsync(string email, string password)
    {
        var user = new User { UserName = email, Email = email, LockoutEnabled = true };
        var result = await userManager.CreateAsync(user, password);

        return result.Succeeded
            ? CreateUserResult.Success(Map(user))
            : CreateUserResult.Failure(result.Errors.Select(e => e.Description));
    }

    public async Task<CreateUserResult> CreateWithoutPasswordAsync(string email)
    {
        var user = new User { UserName = email, Email = email, EmailConfirmed = true, LockoutEnabled = true };
        var result = await userManager.CreateAsync(user); // No password

        return result.Succeeded
            ? CreateUserResult.Success(Map(user))
            : CreateUserResult.Failure(result.Errors.Select(e => e.Description));
    }

    public async Task<AppUser?> FindByIdAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null ? null : Map(user);
    }

    public async Task<AppUser?> FindByEmailOrUsernameAsync(string emailOrUsername)
    {
        // Try email first (most common), then fall back to username.
        // UserManager normalises both internally so casing is ignored.
        var user = await userManager.FindByEmailAsync(emailOrUsername)
                   ?? await userManager.FindByNameAsync(emailOrUsername);
        return user is null ? null : Map(user);
    }

    public async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is not null && await userManager.CheckPasswordAsync(user, password);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> SetPasswordAsync(string userId, string newPassword)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return ["User not found."];

        // UserManager requires us to remove the old password before adding a new one
        // if we are not verifying the old password (which we aren't, this is a reset).
        if (await userManager.HasPasswordAsync(user))
        {
            var removeResult = await userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
                return removeResult.Errors.Select(e => e.Description);
        }

        var addResult = await userManager.AddPasswordAsync(user, newPassword);
        return addResult.Succeeded
            ? []
            : addResult.Errors.Select(e => e.Description);
    }

    /// <inheritdoc/>
    public async Task<bool> IsEmailConfirmedAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is not null && await userManager.IsEmailConfirmedAsync(user);
    }

    /// <inheritdoc/>
    public async Task<bool> ConfirmEmailAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var result = await userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded;
    }

    /// <inheritdoc/>
    public async Task<AppUser?> FindByLoginAsync(string provider, string providerKey)
    {
        var user = await userManager.FindByLoginAsync(provider, providerKey);
        return user is null ? null : Map(user);
    }

    /// <inheritdoc/>
    public async Task AddLoginAsync(string userId, string provider, string providerKey)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            await userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsLockedOutAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is not null && await userManager.IsLockedOutAsync(user);
    }

    /// <inheritdoc/>
    public async Task AccessFailedAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            await userManager.AccessFailedAsync(user);
        }
    }

    private static AppUser Map(User user) => new(user.Id, user.Email!, user.CreatedAt);
}