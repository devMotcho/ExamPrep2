using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Auth.Api.Contracts;
using Auth.Api.Tests.Fixtures;
using Auth.Application.Events;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Api.Tests.IntegrationTests;

public class PasswordResetEndpointTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly AuthApiWebApplicationFactory _factory;

    public PasswordResetEndpointTests(AuthApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }


    private async Task<string> RegisterUserAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _factory.CreateUserAsync(email, "OldPassword123!");
        return email;
    }

    private async Task<string?> GetLatestOtpCodeFromOutboxAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        // Find the user to get their ID
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user is null) return null;

        // Get the latest outbox message for this user for password reset
        var message = await db.OutboxMessages
            .Where(m => m.Topic == "password-reset-requested" && m.Key == user.Id)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        if (message is null) return null;

        var payload = JsonSerializer.Deserialize<PasswordResetRequestedEvent>(message.Payload);
        return payload?.Code;
    }


    [Fact]
    public async Task RequestReset_ExistingUser_ReturnsOk_AndWritesOutboxEvent()
    {
        var email = await RegisterUserAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/password-reset/request", new { Email = email });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify outbox message was written
        var code = await GetLatestOtpCodeFromOutboxAsync(email);
        Assert.NotNull(code);
        Assert.Equal(8, code.Length);
    }

    [Fact]
    public async Task RequestReset_UnknownUser_ReturnsOk_NoOutboxEvent()
    {
        var email = "nobody@example.com";

        var response = await _client.PostAsJsonAsync("/api/auth/password-reset/request", new { Email = email });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // Enumeration protection

        var code = await GetLatestOtpCodeFromOutboxAsync(email);
        Assert.Null(code);
    }

    [Fact]
    public async Task Verify_ValidCode_ReturnsOk_AndTicket()
    {
        var email = await RegisterUserAsync();
        await _client.PostAsJsonAsync("/api/auth/password-reset/request", new { Email = email });
        var code = await GetLatestOtpCodeFromOutboxAsync(email);

        var response = await _client.PostAsJsonAsync("/api/auth/password-reset/verify", new { Email = email, Code = code });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("resetTicket", out var ticketProp));
        Assert.False(string.IsNullOrWhiteSpace(ticketProp.GetString()));
    }

    [Fact]
    public async Task Verify_InvalidCode_ReturnsBadRequest()
    {
        var email = await RegisterUserAsync();
        await _client.PostAsJsonAsync("/api/auth/password-reset/request", new { Email = email });

        var response = await _client.PostAsJsonAsync("/api/auth/password-reset/verify", new { Email = email, Code = "00000000" }); // Wrong code

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Verify_TooManyFailedAttempts_LocksCode()
    {
        var email = await RegisterUserAsync();
        await _client.PostAsJsonAsync("/api/auth/password-reset/request", new { Email = email });
        var code = await GetLatestOtpCodeFromOutboxAsync(email);

        // Fail 5 times (MaxAttempts)
        for (int i = 0; i < 5; i++)
        {
            var failResponse = await _client.PostAsJsonAsync("/api/auth/password-reset/verify", new { Email = email, Code = "00000000" });
            Assert.Equal(HttpStatusCode.BadRequest, failResponse.StatusCode);
        }

        // Try with the correct code now — should still fail because it's locked
        var response = await _client.PostAsJsonAsync("/api/auth/password-reset/verify", new { Email = email, Code = code });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Too many failed attempts", body);
    }

    [Fact]
    public async Task Confirm_ValidTicket_ResetsPassword_AndRevokesSessions()
    {
        // Setup: register, login (to get a refresh token), request reset, verify
        var email = await RegisterUserAsync();
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "OldPassword123!"));
        var refreshToken = loginResponse.Headers.GetValues("Set-Cookie").First().Split(';')[0].Split('=')[1];

        await _client.PostAsJsonAsync("/api/auth/password-reset/request", new { Email = email });
        var code = await GetLatestOtpCodeFromOutboxAsync(email);
        
        var verifyResponse = await _client.PostAsJsonAsync("/api/auth/password-reset/verify", new { Email = email, Code = code });
        var verifyBody = await verifyResponse.Content.ReadFromJsonAsync<JsonElement>();
        var ticket = verifyBody.GetProperty("resetTicket").GetString();

        // Act: confirm the reset
        var newPassword = "NewStrongPassword123!";
        var confirmResponse = await _client.PostAsJsonAsync("/api/auth/password-reset/confirm", new { ResetTicket = ticket, NewPassword = newPassword });

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        // Assert 1: Old password no longer works
        var oldLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "OldPassword123!"));
        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);

        // Assert 2: New password works
        var newLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, newPassword));
        Assert.Equal(HttpStatusCode.OK, newLoginResponse.StatusCode);

        // Assert 3: Old refresh token is revoked
        var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshReq.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        var refreshResp = await _client.SendAsync(refreshReq);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResp.StatusCode);
    }

    [Fact]
    public async Task Confirm_InvalidTicket_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/password-reset/confirm", new { ResetTicket = "faketicket", NewPassword = "NewPassword123!" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
