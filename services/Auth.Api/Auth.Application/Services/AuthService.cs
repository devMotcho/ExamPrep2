using System.Text.Json;
using Auth.Application.Results;
using Auth.Infrastructure.Identity;
using Auth.Infrastructure.Messaging;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Persistence.Outbox;
using Auth.Infrastructure.Repositories;
using Auth.Infrastructure.Security;

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

        var user = new User
        {
            UserName = email,
            Email = email
        };

        await using var transaction = await unitOfWork.BeginTransactionAsync();

        var createResult = await users.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return RegisterResult.ValidationFailed(createResult.Errors.Select(e => e.Description));
        }

        var (rawRefreshToken, refreshTokenHash) = tokens.GenerateRefreshToken();
        await refreshTokens.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        });

        await outbox.AddAsync(new OutboxMessage
        {
            Topic = "user-registered",
            Key = user.Id,
            Payload = JsonSerializer.Serialize(new UserRegisteredEvent(user.Id, user.Email!, user.CreatedAt))
        });

        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        var accessToken = tokens.GenerateAccessToken(user);
        return RegisterResult.Success(accessToken, rawRefreshToken);
    }
}
