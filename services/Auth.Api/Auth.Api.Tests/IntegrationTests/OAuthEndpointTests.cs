using System.Net;
using System.Net.Http.Json;
using Auth.Api.Contracts;
using Auth.Api.Tests.Fixtures;
using Auth.Application.Interfaces;
using Auth.Application.Models;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Auth.Api.Tests.IntegrationTests;

public class FakeExternalAuthProvider : IExternalAuthProvider
{
    public string ProviderName => "fake";

    public Task<ExternalUserInfo?> ValidateTokenAsync(string token)
    {
        if (token == "valid-token")
        {
            return Task.FromResult<ExternalUserInfo?>(new ExternalUserInfo(
                "oauth-user@example.com",
                "OAuth User",
                ProviderName,
                "fake-id-123",
                true
            ));
        }

        if (token == "collision-token")
        {
            return Task.FromResult<ExternalUserInfo?>(new ExternalUserInfo(
                "collision@example.com",
                "Collision User",
                ProviderName,
                "collision-id-456",
                true
            ));
        }

        if (token == "lockout-token")
        {
            return Task.FromResult<ExternalUserInfo?>(new ExternalUserInfo(
                "lockout@example.com",
                "Lockout User",
                ProviderName,
                "lockout-id-789",
                true
            ));
        }

        return Task.FromResult<ExternalUserInfo?>(null);
    }
}

public class OAuthEndpointTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public OAuthEndpointTests(AuthApiWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove real providers and add the fake one
                services.RemoveAll<IExternalAuthProvider>();
                services.AddSingleton<IExternalAuthProvider, FakeExternalAuthProvider>();
            });
        });

        _client = _factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Login_ValidOAuthToken_CreatesUserAndReturnsJwt()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/oauth/fake/login", new OAuthLoginRequest("valid-token"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, c => c.StartsWith("refresh_token="));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "oauth-user@example.com");
        
        Assert.NotNull(user);
        Assert.True(user.EmailConfirmed, "OAuth users should have their email confirmed automatically.");
    }

    [Fact]
    public async Task Login_InvalidOAuthToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/oauth/fake/login", new OAuthLoginRequest("invalid-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownProvider_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/oauth/unknown/login", new OAuthLoginRequest("valid-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProviders_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/auth/oauth/providers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        
        Assert.Contains("google", body);
    }

    [Fact]
    public async Task Login_ExistingEmail_ReturnsAccountLinkRequired()
    {
        // 1. Create the pre-existing user
        using var scope = _factory.Services.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var createResult = await userRepo.CreateAsync("collision@example.com", "Password123!");
        Assert.True(createResult.Succeeded);
        
        // 2. Attempt OAuth login
        var response = await _client.PostAsJsonAsync("/api/auth/oauth/fake/login", new OAuthLoginRequest("collision-token"));
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Should not contain tokens, but rather the link requirement
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("linkRequired").GetBoolean());
        Assert.NotEmpty(body.GetProperty("linkTicket").GetString()!);
        Assert.Equal("co***@example.com", body.GetProperty("maskedEmail").GetString());
    }

    [Fact]
    public async Task ConfirmLink_ValidTicketAndPassword_LinksAccountAndReturnsTokens()
    {
        // 1. Setup existing user and trigger collision
        using var scope = _factory.Services.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        await userRepo.CreateAsync("collision@example.com", "Password123!");
        
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/oauth/fake/login", new OAuthLoginRequest("collision-token"));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var ticket = loginBody.GetProperty("linkTicket").GetString()!;

        // 2. Confirm link with WRONG password
        var badPassResponse = await _client.PostAsJsonAsync("/api/auth/oauth/link/confirm", new ConfirmLinkRequest(ticket, "WrongPassword123!"));
        Assert.Equal(HttpStatusCode.Unauthorized, badPassResponse.StatusCode);

        // 3. Confirm link with WRONG ticket
        var badTicketResponse = await _client.PostAsJsonAsync("/api/auth/oauth/link/confirm", new ConfirmLinkRequest("invalid-ticket", "Password123!"));
        Assert.Equal(HttpStatusCode.BadRequest, badTicketResponse.StatusCode);

        // 4. Confirm link successfully
        var successResponse = await _client.PostAsJsonAsync("/api/auth/oauth/link/confirm", new ConfirmLinkRequest(ticket, "Password123!"));
        Assert.Equal(HttpStatusCode.OK, successResponse.StatusCode);
        
        var successBody = await successResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(successBody);
        Assert.False(string.IsNullOrWhiteSpace(successBody.AccessToken));

        // Verify the login was added
        var linkedUser = await userRepo.FindByLoginAsync("fake", "collision-id-456");
        Assert.NotNull(linkedUser);
        Assert.Equal("collision@example.com", linkedUser.Email);
    }

    [Fact]
    public async Task ConfirmLink_TooManyFailedAttempts_LocksOutTicketAndAccount()
    {
        // 1. Setup existing user and trigger collision
        using var scope = _factory.Services.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        
        await userRepo.CreateAsync("lockout@example.com", "Password123!");
        
        // Use a new token for this test to avoid collision with others
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/oauth/fake/login", new OAuthLoginRequest("lockout-token"));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var ticket = loginBody.GetProperty("linkTicket").GetString()!;

        // 2. Fail exactly 5 times (MaxFailedAccessAttempts)
        for (int i = 0; i < 5; i++)
        {
            var failResponse = await _client.PostAsJsonAsync("/api/auth/oauth/link/confirm", new ConfirmLinkRequest(ticket, "WrongPassword123!"));
            Assert.Equal(HttpStatusCode.Unauthorized, failResponse.StatusCode);
        }

        // 3. The ticket and account should now be locked. A 6th attempt with the CORRECT password should fail.
        var lockedResponse = await _client.PostAsJsonAsync("/api/auth/oauth/link/confirm", new ConfirmLinkRequest(ticket, "Password123!"));
        Assert.Equal(HttpStatusCode.TooManyRequests, lockedResponse.StatusCode);

        // Verify Identity account lockout
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var dbUser = await db.Users.AsNoTracking().FirstAsync(u => u.Email == "lockout@example.com");
        
        Assert.True(dbUser.LockoutEnd > DateTimeOffset.UtcNow, 
            $"FailedCount: {dbUser.AccessFailedCount}, LockoutEnabled: {dbUser.LockoutEnabled}, LockoutEnd: {dbUser.LockoutEnd}");
    }
}
