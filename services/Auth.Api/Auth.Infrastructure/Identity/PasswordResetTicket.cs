namespace Auth.Infrastructure.Identity;

/// <summary>
/// Short-lived, single-use token issued after a successful OTP verify.
/// The client presents this ticket in the confirm step instead of the code.
/// </summary>
public class PasswordResetTicket
{
    public Guid Id { get; set; }

    /// <summary>SHA-256 hash of the raw ticket value sent to the client.</summary>
    public string TicketHash { get; set; } = null!;

    public string UserId { get; set; } = null!;
    public User User { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Marked true once the confirm step consumes this ticket.</summary>
    public bool IsUsed { get; set; }
}
