using Microsoft.AspNetCore.Identity;

namespace Auth.Infrastructure.Identity;

public class User : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<PasswordResetCode> PasswordResetCodes { get; set; } = new List<PasswordResetCode>();
    public ICollection<PasswordResetTicket> PasswordResetTickets { get; set; } = new List<PasswordResetTicket>();
    public ICollection<EmailVerificationCode> EmailVerificationCodes { get; set; } = new List<EmailVerificationCode>();
}