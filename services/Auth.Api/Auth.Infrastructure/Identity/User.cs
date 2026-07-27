using Microsoft.AspNetCore.Identity;

namespace Auth.Infrastructure.Identity;

public class User : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}