using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Auth.Api.Contracts;
using Auth.Api.Tests.Fixtures;
using Auth.Domain.Rules;
using Auth.Application.Interfaces;

namespace Auth.Api.Tests.IntegrationTests;

public class PartnerControllerIntegrationTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly AuthApiWebApplicationFactory _factory;

    public PartnerControllerIntegrationTests(AuthApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    private async Task<string> AuthenticateAsync(string email, string password, params string[] roles)
    {
        await _factory.CreateUserAsync(email, password, roles);
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task GetMyInfo_ReturnsForbidden_WhenNotPartner()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var token = await AuthenticateAsync(email, "Password123!", Roles.Student);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/partners/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyInfo_ReturnsOk_WhenPartner()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var token = await AuthenticateAsync(email, "Password123!", Roles.Partner);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/partners/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var info = await response.Content.ReadFromJsonAsync<PartnerInfoDto>();
        Assert.NotNull(info);
        Assert.Equal(email, info.PartnerEmail);
        Assert.Equal(0m, info.Balance);
    }

    [Fact]
    public async Task SubtractBalance_ReturnsForbidden_WhenNotAdmin()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var token = await AuthenticateAsync(email, "Password123!", Roles.Partner);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var request = new { Amount = 10m, Description = "Test" };
        var response = await _client.PostAsJsonAsync($"/api/partners/{Guid.NewGuid()}/subtract-balance", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
