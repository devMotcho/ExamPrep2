using Auth.Application.Interfaces;
using Auth.Application.Models;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Security;

public class GoogleAuthProvider(IConfiguration config, ILogger<GoogleAuthProvider> logger) : IExternalAuthProvider
{
    public string ProviderName => "google";

    public async Task<ExternalUserInfo?> ValidateTokenAsync(string token)
    {
        try
        {
            var clientId = config["OAuthProviders:Google:ClientId"];
            var result = bool.TryParse(config["OAuthProviders:Google:Enabled"], out var enabled);
            if (!result) enabled = false;

            if (!enabled || string.IsNullOrEmpty(clientId))
            {
                logger.LogWarning("Google OAuth is disabled or ClientId is missing.");
                return null;
            }

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [clientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(token, settings);
            
            if (payload == null)
            {
                logger.LogWarning("Google token payload was null after validation.");
                return null;
            }

            return new ExternalUserInfo(payload.Email, payload.Name, ProviderName, payload.Subject, payload.EmailVerified);
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning(ex, "Google token validation failed: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error validating Google token.");
            return null;
        }
    }
}