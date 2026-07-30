namespace Auth.Infrastructure.Identity;

/// <summary>
/// Stores a hashed 8-digit OTP issued during the password-reset request step.
/// One row per request; superseded rows are soft-deleted via <see cref="IsUsed"/>.
/// </summary>
public class PasswordResetCode
{
    /// <summary>The unique identifier for this reset code.</summary>
    public Guid Id { get; set; }

    /// <summary>SHA-256 hash of the 8-digit code shown to the user.</summary>
    public string CodeHash { get; set; } = null!;

    /// <summary>The unique identifier of the user requesting the reset.</summary>
    public string UserId { get; set; } = null!;
    
    /// <summary>Navigation property to the associated user.</summary>
    public User User { get; set; } = null!;

    /// <summary>The date and time when this code expires.</summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>The date and time this code was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Incremented on every failed verify attempt. Locked out at
    /// <see cref="Auth.Domain.Rules.AuthLifetimes.MaxCodeAttempts"/>.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>Marked true once a verify attempt succeeds and a reset ticket is issued.</summary>
    public bool IsUsed { get; set; }
}
