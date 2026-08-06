using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Auth.Api.Contracts;
using Auth.Api.Tests.Fixtures;

namespace Auth.Api.Tests.IntegrationTests;

public class LogoutEndpointTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly AuthApiWebApplicationFactory _factory;

    public LogoutEndpointTests(AuthApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>Registers a new unique user, verifies their email, logs them in, and returns their access token + raw refresh token.</summary>
    private async Task<(string AccessToken, string RefreshToken)> RegisterAndGetTokensAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var password = "StrongPass123!";
        await _factory.CreateUserAsync(email, password);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();

        var body = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var refreshToken = ExtractCookieValue(loginResponse, "refresh_token")!;
        return (body!.AccessToken, refreshToken);
    }

    private static string? ExtractCookieValue(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;

        foreach (var cookie in cookies)
        {
            var part = cookie.Split(';')[0];
            var eq = part.IndexOf('=');
            if (eq > 0 && part[..eq].Trim() == name)
                return part[(eq + 1)..].Trim();
        }
        return null;
    }

    private static HttpRequestMessage BuildLogoutRequest(string accessToken, string? refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (refreshToken is not null)
            request.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        return request;
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_WithValidAccessTokenAndCookie_ReturnsNoContent()
    {
        var (accessToken, refreshToken) = await RegisterAndGetTokensAsync();

        var response = await _client.SendAsync(BuildLogoutRequest(accessToken, refreshToken));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ClearsRefreshTokenCookie()
    {
        var (accessToken, refreshToken) = await RegisterAndGetTokensAsync();

        var response = await _client.SendAsync(BuildLogoutRequest(accessToken, refreshToken));

        // The server must set Set-Cookie: refresh_token=; Expires=<past>
        var cookieValue = ExtractCookieValue(response, "refresh_token");
        Assert.NotNull(cookieValue);
        Assert.Empty(cookieValue); // value wiped to empty string
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken_SubsequentRefreshIsUnauthorized()
    {
        var (accessToken, refreshToken) = await RegisterAndGetTokensAsync();

        // Logout — revokes the token
        var logoutResponse = await _client.SendAsync(BuildLogoutRequest(accessToken, refreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        // Try to use the now-revoked token
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        var refreshResponse = await _client.SendAsync(refreshRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutAccessToken_ReturnsUnauthorized()
    {
        // No Authorization header — [Authorize] must block the request
        var response = await _client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidAccessToken_ButNoCookie_ReturnsNoContent()
    {
        // Cookie may already be gone (e.g. browser cleared storage).
        // The endpoint must still succeed — the session is effectively ended
        // from the server's perspective once the access token expires.
        var (accessToken, _) = await RegisterAndGetTokensAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        // deliberately no Cookie header

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithAlreadyRevokedToken_ReturnsUnauthorized()
    {
        // Because we now have a Redis access token blocklist, the second call
        // will be rejected at the Authentication middleware level.
        var (accessToken, refreshToken) = await RegisterAndGetTokensAsync();

        await _client.SendAsync(BuildLogoutRequest(accessToken, refreshToken));

        // Second call — access token is blocked, so middleware returns 401
        var secondResponse = await _client.SendAsync(BuildLogoutRequest(accessToken, refreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, secondResponse.StatusCode);
    }
}
