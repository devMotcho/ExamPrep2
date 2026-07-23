using Microsoft.AspNetCore.Identity;

namespace Auth.Infrastructure.Identity;

public class User : IdentityUser
{
    public RefreshToken? RefreshToken { get; set; }
}