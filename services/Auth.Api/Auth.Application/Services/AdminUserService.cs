using Auth.Application.Models;
using Auth.Application.Results;
using Auth.Application.Interfaces;
using Auth.Domain.Rules;

namespace Auth.Application.Services;

public interface IAdminUserService
{
    Task<(IReadOnlyList<AppUser> Users, int TotalCount)> SearchUsersAsync(string? searchTerm, int page, int pageSize);
    Task<AppUser?> GetUserAsync(string userId);
    Task<AssignRoleResult> AssignRoleAsync(string userId, string role);
    Task<RemoveRoleResult> RemoveRoleAsync(string userId, string role);
    Task DeactivateUserAsync(string userId);
}

public class AdminUserService(IUserRepository users, IUnitOfWork unitOfWork) : IAdminUserService
{
    public Task<(IReadOnlyList<AppUser> Users, int TotalCount)> SearchUsersAsync(string? searchTerm, int page, int pageSize) =>
        users.SearchUsersAsync(searchTerm, page, pageSize);

    public Task<AppUser?> GetUserAsync(string userId) => users.FindByIdAsync(userId);

    public async Task<AssignRoleResult> AssignRoleAsync(string userId, string role)
    {
        if (!Roles.All.Contains(role))
            return AssignRoleResult.UnknownRole();

        var user = await users.FindByIdAsync(userId);
        if (user is null) return AssignRoleResult.UserNotFound();

        await users.AddToRoleAsync(userId, role);
        await unitOfWork.SaveChangesAsync();
        return AssignRoleResult.Success();
    }

    public async Task<RemoveRoleResult> RemoveRoleAsync(string userId, string role)
    {
        if (!Roles.All.Contains(role))
            return RemoveRoleResult.UnknownRole();

        if (Roles.Protected.Contains(role))
            return RemoveRoleResult.RoleIsProtected();

        var user = await users.FindByIdAsync(userId);
        if (user is null) return RemoveRoleResult.UserNotFound();

        if (role == Roles.Admin && await users.CountUsersInRoleAsync(Roles.Admin) <= 1)
            return RemoveRoleResult.LastAdminCannotBeRemoved();

        await users.RemoveFromRoleAsync(userId, role);
        await unitOfWork.SaveChangesAsync();
        return RemoveRoleResult.Success();
    }

    public async Task DeactivateUserAsync(string userId)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return;

        await users.DeactivateAsync(userId);
        await unitOfWork.SaveChangesAsync();
    }
}
