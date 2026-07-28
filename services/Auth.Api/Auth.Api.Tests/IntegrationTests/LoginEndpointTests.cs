using System.Net;
using System.Net.Http.Json;
using Auth.Api.Contracts;
using Auth.Api.Tests.Fixtures;

namespace Auth.Api.Tests.IntegrationTests;

public class LoginEndpointTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LoginEndpointTests(AuthApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
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

    [Fact]
    public async Task Login_ValidEmailCredentials_ReturnsOkAndTokens()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var password = "StrongPassword123!";
        
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));

        var refreshToken = ExtractCookieValue(response, "refresh_token");
        Assert.NotNull(refreshToken);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var password = "StrongPassword123!";
        
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("unknown@example.com", "Password123!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingFields_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { EmailOrUsername = "test@example.com" }); // Missing password

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
