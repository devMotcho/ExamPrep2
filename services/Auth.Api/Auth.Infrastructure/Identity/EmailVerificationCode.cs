namespace Auth.Infrastructure.Identity;

/// <summary>
/// Stores a hashed 8-digit OTP issued for email verification.
/// One row per request; superseded rows are soft-deleted via <see cref="IsUsed"/>.
/// </summary>
public class EmailVerificationCode
{
    /// <summary>The unique identifier for this code.</summary>
    public Guid Id { get; set; }

    /// <summary>SHA-256 hash of the 8-digit code shown to the user.</summary>
    public string CodeHash { get; set; } = null!;

    /// <summary>The email address this code is associated with.</summary>
    public string Email { get; set; } = null!;

    /// <summary>The date and time when this code expires.</summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>The date and time this code was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Incremented on every failed verify attempt. Locked out at max attempts.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>Marked true once a verify attempt succeeds.</summary>
    public bool IsUsed { get; set; }
}
