namespace Auth.Infrastructure.Identity;

/// <summary>
/// Short-lived, single-use token issued after a successful OTP verify.
/// The client presents this ticket in the confirm step instead of the code.
/// </summary>
public class PasswordResetTicket
{
    /// <summary>The unique identifier for this reset ticket.</summary>
    public Guid Id { get; set; }

    /// <summary>SHA-256 hash of the raw ticket value sent to the client.</summary>
    public string TicketHash { get; set; } = null!;

    /// <summary>The unique identifier of the user who owns this ticket.</summary>
    public string UserId { get; set; } = null!;
    
    /// <summary>Navigation property to the associated user.</summary>
    public User User { get; set; } = null!;

    /// <summary>The date and time when this ticket expires.</summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>The date and time this ticket was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Marked true once the confirm step consumes this ticket.</summary>
    public bool IsUsed { get; set; }
}
