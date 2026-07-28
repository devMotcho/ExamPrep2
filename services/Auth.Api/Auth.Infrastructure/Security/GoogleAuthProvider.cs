#nullable enable
using Auth.Application.Interfaces;
using Auth.Application.Models;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace Auth.Infrastructure.Security;

public class GoogleAuthProvider(IConfiguration config) : IExternalAuthProvider
{
    public string ProviderName => "google";

    public async Task<ExternalUserInfo?> ValidateTokenAsync(string token)
    {
        try
        {
            var clientId = config["OAuthProviders:Google:ClientId"];
            bool.TryParse(config["OAuthProviders:Google:Enabled"], out var enabled);

            if (!enabled || string.IsNullOrEmpty(clientId))
                return null;

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(token, settings);
            
            if (payload == null)
                return null;

            return new ExternalUserInfo(payload.Email, payload.Name, ProviderName, payload.Subject);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}