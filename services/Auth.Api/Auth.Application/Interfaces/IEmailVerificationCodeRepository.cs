using Auth.Application.Models;

namespace Auth.Application.Interfaces;

/// <summary>
/// Port for persisting and querying email verification OTP codes.
/// </summary>
public interface IEmailVerificationCodeRepository
{
    Task UpsertAsync(string email, string codeHash, DateTime expiresAt);
    Task<EmailVerificationCodeModel?> FindActiveByEmailAsync(string email);
    Task IncrementAttemptsAsync(Guid id);
    Task MarkUsedAsync(Guid id);
}
