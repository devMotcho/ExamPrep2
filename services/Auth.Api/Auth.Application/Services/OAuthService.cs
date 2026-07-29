using System.Text.Json;
using Auth.Application.Events;
using Auth.Application.Interfaces;
using Auth.Application.Results;
using Auth.Application.Models;

namespace Auth.Application.Services;

public class OAuthService(
    IEnumerable<IExternalAuthProvider> providers,
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPendingOAuthLinkRepository pendingLinks,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork,
    ITokenService tokens) : IOAuthService
{
    private static readonly TimeSpan LinkTicketLifetime = TimeSpan.FromMinutes(10);

    public async Task<LoginResult> LoginAsync(string providerName, string token)
    {
        var provider = providers.FirstOrDefault(p =>
            string.Equals(p.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));
        if (provider is null)
            return LoginResult.InvalidCredentials($"Provider '{providerName}' is not configured or enabled.");

        var externalUser = await provider.ValidateTokenAsync(token);
        if (externalUser is null)
            return LoginResult.InvalidCredentials($"Token validation failed for provider '{providerName}'.");
            
        if (!externalUser.EmailVerified)
            return LoginResult.InvalidCredentials("The email address provided by the identity provider is not verified.");

        // Fast path: this exact provider identity is already linked to an account
        var linkedUser = await users.FindByLoginAsync(providerName, externalUser.ProviderId);
        if (linkedUser is not null)
        {
            return await IssueTokensAsync(linkedUser);
        }

        var existingUser = await users.FindByEmailAsync(externalUser.Email);

        if (existingUser is null)
        {
            // No collision - genuinely new user, safe to create and link immediately.
            // Nothing pre-existing is at risk here, so no confirmation step is needed.
            return await CreateAndLinkNewUserAsync(providerName, externalUser);
        }

        // Collision: an account with this email already exists, but this Google
        // identity has never been linked to it. Do NOT link or issue tokens yet -
        // require proof of ownership of the existing account first.
        var rawTicket = tokens.GenerateResetTicket();
        var ticketHash = tokens.HashResetTicket(rawTicket); 

        await pendingLinks.AddAsync(
            userId: existingUser.Id,
            provider: providerName,
            providerKey: externalUser.ProviderId,
            ticketHash: ticketHash,
            expiresAt: DateTime.UtcNow.Add(LinkTicketLifetime)
        );
        await unitOfWork.SaveChangesAsync();

        return LoginResult.AccountLinkRequired(rawTicket, MaskEmail(existingUser.Email));
    }

    public async Task<ConfirmLinkResult> ConfirmLinkAsync(string linkTicket, string password)
    {
        var ticketHash = tokens.HashResetTicket(linkTicket);
        var pending = await pendingLinks.FindByTicketHashAsync(ticketHash);

        if (pending is null || pending.IsUsed || pending.ExpiresAt < DateTime.UtcNow)
            return ConfirmLinkResult.InvalidOrExpiredTicket();

        if (pending.Attempts >= 5)
            return ConfirmLinkResult.InvalidOrExpiredTicket();

        var user = await users.FindByIdAsync(pending.UserId);
        if (user is null)
            return ConfirmLinkResult.InvalidOrExpiredTicket();

        if (await users.IsLockedOutAsync(user.Id))
            return ConfirmLinkResult.InvalidPassword(); // or a new Lockout status, but InvalidPassword works

        var passwordValid = await users.CheckPasswordAsync(user.Id, password);
        if (!passwordValid)
        {
            await pendingLinks.IncrementAttemptsAsync(pending.Id);
            await users.AccessFailedAsync(user.Id);
            await unitOfWork.SaveChangesAsync(); // Persist the attempt increment
            return ConfirmLinkResult.InvalidPassword();
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync();

        await users.AddLoginAsync(user.Id, pending.Provider, pending.ProviderKey);
        await pendingLinks.MarkUsedAsync(pending.Id);

        var (rawRefreshToken, refreshTokenHash) = tokens.GenerateRefreshToken();
        await refreshTokens.AddAsync(user.Id, refreshTokenHash, DateTime.UtcNow.AddDays(30));

        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        var accessToken = tokens.GenerateAccessToken(user);
        return ConfirmLinkResult.Success(accessToken, rawRefreshToken);
    }

    private async Task<LoginResult> CreateAndLinkNewUserAsync(string providerName, ExternalUserInfo externalUser)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync();

        var createResult = await users.CreateWithoutPasswordAsync(externalUser.Email);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return LoginResult.InvalidCredentials("Failed to create the user account in the database.");
        }

        var user = createResult.User!;
        await users.AddLoginAsync(user.Id, providerName, externalUser.ProviderId);

        await outbox.AddAsync(
            topic: "user-registered",
            key: user.Id,
            payload: JsonSerializer.Serialize(new UserRegisteredEvent(user.Id, user.Email, user.CreatedAt)));

        var (rawRefreshToken, refreshTokenHash) = tokens.GenerateRefreshToken();
        await refreshTokens.AddAsync(user.Id, refreshTokenHash, DateTime.UtcNow.AddDays(30));

        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        var accessToken = tokens.GenerateAccessToken(user);
        return LoginResult.Success(accessToken, rawRefreshToken);
    }

    private async Task<LoginResult> IssueTokensAsync(AppUser user)
    {
        var (rawRefreshToken, refreshTokenHash) = tokens.GenerateRefreshToken();
        await refreshTokens.AddAsync(user.Id, refreshTokenHash, DateTime.UtcNow.AddDays(30));
        await unitOfWork.SaveChangesAsync();

        var accessToken = tokens.GenerateAccessToken(user);
        return LoginResult.Success(accessToken, rawRefreshToken);
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 2) return email; // too short to usefully mask
        return $"{email[..2]}***{email[atIndex..]}";
    }
}
