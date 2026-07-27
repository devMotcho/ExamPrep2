using System.Net;
using System.Net.Http.Json;
using Auth.Api.Contracts;
using Auth.Api.Tests.Fixtures;

namespace Auth.Api.Tests.IntegrationTests;

public class RefreshEndpointTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RefreshEndpointTests(AuthApiWebApplicationFactory factory)
    {
        // UseCookies = true so the HttpClient automatically stores and resends the
        // Set-Cookie header from /register when calling /refresh.
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    /// <summary>Extracts the raw value of a Set-Cookie entry by name.</summary>
    private static string? ExtractCookieValue(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;

        foreach (var cookie in cookies)
        {
            var part = cookie.Split(';')[0]; // "name=value"
            var eq = part.IndexOf('=');
            if (eq > 0 && part[..eq].Trim() == name)
                return part[(eq + 1)..].Trim();
        }
        return null;
    }

    [Fact]
    public async Task Refresh_ValidCookie_ReturnsOkWithNewAccessToken()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "StrongPass123!"));
        var rawToken = ExtractCookieValue(registerResponse, "refresh_token");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refresh_token={rawToken}");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
    }

    [Fact]
    public async Task Refresh_ValidCookie_RotatesRefreshToken()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "StrongPass123!"));
        var originalToken = ExtractCookieValue(registerResponse, "refresh_token");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refresh_token={originalToken}");
        var refreshResponse = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var newToken = ExtractCookieValue(refreshResponse, "refresh_token");
        Assert.NotNull(newToken);
        Assert.NotEqual(originalToken, newToken); // token was rotated
    }

    [Fact]
    public async Task Refresh_ConsumedToken_ReturnsUnauthorized()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "StrongPass123!"));
        var originalToken = ExtractCookieValue(registerResponse, "refresh_token");

        // First use — should succeed and rotate
        var first = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        first.Headers.Add("Cookie", $"refresh_token={originalToken}");
        await _client.SendAsync(first);

        // Second use of the same token — must be rejected (token rotation)
        var second = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        second.Headers.Add("Cookie", $"refresh_token={originalToken}");
        var secondResponse = await _client.SendAsync(second);

        Assert.Equal(HttpStatusCode.Unauthorized, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_NoCookie_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", "refresh_token=completelyfaketoken");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
