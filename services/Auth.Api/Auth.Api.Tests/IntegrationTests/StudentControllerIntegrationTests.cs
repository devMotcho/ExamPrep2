using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Auth.Api.Contracts;
using Auth.Api.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
namespace Auth.Api.Tests.IntegrationTests;

public class StudentControllerIntegrationTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly AuthApiWebApplicationFactory _factory;

    public StudentControllerIntegrationTests(AuthApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    private async Task<string> AuthenticateAsync(string email, string password)
    {
        await _factory.CreateUserAsync(email, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task GetProfile_ReturnsOk_WithValidToken()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var token = await AuthenticateAsync(email, "Password123!");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal(email, profile.Email);
        Assert.Null(profile.FirstName); // defaults to null
    }

    [Fact]
    public async Task UpdateProfile_UpdatesFields_ReturnsNoContent()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var token = await AuthenticateAsync(email, "Password123!");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var updateRequest = new UpdateProfileRequest("Jane", "Doe", null);
        var response = await _client.PatchAsJsonAsync("/api/me", updateRequest);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it was updated
        var getResponse = await _client.GetAsync("/api/me");
        var profile = await getResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal("Jane", profile!.FirstName);
        Assert.Equal("Doe", profile.LastName);
    }

    [Fact]
    public async Task Deactivate_DeactivatesUser_ReturnsNoContent()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var token = await AuthenticateAsync(email, "Password123!");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // Deactivate
        var response = await _client.DeleteAsync("/api/me");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Now trying to login should fail because account is locked out (returns 429)
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password123!"));
        Assert.Equal(HttpStatusCode.TooManyRequests, loginResponse.StatusCode);
    }

    private async Task<string?> GetLatestOtpCodeFromOutboxAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Auth.Infrastructure.Persistence.AuthDbContext>();

        var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(db.Users, u => u.Email == email);
        if (user is null) return null;

        var message = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            System.Linq.Queryable.OrderByDescending(
                System.Linq.Queryable.Where(db.OutboxMessages, m => m.Topic == ExamPrep.Shared.Constants.KafkaTopics.PasswordChangeCodeRequested && m.Key == user.Id),
                m => m.CreatedAt));

        if (message is null) return null;

        var payload = System.Text.Json.JsonSerializer.Deserialize<Auth.Application.Events.PasswordChangeCodeRequestedEvent>(message.Payload);
        return payload?.Code;
    }

    [Fact]
    public async Task ChangePassword_WithValidCode_ChangesPassword_ReturnsNoContent()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var token = await AuthenticateAsync(email, "OldPassword123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Request code
        var requestResponse = await _client.PostAsync("/api/me/change-password/request", null);
        Assert.Equal(HttpStatusCode.OK, requestResponse.StatusCode);

        var code = await GetLatestOtpCodeFromOutboxAsync(email);
        Assert.NotNull(code);

        // Change password
        var changeReq = new ChangePasswordRequest("OldPassword123!", "NewPassword123!", code);
        var changeResponse = await _client.PostAsJsonAsync("/api/me/change-password", changeReq);
        Assert.Equal(HttpStatusCode.NoContent, changeResponse.StatusCode);

        // Verify old password fails
        var oldLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "OldPassword123!"));
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        // Verify new password succeeds
        var newLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "NewPassword123!"));
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithInvalidCode_ReturnsBadRequest()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var token = await AuthenticateAsync(email, "OldPassword123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync("/api/me/change-password/request", null);

        var changeReq = new ChangePasswordRequest("OldPassword123!", "NewPassword123!", "00000000");
        var changeResponse = await _client.PostAsJsonAsync("/api/me/change-password", changeReq);
        
        Assert.Equal(HttpStatusCode.BadRequest, changeResponse.StatusCode);
    }
}
