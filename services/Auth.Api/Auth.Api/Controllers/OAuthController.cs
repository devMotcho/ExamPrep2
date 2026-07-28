using Auth.Api.Contracts;
using Auth.Application.Results;
using Auth.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/oauth")]
/// <summary>
/// Provides endpoints for handling third-party OAuth logins.
/// </summary>
public class OAuthController(IOAuthService oauthService, IConfiguration config) : ControllerBase
{
    /// <summary>
    /// Authenticates a user via a third-party OAuth provider token.
    /// </summary>
    /// <param name="provider">The name of the OAuth provider (e.g., google).</param>
    /// <param name="req">The OAuth login request containing the token.</param>
    /// <returns>An AuthResponse with the access token if successful.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid token or account could not be created.</response>
    [HttpPost("{provider}/login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(string provider, OAuthLoginRequest req)
    {
        var result = await oauthService.LoginAsync(provider, req.Token);

        switch (result.Status)
        {
            case LoginStatus.InvalidCredentials:
                return Unauthorized(new { message = $"Invalid token for provider '{provider}' or account could not be created." });

            case LoginStatus.Success:
                SetRefreshCookie(result.RawRefreshToken!);
                return Ok(new AuthResponse(result.AccessToken!));

            default:
                throw new InvalidOperationException($"Unhandled login status: {result.Status}");
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
        // In a real app, this could be dynamic, but for now we read from config
        var googleEnabled = config.GetValue<bool>("OAuthProviders:Google:Enabled");
        var googleClientId = config.GetValue<string>("OAuthProviders:Google:ClientId");

        return Ok(new
        {
            google = new
            {
                enabled = googleEnabled,
                clientId = googleEnabled ? googleClientId : null
            }
            // Add other providers here later (microsoft, apple, etc.)
        });
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
