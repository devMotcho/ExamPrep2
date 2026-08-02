using Auth.Application.Models;
using Auth.Application.Results;
using Auth.Application.Interfaces;

namespace Auth.Application.Services;

public interface IStudentProfileService
{
    Task<AppUser?> GetProfileAsync(string userId);
    Task<UpdateProfileResult> UpdateProfileAsync(string userId, string? firstName, string? lastName, string? phoneNumber);
    Task<ChangePasswordResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task DeactivateAsync(string userId);
}

public class StudentProfileService(IUserRepository users, IRefreshTokenRepository refreshTokens, IUnitOfWork unitOfWork)
    : IStudentProfileService
{
    public async Task<AppUser?> GetProfileAsync(string userId) => await users.FindByIdAsync(userId);

    public async Task<UpdateProfileResult> UpdateProfileAsync(string userId, string? firstName, string? lastName, string? phoneNumber)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return UpdateProfileResult.ValidationFailed(["User not found."]);

        var result = await users.UpdateProfileAsync(userId, firstName, lastName, phoneNumber);
        return result.Succeeded
            ? UpdateProfileResult.Success()
            : UpdateProfileResult.ValidationFailed(result.Errors.Select(e => e.Description));
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return ChangePasswordResult.IncorrectCurrentPassword(); // don't leak existence

        var result = await users.ChangePasswordAsync(userId, currentPassword, newPassword);
        if (result.Succeeded)
        {
            await refreshTokens.RevokeAllForUserAsync(userId);
            await unitOfWork.SaveChangesAsync();
            return ChangePasswordResult.Success();
        }

        var isWrongCurrentPassword = result.Errors.Any(e => e.Code == "PasswordMismatch");
        return isWrongCurrentPassword
            ? ChangePasswordResult.IncorrectCurrentPassword()
            : ChangePasswordResult.ValidationFailed(result.Errors.Select(e => e.Description));
    }

    public async Task DeactivateAsync(string userId)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return;

        await users.DeactivateAsync(userId);
        await refreshTokens.RevokeAllForUserAsync(userId); // kill all sessions on deactivation
        await unitOfWork.SaveChangesAsync();
    }
}
