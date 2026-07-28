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

public class EmailVerificationEndpointTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly AuthApiWebApplicationFactory _factory;

    public EmailVerificationEndpointTests(AuthApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    private async Task<string> RegisterUserAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!"));
        response.EnsureSuccessStatusCode();
        return email;
    }

    private async Task<string?> GetLatestOtpCodeFromOutboxAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user is null) return null;

        var message = await db.OutboxMessages
            .Where(m => m.Topic == "email-verification-requested" && m.Key == user.Id)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        if (message is null) return null;

        var payload = JsonSerializer.Deserialize<EmailVerificationRequestedEvent>(message.Payload);
        return payload?.Code;
    }

    [Fact]
    public async Task RequestVerification_ExistingUser_ReturnsOk_AndWritesOutboxEvent()
    {
        var email = await RegisterUserAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/email-verification/request", new { Email = email });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var code = await GetLatestOtpCodeFromOutboxAsync(email);
        Assert.NotNull(code);
        Assert.Equal(8, code.Length);
    }

    [Fact]
    public async Task RequestVerification_UnknownUser_ReturnsOk_NoOutboxEvent()
    {
        var email = "nobody-verify@example.com";

        var response = await _client.PostAsJsonAsync("/api/auth/email-verification/request", new { Email = email });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); 

        var code = await GetLatestOtpCodeFromOutboxAsync(email);
        Assert.Null(code);
    }

    [Fact]
    public async Task Verify_ValidCode_ReturnsOk()
    {
        var email = await RegisterUserAsync();
        await _client.PostAsJsonAsync("/api/auth/email-verification/request", new { Email = email });
        var code = await GetLatestOtpCodeFromOutboxAsync(email);

        var response = await _client.PostAsJsonAsync("/api/auth/email-verification/verify", new { Email = email, Code = code });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Assert user email is confirmed in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public async Task Verify_InvalidCode_ReturnsBadRequest()
    {
        var email = await RegisterUserAsync();
        await _client.PostAsJsonAsync("/api/auth/email-verification/request", new { Email = email });

        var response = await _client.PostAsJsonAsync("/api/auth/email-verification/verify", new { Email = email, Code = "00000000" }); 

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Verify_TooManyFailedAttempts_LocksCode()
    {
        var email = await RegisterUserAsync();
        await _client.PostAsJsonAsync("/api/auth/email-verification/request", new { Email = email });
        var code = await GetLatestOtpCodeFromOutboxAsync(email);

        // Fail 5 times
        for (int i = 0; i < 5; i++)
        {
            var failResponse = await _client.PostAsJsonAsync("/api/auth/email-verification/verify", new { Email = email, Code = "00000000" });
            Assert.Equal(HttpStatusCode.BadRequest, failResponse.StatusCode);
        }

        // Try with correct code - should fail
        var response = await _client.PostAsJsonAsync("/api/auth/email-verification/verify", new { Email = email, Code = code });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Too many failed attempts", body);
    }
}
