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
                "fake-id-123"
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
        var response = await _client.PostAsJsonAsync("/api/oauth/fake/login", new OAuthLoginRequest("valid-token"));

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
        var response = await _client.PostAsJsonAsync("/api/oauth/fake/login", new OAuthLoginRequest("invalid-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownProvider_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/oauth/unknown/login", new OAuthLoginRequest("valid-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProviders_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/oauth/providers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        
        Assert.Contains("google", body);
    }
}
