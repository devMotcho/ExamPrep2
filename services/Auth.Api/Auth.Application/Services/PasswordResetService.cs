using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Auth.Application.Events;
using Auth.Application.Interfaces;
using Auth.Application.Results;
using Auth.Domain.Rules;
using ExamPrep.Shared.Constants;

namespace Auth.Application.Services;

/// <inheritdoc/>
public class PasswordResetService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordResetCodeRepository codes,
    IPasswordResetTicketRepository tickets,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork,
    ITokenService tokenService) : IPasswordResetService
{

    /// <inheritdoc/>
    public async Task<PasswordResetRequestResult> RequestAsync(string email)
    {
        // Look up silently — if the user does not exist we still return success
        // to the caller so callers cannot enumerate registered accounts.
        var user = await users.FindByEmailAsync(email);

        if (user is not null)
        {
            var rawCode = tokenService.GenerateOtpCode();
            var codeHash = tokenService.HashOtpCode(rawCode);

            await using var transaction = await unitOfWork.BeginTransactionAsync();

            await codes.AddAsync(user.Id, codeHash, DateTime.UtcNow.Add(AuthLifetimes.PasswordResetCodeLifetime));

            // Publish the event with the raw code so Notification.Api can embed
            // it in the email. Auth.Api only stores the hash.
            await outbox.AddAsync(
                topic: KafkaTopics.PasswordResetRequested,
                key: user.Id,
                payload: JsonSerializer.Serialize(
                    new PasswordResetRequestedEvent(user.Id, user.Email, rawCode)));

            await unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // Always return success — the controller must never leak whether the
        // email is registered.
        return PasswordResetRequestResult.Success();
    }

    /// <inheritdoc/>
    public async Task<PasswordResetVerifyResult> VerifyAsync(string email, string code)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
            return PasswordResetVerifyResult.CodeNotFound();

        var stored = await codes.FindActiveByUserIdAsync(user.Id);
        if (stored is null || stored.ExpiresAt <= DateTime.UtcNow)
            return PasswordResetVerifyResult.CodeNotFound();

        if (stored.Attempts >= AuthLifetimes.MaxCodeAttempts)
            return PasswordResetVerifyResult.TooManyAttempts();

        var submittedHash = tokenService.HashOtpCode(code);

        // Constant-time comparison prevents timing attacks even though the
        // code is only 8 digits (and therefore short-lived by design).
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(submittedHash),
                Encoding.UTF8.GetBytes(stored.CodeHash)))
        {
            await using var failTx = await unitOfWork.BeginTransactionAsync();
            await codes.IncrementAttemptsAsync(stored.Id);
            await unitOfWork.SaveChangesAsync();
            await failTx.CommitAsync();

            // Re-read attempt count to return the most accurate status
            return stored.Attempts + 1 >= AuthLifetimes.MaxCodeAttempts
                ? PasswordResetVerifyResult.TooManyAttempts()
                : PasswordResetVerifyResult.CodeInvalid();
        }

        // Code is correct — generate a short-lived ticket and mark the code used.
        var rawTicket = tokenService.GenerateResetTicket();
        var ticketHash = tokenService.HashResetTicket(rawTicket);

        await using var successTx = await unitOfWork.BeginTransactionAsync();
        await codes.MarkUsedAsync(stored.Id);
        await tickets.AddAsync(user.Id, ticketHash, DateTime.UtcNow.Add(AuthLifetimes.PasswordResetTicketLifetime));
        await unitOfWork.SaveChangesAsync();
        await successTx.CommitAsync();

        return PasswordResetVerifyResult.Success(rawTicket);
    }

    /// <inheritdoc/>
    public async Task<PasswordResetConfirmResult> ConfirmAsync(string rawResetTicket, string newPassword)
    {
        var ticketHash = tokenService.HashResetTicket(rawResetTicket);
        var ticket = await tickets.FindByHashAsync(ticketHash);

        if (ticket is null || ticket.IsUsed || ticket.ExpiresAt <= DateTime.UtcNow)
            return PasswordResetConfirmResult.TicketInvalid();

        // Set the new password via Identity (validates rules).
        var errors = await users.SetPasswordAsync(ticket.UserId, newPassword);
        var enumerable = errors as string[] ?? [.. errors];
        if (enumerable.Length != 0)
            return PasswordResetConfirmResult.PasswordValidationFailed(enumerable);

        // Invalidate the ticket and every existing refresh token in one transaction
        // so the user is forced to log in again on all devices.
        await using var tx = await unitOfWork.BeginTransactionAsync();
        await tickets.MarkUsedAsync(ticket.Id);
        await refreshTokens.RevokeAllForUserAsync(ticket.UserId);
        await unitOfWork.SaveChangesAsync();
        await tx.CommitAsync();

        return PasswordResetConfirmResult.Success();
    }
}
