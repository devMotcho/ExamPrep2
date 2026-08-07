using System.Text.Json;
using Auth.Application.Events;
using Auth.Application.Models;
using Auth.Application.Results;
using Auth.Application.Interfaces;
using Auth.Domain.Rules;

namespace Auth.Application.Services;

public interface IStudentProfileService
{
    Task<AppUser?> GetProfileAsync(string userId);
    Task<UpdateProfileResult> UpdateProfileAsync(string userId, string? firstName, string? lastName, string? phoneNumber);
    Task RequestChangePasswordCodeAsync(string userId);
    Task<ChangePasswordResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword, string code);
    Task DeactivateAsync(string userId);
}

public class StudentProfileService(
    IUserRepository users, 
    IRefreshTokenRepository refreshTokens, 
    IPasswordResetCodeRepository codes,
    ITokenService tokenService,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork)
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

    public async Task RequestChangePasswordCodeAsync(string userId)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is not null)
        {
            var rawCode = tokenService.GenerateOtpCode();
            var codeHash = tokenService.HashOtpCode(rawCode);

            await using var transaction = await unitOfWork.BeginTransactionAsync();

            await codes.AddAsync(user.Id, codeHash, DateTime.UtcNow.Add(AuthLifetimes.PasswordResetCodeLifetime));

            await outbox.AddAsync(
                topic: ExamPrep.Shared.Constants.KafkaTopics.PasswordChangeCodeRequested,
                key: user.Id,
                payload: JsonSerializer.Serialize(
                    new PasswordChangeCodeRequestedEvent(user.Id, user.Email, rawCode)));

            await unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();
        }
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword, string code)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return ChangePasswordResult.IncorrectCurrentPassword(); // don't leak existence

        var storedCode = await codes.FindActiveByUserIdAsync(userId);
        if (storedCode is null || storedCode.ExpiresAt < DateTime.UtcNow)
            return ChangePasswordResult.CodeNotFound();

        if (storedCode.Attempts >= AuthLifetimes.MaxCodeAttempts)
            return ChangePasswordResult.TooManyAttempts();

        if (tokenService.HashOtpCode(code) != storedCode.CodeHash)
        {
            await codes.IncrementAttemptsAsync(storedCode.Id);
            await unitOfWork.SaveChangesAsync();
            return ChangePasswordResult.CodeInvalid();
        }

        var result = await users.ChangePasswordAsync(userId, currentPassword, newPassword);
        if (result.Succeeded)
        {
            await codes.MarkUsedAsync(storedCode.Id);
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
