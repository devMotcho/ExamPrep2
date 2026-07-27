using System.Text.Json;
using Auth.Application.Events;
using Auth.Application.Interfaces;
using Auth.Application.Results;

namespace Auth.Application.Services;

public class OAuthService(
    IEnumerable<IExternalAuthProvider> providers,
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork,
    ITokenService tokens) : IOAuthService
{
    public async Task<LoginResult> LoginAsync(string providerName, string token)
    {
        var provider = providers.FirstOrDefault(p => string.Equals(p.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));
        
        if (provider is null)
            return LoginResult.InvalidCredentials();

        var externalUser = await provider.ValidateTokenAsync(token);
        if (externalUser is null)
            return LoginResult.InvalidCredentials();

        var user = await users.FindByEmailAsync(externalUser.Email);
        
        await using var transaction = await unitOfWork.BeginTransactionAsync();

        if (user is null)
        {
            var createResult = await users.CreateWithoutPasswordAsync(externalUser.Email);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return LoginResult.InvalidCredentials();
            }
            user = createResult.User!;

            await outbox.AddAsync(
                topic: "user-registered",
                key: user.Id,
                payload: JsonSerializer.Serialize(new UserRegisteredEvent(user.Id, user.Email, user.CreatedAt)));
        }

        var (rawRefreshToken, refreshTokenHash) = tokens.GenerateRefreshToken();
        await refreshTokens.AddAsync(user.Id, refreshTokenHash, DateTime.UtcNow.AddDays(30));

        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        var accessToken = tokens.GenerateAccessToken(user);
        return LoginResult.Success(accessToken, rawRefreshToken);
    }
}
