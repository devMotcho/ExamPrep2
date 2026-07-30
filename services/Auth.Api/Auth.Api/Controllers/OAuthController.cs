using Auth.Api.Contracts;
using Auth.Api.Services;
using Auth.Application.Results;
using Auth.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

/// <summary>
/// Provides endpoints for handling third-party OAuth logins.
/// </summary>
[ApiController]
[Route("api/auth/oauth")]
public class OAuthController(IOAuthService oauthService, IConfiguration config, ICookieService cookieService) : ControllerBase
{
    /// <summary>
    /// Authenticates a user using an OAuth token provided by a third-party identity provider.
    /// </summary>
    /// <param name="provider">The name of the identity provider (e.g., 'google').</param>
    /// <param name="req">The request body containing the OAuth token.</param>
    /// <returns>
    /// Returns 200 OK with a JWT if successful. If an account link is required, returns 200 OK 
    /// with linkRequired=true and a linkTicket. Returns 401 Unauthorized if the token is invalid.
    /// </returns>
    /// <response code="200">Successfully authenticated or account linking required.</response>
    /// <response code="401">Invalid provider or token validation failed.</response>
    [HttpPost("{provider}/login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(string provider, OAuthLoginRequest req)
    {
        var result = await oauthService.LoginAsync(provider, req.Token);

        switch (result.Status)
        {
            case LoginStatus.InvalidCredentials:
                return Unauthorized(new { message = result.ErrorMessage });

            case LoginStatus.AccountLinkRequired:
                // No tokens issued. Frontend should prompt for the existing account's password
                // and call /api/auth/oauth/link/confirm.
                return Ok(new
                {
                    linkRequired = true,
                    maskedEmail = result.MaskedEmail,
                    linkTicket = result.LinkTicket
                });

            case LoginStatus.Success:
                cookieService.SetRefreshTokenCookie(Response, result.RawRefreshToken!);
                return Ok(new AuthResponse(result.AccessToken!));

            default:
                throw new InvalidOperationException($"Unhandled login status: {result.Status}");
        }
    }

    /// <summary>
    /// Confirms the linking of a third-party identity to an existing account.
    /// </summary>
    /// <param name="req">The request body containing the link ticket and the user's password.</param>
    /// <returns>Returns 200 OK with a JWT if successful. Returns 401 Unauthorized for incorrect password, or 400 BadRequest for an invalid ticket.</returns>
    /// <response code="200">Successfully linked account and authenticated.</response>
    /// <response code="400">The link ticket is invalid or expired.</response>
    /// <response code="401">Incorrect password.</response>
    /// <response code="429">Account locked due to too many failed attempts.</response>
    [HttpPost("link/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ConfirmLink(ConfirmLinkRequest req)
    {
        var result = await oauthService.ConfirmLinkAsync(req.LinkTicket, req.Password);

        switch (result.Status)
        {
            case ConfirmLinkStatus.InvalidOrExpiredTicket:
                return BadRequest(new { message = "This link request has expired. Please try signing in again." });

            case ConfirmLinkStatus.TooManyAttempts:
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Account locked due to too many failed attempts. Please try again later." });

            case ConfirmLinkStatus.InvalidPassword:
                return Unauthorized(new { message = "Incorrect password." });

            case ConfirmLinkStatus.Success:
                cookieService.SetRefreshTokenCookie(Response, result.RawRefreshToken!);
                return Ok(new AuthResponse(result.AccessToken!));

            default:
                throw new InvalidOperationException($"Unhandled confirm-link status: {result.Status}");
        }
    }

    /// <summary>
    /// Retrieves a list of available and enabled OAuth providers.
    /// </summary>
    /// <returns>A JSON object detailing provider status.</returns>
    /// <response code="200">Successfully retrieved providers.</response>
    [HttpGet("providers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAuthProviders()
    {
        var googleEnabled = config.GetValue<bool>("OAuthProviders:Google:Enabled");
        var googleClientId = config.GetValue<string>("OAuthProviders:Google:ClientId");

        return Ok(new
        {
            google = new
            {
                enabled = googleEnabled,
                clientId = googleEnabled ? googleClientId : null
            }
        });
    }
}
