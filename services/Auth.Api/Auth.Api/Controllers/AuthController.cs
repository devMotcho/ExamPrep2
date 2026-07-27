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
                SetRefreshCookie(result.RawRefreshToken!);
                return CreatedAtAction(nameof(Register), new AuthResponse(result.AccessToken!));

            default:
                throw new InvalidOperationException($"Unhandled register status: {result.Status}");
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var rawToken = Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(rawToken))
            return Unauthorized(new { message = "No refresh token provided." });

        var result = await authService.RefreshAsync(rawToken);

        switch (result.Status)
        {
            // Both statuses map to 401 — callers cannot distinguish which, preventing token probing
            case RefreshStatus.TokenNotFound:
            case RefreshStatus.TokenExpiredOrRevoked:
                return Unauthorized(new { message = "Invalid or expired refresh token." });

            case RefreshStatus.Success:
                SetRefreshCookie(result.RawRefreshToken!);
                return Ok(new AuthResponse(result.AccessToken!));

            default:
                throw new InvalidOperationException($"Unhandled refresh status: {result.Status}");
        }
    }

    private void SetRefreshCookie(string rawToken) =>
        Response.Cookies.Append("refresh_token", rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
}