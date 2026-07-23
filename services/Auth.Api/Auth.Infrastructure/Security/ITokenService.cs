using Auth.Infrastructure.Identity;

namespace Auth.Infrastructure.Security;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    (string RawToken, string TokenHash) GenerateRefreshToken();
}