using System.Text.Json;
using Auth.Application.Events;
using Auth.Application.Interfaces;
using Auth.Application.Results;
using Auth.Application.Models;
using Auth.Application.Constants;

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

    /// <inheritdoc/>
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

        var linkedUser = await users.FindByLoginAsync(providerName, externalUser.ProviderId);
        if (linkedUser is not null)
            return await IssueTokensAsync(linkedUser);

        var existingUser = await users.FindByEmailAsync(externalUser.Email);

        if (existingUser is null)
            return await CreateAndLinkNewUserAsync(providerName, externalUser);

        // Collision: an account with this email exists, but this identity has
        // never been linked. Require proof of ownership before linking.
        var rawTicket = tokens.GenerateResetTicket();
        var ticketHash = tokens.HashResetTicket(rawTicket);

        await pendingLinks.AddAsync(
            userId: existingUser.Id,
            provider: providerName,
            providerKey: externalUser.ProviderId,
            ticketHash: ticketHash,
            expiresAt: DateTime.UtcNow.Add(AuthLifetimes.LinkTicketLifetime));
        await unitOfWork.SaveChangesAsync();

        return LoginResult.AccountLinkRequired(rawTicket, MaskEmail(existingUser.Email));
    }

    /// <inheritdoc/>
    public async Task<ConfirmLinkResult> ConfirmLinkAsync(string linkTicket, string password)
    {
        var ticketHash = tokens.HashResetTicket(linkTicket);
        var pending = await pendingLinks.FindByTicketHashAsync(ticketHash);

        if (pending is null || pending.IsUsed || pending.ExpiresAt < DateTime.UtcNow)
            return ConfirmLinkResult.InvalidOrExpiredTicket();

        if (pending.Attempts >= AuthAttempts.MaxLinkAttempts)
            return ConfirmLinkResult.TooManyAttempts();

        var user = await users.FindByIdAsync(pending.UserId);
        if (user is null)
            return ConfirmLinkResult.InvalidOrExpiredTicket();

        if (await users.IsLockedOutAsync(user.Id))
            return ConfirmLinkResult.InvalidPassword();

        var passwordValid = await users.CheckPasswordAsync(user.Id, password);
        if (!passwordValid)
        {
            await pendingLinks.IncrementAttemptsAsync(pending.Id);
            await users.AccessFailedAsync(user.Id);
            await unitOfWork.SaveChangesAsync();
            return ConfirmLinkResult.InvalidPassword();
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync();

        await users.AddLoginAsync(user.Id, pending.Provider, pending.ProviderKey);
        await pendingLinks.MarkUsedAsync(pending.Id);

        var loginResult = await IssueTokensAsync(user);

        await transaction.CommitAsync();

        return ConfirmLinkResult.Success(loginResult.AccessToken!, loginResult.RawRefreshToken!);
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

        var result = await IssueTokensAsync(user);
        await transaction.CommitAsync();

        return result;
    }

    private async Task<LoginResult> IssueTokensAsync(AppUser user)
    {
        var (rawRefreshToken, refreshTokenHash) = tokens.GenerateRefreshToken();
        await refreshTokens.AddAsync(user.Id, refreshTokenHash,
            DateTime.UtcNow.Add(AuthLifetimes.RefreshTokenLifetime));
        await unitOfWork.SaveChangesAsync();

        var accessToken = tokens.GenerateAccessToken(user);
        return LoginResult.Success(accessToken, rawRefreshToken);
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex <= 2 ? email : $"{email[..2]}***{email[atIndex..]}";
    }
}