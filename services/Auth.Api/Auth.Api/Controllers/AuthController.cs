using Auth.Api.Contracts;
using Auth.Application.Results;
using Auth.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var result = await authService.RegisterAsync(req.Email, req.Password);

        switch (result.Status)
        {
            case RegisterStatus.EmailAlreadyRegistered:
                return Conflict(new { message = "Email already registered." });

            case RegisterStatus.ValidationFailed:
                return BadRequest(new { errors = result.Errors });

            case RegisterStatus.Success:
                Response.Cookies.Append("refresh_token", result.RawRefreshToken!, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                });
                return CreatedAtAction(nameof(Register), new AuthResponse(result.AccessToken!));

            default:
                throw new InvalidOperationException($"Unhandled register status: {result.Status}");
        }
    }
}