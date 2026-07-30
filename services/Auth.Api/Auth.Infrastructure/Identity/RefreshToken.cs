namespace Auth.Infrastructure.Identity;

/// <summary>
/// Represents a refresh token issued to a user for session management.
/// </summary>
public class RefreshToken
{
    /// <summary>The unique identifier of the token record.</summary>
    public Guid Id { get; set; }
    
    /// <summary>The SHA-256 hash of the raw refresh token value.</summary>
    public string TokenHash { get; set; } = null!;
    
    /// <summary>The expiration date and time of the token.</summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>The date and time the token was issued.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>Indicates whether the token has been explicitly revoked or consumed.</summary>
    public bool IsRevoked { get; set; }

    /// <summary>The ID of the user who owns the token.</summary>
    public string UserId { get; set; } = null!;
    
    /// <summary>The navigation property to the user.</summary>
    public User User { get; set; } = null!;
}