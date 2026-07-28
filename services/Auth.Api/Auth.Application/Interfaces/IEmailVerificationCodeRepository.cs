using Auth.Application.Models;

namespace Auth.Application.Interfaces;

/// <summary>
/// Port for persisting and querying email verification OTP codes.
/// </summary>
public interface IEmailVerificationCodeRepository
{
    /// <summary>Persists a new hashed OTP code for <paramref name="userId"/>.</summary>
    Task AddAsync(string userId, string codeHash, DateTime expiresAt);

    /// <summary>
    /// Returns the most recent active (non-used, non-expired) code for the user,
    /// or <see langword="null"/> when none exists.
    /// </summary>
    Task<EmailVerificationCodeModel?> FindActiveByUserIdAsync(string userId);

    /// <summary>Increments the failed-attempt counter on the given code row.</summary>
    Task IncrementAttemptsAsync(Guid codeId);

    /// <summary>Marks the code as used so it cannot be replayed.</summary>
    Task MarkUsedAsync(Guid codeId);
}
