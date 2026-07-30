using System.Net;
using System.Net.Http.Json;
using Auth.Api.Contracts;
using Auth.Api.Tests.Fixtures;

namespace Auth.Api.Tests.IntegrationTests;

public class LoginEndpointTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly AuthApiWebApplicationFactory _factory;

    public LoginEndpointTests(AuthApiWebApplicationFactory factory)
    {
        _factory = factory;
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
        
        await _factory.CreateUserAsync(email, password);

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
        
        await _factory.CreateUserAsync(email, password);

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

    [Fact]
    public async Task Login_TooManyFailedAttempts_ReturnsTooManyRequests()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var password = "StrongPassword123!";
        
        await _factory.CreateUserAsync(email, password);

        // MaxFailedAccessAttempts is 5.
        // First 4 attempts return 401
        for (int i = 0; i < 4; i++)
        {
            var r = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword!"));
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        }

        // 5th attempt triggers lockout and returns 429
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword!"));
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        
        // 6th attempt with CORRECT password still returns 429 because account is locked
        var correctResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.TooManyRequests, correctResponse.StatusCode);
    }

    [Fact]
    public async Task Login_SuccessfulAttempt_ResetsFailedAttemptCount()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var password = "StrongPassword123!";
        
        await _factory.CreateUserAsync(email, password);

        // 4 failed attempts
        for (int i = 0; i < 4; i++)
        {
            await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword!"));
        }

        // Successful login should reset the count
        var successResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, successResponse.StatusCode);

        // Now 1 more failure should NOT lock the account (since count was reset)
        var failureResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword!"));
        Assert.Equal(HttpStatusCode.Unauthorized, failureResponse.StatusCode);
    }
}
