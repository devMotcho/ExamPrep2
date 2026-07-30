namespace Auth.Api.Services;

public interface ICookieService
{
    void SetRefreshTokenCookie(HttpResponse response, string rawToken);

    /// <summary>
    /// Overwrites the refresh token cookie with an expired, empty value so the
    /// browser removes it immediately on receipt.
    /// </summary>
    void ExpireRefreshTokenCookie(HttpResponse response);
}