using Auth.Application.Interfaces;
using Auth.Application.Models;
using Auth.Infrastructure.Identity;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories;

public class PasswordResetTicketRepository(AuthDbContext db) : IPasswordResetTicketRepository
{
    /// <inheritdoc/>
    public Task AddAsync(string userId, string ticketHash, DateTime expiresAt)
    {
        db.PasswordResetTickets.Add(new PasswordResetTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TicketHash = ticketHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        });
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<PasswordResetTicketModel?> FindByHashAsync(string ticketHash)
    {
        var ticket = await db.PasswordResetTickets
            .SingleOrDefaultAsync(t => t.TicketHash == ticketHash);

        return ticket is null ? null
            : new PasswordResetTicketModel(ticket.Id, ticket.UserId, ticket.TicketHash, ticket.ExpiresAt, ticket.IsUsed);
    }

    /// <inheritdoc/>
    public async Task MarkUsedAsync(Guid ticketId)
    {
        var ticket = await db.PasswordResetTickets.FindAsync(ticketId);
        if (ticket is not null)
            ticket.IsUsed = true;
    }
}
