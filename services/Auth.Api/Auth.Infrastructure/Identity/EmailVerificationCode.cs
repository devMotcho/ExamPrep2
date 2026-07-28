namespace Auth.Infrastructure.Identity;

/// <summary>
/// Stores a hashed 8-digit OTP issued for email verification.
/// One row per request; superseded rows are soft-deleted via <see cref="IsUsed"/>.
/// </summary>
public class EmailVerificationCode
{
    public Guid Id { get; set; }

    /// <summary>SHA-256 hash of the 8-digit code shown to the user.</summary>
    public string CodeHash { get; set; } = null!;

    public string UserId { get; set; } = null!;
    public User User { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Incremented on every failed verify attempt. Locked out at max attempts.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>Marked true once a verify attempt succeeds.</summary>
    public bool IsUsed { get; set; }
}
