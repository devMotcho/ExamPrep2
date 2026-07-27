using Auth.Application.Models;

namespace Auth.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(AppUser user);
    (string RawToken, string TokenHash) GenerateRefreshToken();
    string? HashRefreshToken(string rawToken);
}
