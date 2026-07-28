using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Auth.Application.Events;
using Auth.Application.Interfaces;
using Auth.Application.Results;

namespace Auth.Application.Services;

/// <inheritdoc/>
public class EmailVerificationService(
    IUserRepository users,
    IEmailVerificationCodeRepository codes,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork,
    ITokenService tokenService) : IEmailVerificationService
{
    public const int MaxAttempts = 5;
    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(15);

    /// <inheritdoc/>
    public async Task<EmailVerificationRequestResult> RequestAsync(string email)
    {
        var user = await users.FindByEmailAsync(email);

        if (user is not null)
        {
            if (await users.IsEmailConfirmedAsync(user.Id))
                return EmailVerificationRequestResult.AlreadyVerified();

            var rawCode = tokenService.GenerateOtpCode();
            var codeHash = tokenService.HashOtpCode(rawCode);

            await using var transaction = await unitOfWork.BeginTransactionAsync();

            await codes.AddAsync(user.Id, codeHash, DateTime.UtcNow.Add(CodeTtl));

            await outbox.AddAsync(
                topic: "email-verification-requested",
                key: user.Id,
                payload: JsonSerializer.Serialize(
                    new EmailVerificationRequestedEvent(user.Id, user.Email, rawCode)));

            await unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        return EmailVerificationRequestResult.Success();
    }

    /// <inheritdoc/>
    public async Task<EmailVerificationVerifyResult> VerifyAsync(string email, string code)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
            return EmailVerificationVerifyResult.CodeNotFound();

        if (await users.IsEmailConfirmedAsync(user.Id))
            return EmailVerificationVerifyResult.AlreadyVerified();

        var stored = await codes.FindActiveByUserIdAsync(user.Id);
        if (stored is null || stored.ExpiresAt <= DateTime.UtcNow)
            return EmailVerificationVerifyResult.CodeNotFound();

        if (stored.Attempts >= MaxAttempts)
            return EmailVerificationVerifyResult.TooManyAttempts();

        var submittedHash = tokenService.HashOtpCode(code);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(submittedHash),
                Encoding.UTF8.GetBytes(stored.CodeHash)))
        {
            await using var failTx = await unitOfWork.BeginTransactionAsync();
            await codes.IncrementAttemptsAsync(stored.Id);
            await unitOfWork.SaveChangesAsync();
            await failTx.CommitAsync();

            return stored.Attempts + 1 >= MaxAttempts
                ? EmailVerificationVerifyResult.TooManyAttempts()
                : EmailVerificationVerifyResult.CodeInvalid();
        }

        await using var successTx = await unitOfWork.BeginTransactionAsync();
        await codes.MarkUsedAsync(stored.Id);
        
        var confirmResult = await users.ConfirmEmailAsync(user.Id);
        if (!confirmResult)
        {
            // If identity somehow fails to confirm, rollback
            await successTx.RollbackAsync();
            throw new InvalidOperationException("Failed to confirm email in Identity.");
        }

        await unitOfWork.SaveChangesAsync();
        await successTx.CommitAsync();

        return EmailVerificationVerifyResult.Success();
    }
}
