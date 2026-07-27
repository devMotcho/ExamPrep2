using System.Text.Json;
using Auth.Application.Events;
using Auth.Application.Interfaces;
using Auth.Application.Results;

namespace Auth.Application.Services;

public class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork,
    ITokenService tokens) : IAuthService
{
    public async Task<RegisterResult> RegisterAsync(string email, string password)
    {
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
            return RegisterResult.EmailAlreadyRegistered();

        await using var transaction = await unitOfWork.BeginTransactionAsync();

        var createResult = await users.CreateAsync(email, password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return RegisterResult.ValidationFailed(createResult.Errors);
        }

        var user = createResult.User!;

        var (rawRefreshToken, refreshTokenHash) = tokens.GenerateRefreshToken();
        await refreshTokens.AddAsync(user.Id, refreshTokenHash, DateTime.UtcNow.AddDays(30));

        await outbox.AddAsync(
            topic: "user-registered",
            key: user.Id,
            payload: JsonSerializer.Serialize(new UserRegisteredEvent(user.Id, user.Email, user.CreatedAt)));

        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        var accessToken = tokens.GenerateAccessToken(user);
        return RegisterResult.Success(accessToken, rawRefreshToken);
    }

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

    public async Task<LoginResult> LoginAsync(string emailOrUsername, string password)
    {
        var user = await users.FindByEmailOrUsernameAsync(emailOrUsername);
        if (user is null)
            return LoginResult.InvalidCredentials();

        var passwordValid = await users.CheckPasswordAsync(user.Id, password);
        if (!passwordValid)
            return LoginResult.InvalidCredentials();

        await using var transaction = await unitOfWork.BeginTransactionAsync();

        var (rawRefreshToken, refreshTokenHash) = tokens.GenerateRefreshToken();
        await refreshTokens.AddAsync(user.Id, refreshTokenHash, DateTime.UtcNow.AddDays(30));

        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        var accessToken = tokens.GenerateAccessToken(user);
        return LoginResult.Success(accessToken, rawRefreshToken);
    }
}
