
using System.Text.Json;
using Auth.Api.Contracts;
using Auth.Infrastructure.Identity;
using Auth.Infrastructure.Messaging;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Persistence.Outbox;
using Auth.Infrastructure.Repositories;
using Auth.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;
[ApiController]
[Route("api/auth")]
public class AuthController(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork,
    ITokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var existing = await users.FindByEmailAsync(req.Email);
        if (existing is not null)
            return Conflict(new { message = "Email already registered." });

        var user = new User
        {
            UserName = req.Email,
            Email = req.Email
        };

        await using var transaction = await unitOfWork.BeginTransactionAsync();

        var createResult = await users.CreateAsync(user, req.Password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { errors = createResult.Errors.Select(e => e.Description) });
        }

        var (rawRefreshToken, refreshTokenHash) = tokens.GenerateRefreshToken();
        await refreshTokens.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        });

        await outbox.AddAsync(new OutboxMessage
        {
            Topic = "user-registered",
            Key = user.Id,
            Payload = JsonSerializer.Serialize(new UserRegisteredEvent(user.Id, user.Email, DateTime.UtcNow))
        });

        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        var accessToken = tokens.GenerateAccessToken(user);

        Response.Cookies.Append("refresh_token", rawRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        return CreatedAtAction(nameof(Register), new AuthResponse(accessToken));
    }
}