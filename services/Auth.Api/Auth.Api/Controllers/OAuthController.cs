using Auth.Api.Contracts;
using Auth.Application.Results;
using Auth.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/oauth")]
public class OAuthController(IOAuthService oauthService, IConfiguration config) : ControllerBase
{
    [HttpPost("{provider}/login")]
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

    [HttpGet("providers")]
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
