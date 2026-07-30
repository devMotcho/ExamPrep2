namespace Auth.Infrastructure.Identity;

/// <summary>
/// Stores a pending OAuth account linking request when an external login matches an existing account email.
/// </summary>
public class PendingOAuthLink
{
    /// <summary>The unique identifier for this link request.</summary>
    public Guid Id { get; set; }
    
    /// <summary>The user ID of the existing account.</summary>
    public string UserId { get; set; } = null!;
    
    /// <summary>The navigation property to the existing user.</summary>
    public User User { get; set; } = null!;
    
    /// <summary>The name of the OAuth provider (e.g., 'google').</summary>
    public string Provider { get; set; } = null!;
    
    /// <summary>The unique user identifier provided by the OAuth provider.</summary>
    public string ProviderKey { get; set; } = null!;
    
    /// <summary>The hashed ticket required to confirm the linking.</summary>
    public string TicketHash { get; set; } = null!;
    
    /// <summary>The expiration time of the pending link request.</summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>The time the pending link request was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>Indicates whether the link has already been confirmed and consumed.</summary>
    public bool IsUsed { get; set; }
    
    /// <summary>The number of failed confirmation attempts (used for lockout).</summary>
    public int Attempts { get; set; }
}
