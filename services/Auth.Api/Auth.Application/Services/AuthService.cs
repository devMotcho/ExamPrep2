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
}
