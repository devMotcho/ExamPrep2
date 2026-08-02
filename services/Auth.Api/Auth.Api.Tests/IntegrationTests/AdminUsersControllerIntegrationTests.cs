using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Auth.Api.Contracts;
using Auth.Api.Tests.Fixtures;
using Auth.Domain.Rules;

namespace Auth.Api.Tests.IntegrationTests;

public class AdminUsersControllerIntegrationTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly AuthApiWebApplicationFactory _factory;

    public AdminUsersControllerIntegrationTests(AuthApiWebApplicationFactory factory)
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
    public async Task Search_ReturnsForbidden_WhenNotAdmin()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var token = await AuthenticateAsync(email, "Password123!", Roles.Student); // Just student

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Search_ReturnsOk_WhenAdmin()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var token = await AuthenticateAsync(email, "Password123!", Roles.Admin);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AssignRole_AssignsRole_WhenAdmin()
    {
        var adminEmail = $"{Guid.NewGuid()}@example.com";
        var token = await AuthenticateAsync(adminEmail, "Password123!", Roles.Admin);

        var targetEmail = $"{Guid.NewGuid()}@example.com";
        var targetUser = await _factory.CreateUserAsync(targetEmail, "Password123!");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var response = await _client.PostAsync($"/api/admin/users/{targetUser.Id}/roles/{Roles.Promoter}", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
