using Auth.Application.Models;

namespace Auth.Application.Interfaces;

/// <summary>
/// Port for persisting and querying single-use password-reset tickets.
/// </summary>
public interface IPasswordResetTicketRepository
{
    /// <summary>Persists a new hashed ticket for <paramref name="userId"/>.</summary>
    Task AddAsync(string userId, string ticketHash, DateTime expiresAt);

    /// <summary>
    /// Returns the ticket matching <paramref name="ticketHash"/>, or
    /// <see langword="null"/> when not found.
    /// </summary>
    Task<PasswordResetTicketModel?> FindByHashAsync(string ticketHash);

    /// <summary>Marks the ticket as used so it cannot be replayed.</summary>
    Task MarkUsedAsync(Guid ticketId);
}
