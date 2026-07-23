using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Auth.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Security;

public class JwtTokenService(RsaKeyProvider keys, IConfiguration config) : ITokenService
{
    private readonly RsaKeyProvider _keys = keys;
    private readonly IConfiguration _config = config;
    
    public string GenerateAccessToken(User user)
    {
        var creds = new SigningCredentials(
            new RsaSecurityKey(_keys.PrivateKey)
            { KeyId = _keys.KeyId },
            SecurityAlgorithms.RsaSha256
        );

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}