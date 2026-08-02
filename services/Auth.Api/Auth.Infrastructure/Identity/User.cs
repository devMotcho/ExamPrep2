using Microsoft.AspNetCore.Identity;

namespace Auth.Infrastructure.Identity;

/// <summary>
/// Represents a user within the Identity system.
/// </summary>
public class User : IdentityUser
{
    /// <summary>The UTC timestamp when the user account was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsPremium { get; set; }
    public DateTime? PremiumUntil { get; set; }

    // self-service editable profile fields
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>Collection of active and revoked refresh tokens belonging to the user.</summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    
    /// <summary>Collection of password reset OTP codes requested by the user.</summary>
    public ICollection<PasswordResetCode> PasswordResetCodes { get; set; } = new List<PasswordResetCode>();
    
    /// <summary>Collection of password reset tickets issued to the user.</summary>
    public ICollection<PasswordResetTicket> PasswordResetTickets { get; set; } = new List<PasswordResetTicket>();
    
    /// <summary>Collection of pending third-party OAuth links for the user.</summary>
    public ICollection<PendingOAuthLink> PendingOAuthLinks { get; set; } = new List<PendingOAuthLink>();
}