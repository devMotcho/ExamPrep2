namespace Auth.Application.Models;

/// <summary>
/// Application-level view of a password-reset ticket row.
/// Never exposes EF entity types to the Application layer.
/// </summary>
public record PasswordResetTicketModel(
    Guid Id,
    string UserId,
    string TicketHash,
    DateTime ExpiresAt,
    bool IsUsed);
