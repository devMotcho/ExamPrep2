using Auth.Api.Constants;
using Auth.Api.Contracts;
using Auth.Api.Services;
using Auth.Application.Results;
using Auth.Application.Services;
using Auth.Application.Interfaces;
using Auth.Domain.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Auth.Api.Controllers;

/// <summary>
/// Handles authentication tasks including registration, login, token refreshing and logout.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, ICookieService cookieService) : ControllerBase
{
    /// <summary>
    /// Requests a new email verification code (OTP).
    /// </summary>
    /// <param name="req">The request containing the email address.</param>
    /// <returns>A generic success message, regardless of whether the email is already registered.</returns>
    /// <response code="200">The request was processed (a code may have been sent).</response>
    [HttpPost("email-verification/request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestEmailVerification(RequestEmailVerificationRequest req)
    {
        var result = await authService.RequestEmailVerificationAsync(req.Email);
        
        if (result.Status == EmailVerificationRequestStatus.AlreadyVerified)
            return Ok(new { message = "Email is already verified." });

        return Ok(new { message = "If this email isn't already registered, a code has been sent." });
    }

    /// <summary>
    /// Verifies a user's email address using an OTP.
    /// </summary>
    [HttpPost("email-verification/verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VerifyEmailVerification(VerifyEmailVerificationRequest req)
    {
        var result = await authService.VerifyEmailAsync(req.Email, req.Code);

        return result.Status switch
        {
            EmailVerificationVerifyStatus.Success => Ok(new { message = "Email successfully verified." }),
            EmailVerificationVerifyStatus.AlreadyVerified => Ok(new { message = "Email is already verified." }),
            EmailVerificationVerifyStatus.CodeNotFound => BadRequest(new { message = "Invalid or expired code." }),
            EmailVerificationVerifyStatus.CodeInvalid => BadRequest(new { message = "Invalid or expired code." }),
            EmailVerificationVerifyStatus.TooManyAttempts => StatusCode(429, new { message = "Too many attempts. Request a new code." }),
            _ => throw new InvalidOperationException()
        };
    }

    /// <summary>
    /// Registers a new user using a previously requested verification code.
    /// </summary>
    /// <param name="req">The registration request containing email, verification code, and password.</param>
    /// <returns>An AuthResponse with the access token if successful.</returns>
    /// <response code="201">Registration successful and tokens issued.</response>
    /// <response code="400">Validation failed or invalid/expired code.</response>
    /// <response code="409">Email already registered.</response>
    /// <response code="429">Too many failed verification attempts.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var result = await authService.RegisterAsync(req.Email, req.Code, req.Password, req.PartnerEmail);

        return result.Status switch
        {
            RegisterStatus.EmailAlreadyRegistered => Conflict(new { message = "Email already registered." }),
            RegisterStatus.InvalidOrExpiredCode => BadRequest(new { message = "Invalid or expired code." }),
            RegisterStatus.TooManyAttempts => StatusCode(429, new { message = "Too many attempts. Request a new code." }),
            RegisterStatus.ValidationFailed => BadRequest(new { errors = result.Errors }),
            RegisterStatus.Success => IssueSuccessResponse(result),
            _ => throw new InvalidOperationException()
        };
    }

    private IActionResult IssueSuccessResponse(RegisterResult result)
    {
        cookieService.SetRefreshTokenCookie(Response, result.RawRefreshToken!);
        return CreatedAtAction(nameof(Register), new AuthResponse(result.AccessToken!));
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
        var rawToken = Request.Cookies[CookieNames.RefreshToken];
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
                cookieService.SetRefreshTokenCookie(Response, result.RawRefreshToken!);
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
    /// <response code="403">Email verification required.</response>
    /// <response code="429">Account locked due to too many failed attempts.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var result = await authService.LoginAsync(req.EmailOrUsername, req.Password);

        switch (result.Status)
        {
            case LoginStatus.InvalidCredentials:
                return Unauthorized(new { message = "Invalid email/username or password." });

            case LoginStatus.TooManyAttempts:
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Account locked due to too many failed attempts. Please try again later." });

            case LoginStatus.EmailNotVerified:
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Email verification required before login." });

            case LoginStatus.Success:
                cookieService.SetRefreshTokenCookie(Response, result.RawRefreshToken!);
                return Ok(new AuthResponse(result.AccessToken!));

            default:
                throw new InvalidOperationException($"Unhandled login status: {result.Status}");
        }
    }

    /// <summary>
    /// Revokes the current refresh token and clears the authentication cookie.
    /// Requires a valid access token so that logout is bound to an authenticated session.
    /// </summary>
    /// <response code="204">Logout successful. Cookie has been cleared.</response>
    /// <response code="401">No valid access token provided.</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromServices] IJwtBlocklistService blocklistService)
    {
        // Always expire the cookie on the response, regardless of outcome.
        // This ensures the browser clears its state even if the token was
        // already revoked or the cookie value was corrupted.
        cookieService.ExpireRefreshTokenCookie(Response);

        // Revoke the active JWT access token (Stateless Blocklist)
        var jti = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        if (!string.IsNullOrEmpty(jti))
        {
            await blocklistService.BlockTokenAsync(jti, AuthLifetimes.AccessTokenLifetime);
        }

        var rawToken = Request.Cookies[CookieNames.RefreshToken];
        if (string.IsNullOrEmpty(rawToken))
            return NoContent(); // Cookie already absent — client is already logged out

        // Both failure statuses return 204 — callers cannot tell the difference,
        // preventing an attacker from probing which tokens are still active.
        await authService.LogoutAsync(rawToken);
        return NoContent();
    }

}