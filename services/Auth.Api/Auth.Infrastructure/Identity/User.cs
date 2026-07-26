using Microsoft.AspNetCore.Identity;

namespace Auth.Infrastructure.Identity;

public class User : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public RefreshToken? RefreshToken { get; set; }
}