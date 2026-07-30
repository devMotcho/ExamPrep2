using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Auth.Application.Interfaces;
using Auth.Application.Models;
using Auth.Domain.Rules;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Security;

public class JwtTokenService(RsaKeyProvider keys, IConfiguration config) : ITokenService
{
    public string GenerateAccessToken(AppUser user)
    {
        var creds = new SigningCredentials(
            new RsaSecurityKey(keys.PrivateKey) { KeyId = keys.KeyId },
            SecurityAlgorithms.RsaSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.Add(AuthLifetimes.AccessTokenLifetime),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string RawToken, string TokenHash) GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenRules.RefreshTokenByteLength);
        var rawToken = Convert.ToBase64String(bytes);
        var tokenHash = Convert.ToBase64String(SHA256.HashData(bytes));
        return (rawToken, tokenHash);
    }

    public string? HashRefreshToken(string rawToken)
    {
        try
        {
            var bytes = Convert.FromBase64String(rawToken);
            return Convert.ToBase64String(SHA256.HashData(bytes));
        }
        catch (FormatException)
        {
            return null; // not valid Base64 — treat as token-not-found
        }
    }

    public string GenerateOtpCode()
    {
        // Use a random uint, take modulo 100_000_000 to get 0–99_999_999,
        // then zero-pad to 8 digits so the code is always the same length.
        var value = RandomNumberGenerator.GetInt32(0, OtpRules.MaxOtpValue);
        return value.ToString($"D{OtpRules.CodeLength}");
    }

    public string GenerateResetTicket() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenRules.ResetTicketByteLength));

    public string HashOtpCode(string code) =>
        Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code)));

    public string HashResetTicket(string rawTicket)
    {
        try
        {
            var bytes = Convert.FromBase64String(rawTicket);
            return Convert.ToBase64String(SHA256.HashData(bytes));
        }
        catch (FormatException)
        {
            // Not valid base64 — return a sentinel that will never match a stored hash.
            return string.Empty;
        }
    }
}