namespace Auth.Application.Interfaces;

public interface IPendingOAuthLinkRepository
{
    Task AddAsync(string userId, string provider, string providerKey, string ticketHash, DateTime expiresAt);
    Task<PendingOAuthLinkModel?> FindByTicketHashAsync(string ticketHash);
    Task MarkUsedAsync(Guid id);
    Task IncrementAttemptsAsync(Guid id);
}

public record PendingOAuthLinkModel(
    Guid Id,
    string UserId,
    string Provider,
    string ProviderKey,
    string TicketHash,
    DateTime ExpiresAt,
    bool IsUsed,
    int Attempts
);
