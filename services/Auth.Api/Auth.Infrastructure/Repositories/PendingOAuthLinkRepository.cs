using Auth.Application.Interfaces;
using Auth.Infrastructure.Identity;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories;

public class PendingOAuthLinkRepository(AuthDbContext db) : IPendingOAuthLinkRepository
{
    public Task AddAsync(string userId, string provider, string providerKey, string ticketHash, DateTime expiresAt)
    {
        db.PendingOAuthLinks.Add(new PendingOAuthLink
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            ProviderKey = providerKey,
            TicketHash = ticketHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        });
        return Task.CompletedTask;
    }

    public async Task<PendingOAuthLinkModel?> FindByTicketHashAsync(string ticketHash)
    {
        var link = await db.PendingOAuthLinks.SingleOrDefaultAsync(l => l.TicketHash == ticketHash);
        return link is null ? null 
            : new PendingOAuthLinkModel(link.Id, link.UserId, link.Provider, link.ProviderKey, link.TicketHash, link.ExpiresAt, link.IsUsed, link.Attempts);
    }

    public async Task MarkUsedAsync(Guid id)
    {
        var link = await db.PendingOAuthLinks.FindAsync(id);
        if (link is not null)
            link.IsUsed = true;
    }

    public async Task IncrementAttemptsAsync(Guid id)
    {
        var link = await db.PendingOAuthLinks.FindAsync(id);
        if (link is not null)
            link.Attempts++;
    }
}
