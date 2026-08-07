using Auth.Application.Interfaces;
using Auth.Application.Models;
using Auth.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Auth.Domain.Rules;
using Auth.Infrastructure.Persistence;

namespace Auth.Infrastructure.Repositories;

public class UserRepository(UserManager<User> userManager, AuthDbContext dbContext) : IUserRepository
{
    public async Task<AppUser?> FindByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : await MapAsync(user);
    }

    public async Task<CreateUserResult> CreateAsync(string email, string password, bool emailConfirmed = false, string? partnerId = null)
    {
        var user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = emailConfirmed,
            ReferredByPartnerId = partnerId
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return CreateUserResult.Failure(result.Errors.Select(e => e.Description));

        await userManager.AddToRoleAsync(user, Roles.Student);

        return CreateUserResult.Success(await MapAsync(user));
    }

    public async Task<CreateUserResult> CreateWithoutPasswordAsync(string email)
    {
        var user = new User { UserName = email, Email = email, EmailConfirmed = true, LockoutEnabled = true };
        var result = await userManager.CreateAsync(user); // No password

        if (!result.Succeeded)
            return CreateUserResult.Failure(result.Errors.Select(e => e.Description));

        await userManager.AddToRoleAsync(user, Roles.Student);

        return CreateUserResult.Success(await MapAsync(user));
    }

    public async Task<AppUser?> FindByIdAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null ? null : await MapAsync(user);
    }

    public async Task<AppUser?> FindByEmailOrUsernameAsync(string emailOrUsername)
    {
        // Try email first (most common), then fall back to username.
        // UserManager normalises both internally so casing is ignored.
        var user = await userManager.FindByEmailAsync(emailOrUsername)
                   ?? await userManager.FindByNameAsync(emailOrUsername);
        return user is null ? null : await MapAsync(user);
    }

    public async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user != null && await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> ValidatePasswordAsync(string password)
    {
        var validators = userManager.PasswordValidators;
        var errors = new List<string>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(userManager, null!, password);
            if (!result.Succeeded)
                errors.AddRange(result.Errors.Select(e => e.Description));
        }
        return (errors.Count == 0, errors);
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

        user.EmailConfirmed = true;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    /// <inheritdoc/>
    public async Task<AppUser?> FindByLoginAsync(string provider, string providerKey)
    {
        var user = await userManager.FindByLoginAsync(provider, providerKey);
        return user is null ? null : await MapAsync(user);
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

    /// <inheritdoc/>
    public async Task ResetAccessFailedCountAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            await userManager.ResetAccessFailedCountAsync(user);
        }
    }

    public async Task<IdentityResult> UpdateProfileAsync(string userId, string? firstName, string? lastName, string? phoneNumber)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        user.FirstName = firstName;
        user.LastName = lastName;
        if (phoneNumber != null)
        {
            await userManager.SetPhoneNumberAsync(user, phoneNumber);
        }
        
        return await userManager.UpdateAsync(user);
    }

    public async Task<IdentityResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        return await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    }

    public async Task<IdentityResult> DeactivateAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;
        return await userManager.UpdateAsync(user);
    }

    public async Task<(IReadOnlyList<AppUser> Users, int TotalCount)> SearchUsersAsync(string? searchTerm, int page, int pageSize)
    {
        var query = dbContext.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm}%";
            query = query.Where(u => EF.Functions.ILike(u.Email!, pattern) || 
                                     (u.FirstName != null && EF.Functions.ILike(u.FirstName, pattern)) || 
                                     (u.LastName != null && EF.Functions.ILike(u.LastName, pattern)));
        }

        var totalCount = await query.CountAsync();
        
        var usersWithRoles = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                User = u,
                Roles = dbContext.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .ToList()
            })
            .ToListAsync();

        var appUsers = usersWithRoles.Select(u => 
            new AppUser(u.User.Id, u.User.Email!, u.User.CreatedAt, u.Roles!, u.User.FirstName, u.User.LastName))
            .ToList();

        return (appUsers, totalCount);
    }

    public async Task AddToRoleAsync(string userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is not null) await userManager.AddToRoleAsync(user, role);
    }

    public async Task RemoveFromRoleAsync(string userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is not null) await userManager.RemoveFromRoleAsync(user, role);
    }

    public async Task<int> CountUsersInRoleAsync(string role)
    {
        var usersInRole = await userManager.GetUsersInRoleAsync(role);
        return usersInRole.Count;
    }

    private async Task<AppUser> MapAsync(User user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new AppUser(user.Id, user.Email!, user.CreatedAt, roles.ToList(), user.FirstName, user.LastName, user.PhoneNumber);
    }
}