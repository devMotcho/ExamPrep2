using Auth.Api.Contracts;
using Auth.Application.Results;
using Auth.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

/// <summary>
/// Handles authentication tasks including registration, login, and token refreshing.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="req">The registration request containing email and password.</param>
    /// <returns>An AuthResponse with the access token if successful.</returns>
    /// <response code="201">Registration successful.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

    /// <summary>
    /// Issues a new access token using a valid refresh token.
    /// </summary>
    /// <returns>An AuthResponse with the new access token.</returns>
    /// <response code="200">Refresh successful.</response>
    /// <response code="401">No refresh token provided, or invalid/expired refresh token.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Authenticates a user and returns an access token.
    /// </summary>
    /// <param name="req">The login request containing email/username and password.</param>
    /// <returns>An AuthResponse with the access token if successful.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid email/username or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var result = await authService.LoginAsync(req.EmailOrUsername, req.Password);

        switch (result.Status)
        {
            case LoginStatus.InvalidCredentials:
                return Unauthorized(new { message = "Invalid email/username or password." });

            case LoginStatus.Success:
                SetRefreshCookie(result.RawRefreshToken!);
                return Ok(new AuthResponse(result.AccessToken!));

            default:
                throw new InvalidOperationException($"Unhandled login status: {result.Status}");
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