using System.Text.Json;

using Auth.Application.Events;
using Auth.Application.Interfaces;
using Auth.Application.Results;
using Auth.Domain.Rules;

namespace Auth.Application.Services;

public class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork,
    ITokenService tokens,
    IEmailVerificationCodeRepository verificationCodes) : IAuthService
{

    /// <inheritdoc/>
    public async Task<EmailVerificationRequestResult> RequestEmailVerificationAsync(string email)
    {
        var existingUser = await users.FindByEmailAsync(email);
        if (existingUser is not null && await users.IsEmailConfirmedAsync(existingUser.Id))
        {
            return EmailVerificationRequestResult.AlreadyVerified();
        }

        var rawCode = tokens.GenerateOtpCode();
        var codeHash = tokens.HashOtpCode(rawCode);

        await verificationCodes.UpsertAsync(email, codeHash, DateTime.UtcNow.Add(AuthLifetimes.EmailVerificationCodeLifetime));

        await outbox.AddAsync(
            topic: ExamPrep.Shared.Constants.KafkaTopics.EmailVerificationCodeRequested,
            key: email,
            payload: JsonSerializer.Serialize(new EmailVerificationCodeRequestedEvent(email, rawCode)));

        await unitOfWork.SaveChangesAsync();

        return EmailVerificationRequestResult.Success();
    }

    /// <inheritdoc/>
    public async Task<EmailVerificationVerifyResult> VerifyEmailAsync(string email, string code)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
            return EmailVerificationVerifyResult.CodeNotFound();

        if (await users.IsEmailConfirmedAsync(user.Id))
            return EmailVerificationVerifyResult.AlreadyVerified();

        var storedCode = await verificationCodes.FindActiveByEmailAsync(email);
        if (storedCode is null || storedCode.ExpiresAt < DateTime.UtcNow)
            return EmailVerificationVerifyResult.CodeNotFound();

        if (storedCode.Attempts >= AuthLifetimes.MaxCodeAttempts)
            return EmailVerificationVerifyResult.TooManyAttempts();

        if (tokens.HashOtpCode(code) != storedCode.CodeHash)
        {
            await verificationCodes.IncrementAttemptsAsync(storedCode.Id);
            await unitOfWork.SaveChangesAsync();
            return EmailVerificationVerifyResult.CodeInvalid();
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync();

        var confirmResult = await users.ConfirmEmailAsync(user.Id);
        if (!confirmResult)
        {
            await transaction.RollbackAsync();
            return EmailVerificationVerifyResult.CodeInvalid();
        }

        await verificationCodes.MarkUsedAsync(storedCode.Id);
        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        return EmailVerificationVerifyResult.Success();
    }

    public async Task<RegisterResult> RegisterAsync(string email, string code, string password, string? partnerEmail = null)
    {
        var existingUser = await users.FindByEmailAsync(email);
        if (existingUser is not null)
            return RegisterResult.EmailAlreadyRegistered();

        var storedCode = await verificationCodes.FindActiveByEmailAsync(email);
        if (storedCode is null || storedCode.ExpiresAt < DateTime.UtcNow)
            return RegisterResult.InvalidOrExpiredCode();

        if (storedCode.Attempts >= AuthLifetimes.MaxCodeAttempts)
            return RegisterResult.TooManyAttempts();

        if (tokens.HashOtpCode(code) != storedCode.CodeHash)
        {
            await verificationCodes.IncrementAttemptsAsync(storedCode.Id);
            await unitOfWork.SaveChangesAsync();
            return RegisterResult.InvalidOrExpiredCode();
        }

        var passwordCheck = await users.ValidatePasswordAsync(password);
        if (!passwordCheck.Succeeded)
            return RegisterResult.ValidationFailed(passwordCheck.Errors);

        string? partnerId = null;
        if (!string.IsNullOrEmpty(partnerEmail))
        {
            var partner = await users.FindByEmailAsync(partnerEmail);
            if (partner is not null && partner.Roles.Contains(Roles.Partner))
            {
                partnerId = partner.Id;
            }
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync();

        var createResult = await users.CreateAsync(email, password, emailConfirmed: true, partnerId);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return RegisterResult.ValidationFailed(createResult.Errors);
        }

        var user = createResult.User!;

        await verificationCodes.MarkUsedAsync(storedCode.Id);

        var (rawRefreshToken, refreshTokenHash) = tokens.GenerateRefreshToken();
        await refreshTokens.AddAsync(user.Id, refreshTokenHash, DateTime.UtcNow.AddDays(30));

        await outbox.AddAsync(
            topic: ExamPrep.Shared.Constants.KafkaTopics.UserRegistered,
            key: user.Id,
            payload: JsonSerializer.Serialize(new UserRegisteredEvent(user.Id, user.Email, user.CreatedAt)));

        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        var accessToken = tokens.GenerateAccessToken(user);
        return RegisterResult.Success(accessToken, rawRefreshToken);
    }

    /// <inheritdoc/>
    public async Task<RefreshResult> RefreshAsync(string rawRefreshToken)
    {
        var tokenHash = tokens.HashRefreshToken(rawRefreshToken);
        if (tokenHash is null)
            return RefreshResult.TokenNotFound(); // invalid Base64 — not a real token
        var stored = await refreshTokens.FindByHashAsync(tokenHash);

        if (stored is null)
            return RefreshResult.TokenNotFound();

        if (stored.IsRevoked || stored.ExpiresAt <= DateTime.UtcNow)
            return RefreshResult.TokenExpiredOrRevoked();

        var user = await users.FindByIdAsync(stored.UserId);
        if (user is null)
            return RefreshResult.TokenNotFound();

        await using var transaction = await unitOfWork.BeginTransactionAsync();

        // Token rotation: revoke the consumed token before issuing a new one
        await refreshTokens.RevokeAsync(stored.Id);

        var (newRawToken, newTokenHash) = tokens.GenerateRefreshToken();
        await refreshTokens.AddAsync(user.Id, newTokenHash, DateTime.UtcNow.AddDays(30));

        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        var accessToken = tokens.GenerateAccessToken(user);
        return RefreshResult.Success(accessToken, newRawToken);
    }

    /// <inheritdoc/>
    public async Task<LoginResult> LoginAsync(string emailOrUsername, string password)
    {
        var user = await users.FindByEmailOrUsernameAsync(emailOrUsername);
        if (user is null)
            return LoginResult.InvalidCredentials();

        if (await users.IsLockedOutAsync(user.Id))
            return LoginResult.TooManyAttempts();

        var passwordValid = await users.CheckPasswordAsync(user.Id, password);
        if (!passwordValid)
        {
            await users.AccessFailedAsync(user.Id);
            if (await users.IsLockedOutAsync(user.Id))
                return LoginResult.TooManyAttempts();
            return LoginResult.InvalidCredentials();
        }

        await users.ResetAccessFailedCountAsync(user.Id);

        if (!await users.IsEmailConfirmedAsync(user.Id))
            return LoginResult.EmailNotVerified();

        await using var transaction = await unitOfWork.BeginTransactionAsync();

        var (rawRefreshToken, refreshTokenHash) = tokens.GenerateRefreshToken();
        await refreshTokens.AddAsync(user.Id, refreshTokenHash, DateTime.UtcNow.AddDays(30));

        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        var accessToken = tokens.GenerateAccessToken(user);
        return LoginResult.Success(accessToken, rawRefreshToken);
    }

    /// <inheritdoc/>
    public async Task<LogoutResult> LogoutAsync(string rawRefreshToken)
    {
        // Hash first — an invalid Base64 value can never match a stored hash.
        var tokenHash = tokens.HashRefreshToken(rawRefreshToken);
        if (tokenHash is null)
            return LogoutResult.TokenNotFound();

        var stored = await refreshTokens.FindByHashAsync(tokenHash);
        if (stored is null)
            return LogoutResult.TokenNotRecognised();

        // Revoke and persist in a transaction — if the save fails the token
        // remains valid, which is safer than silently swallowing the error.
        await using var transaction = await unitOfWork.BeginTransactionAsync();
        await refreshTokens.RevokeAsync(stored.Id);
        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        return LogoutResult.Success();
    }
}

