using Auth.Api.Constants;

using Auth.Domain.Rules;

namespace Auth.Api.Services;

public class CookieService : ICookieService
{
    public void ExpireRefreshTokenCookie(HttpResponse response) =>
        response.Cookies.Append(CookieNames.RefreshToken,string.Empty, 
            BuildOptions(DateTimeOffset.UnixEpoch)); // past date forces immediate removal

    public void SetRefreshTokenCookie(HttpResponse response, string rawToken) =>
        response.Cookies.Append(CookieNames.RefreshToken, rawToken,
            BuildOptions(DateTimeOffset.UtcNow.Add(AuthLifetimes.RefreshTokenLifetime)));
    

    private static CookieOptions BuildOptions(DateTimeOffset expires) =>
        new ()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expires
        };
}