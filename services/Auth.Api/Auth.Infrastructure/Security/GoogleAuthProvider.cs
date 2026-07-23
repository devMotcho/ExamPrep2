using Auth.Domain.ValueObjects;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace Auth.Infrastructure.Security;

public class GoogleAuthProvider(IConfiguration config) 
    : IExternalAuthProvider
{
    public string ProviderName => "Google";

    public async Task<ExternalUserInfo> ValidateAsync(string providerToken)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [config["Authentication:Google:ClientId"]]
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(providerToken, settings);

        return new ExternalUserInfo(payload.Subject, payload.Email, payload.Name);
    }
}