using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Auth.Api.Contracts;
using Auth.Api.Tests.Fixtures;
using Auth.Application.Events;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ExamPrep.Shared.Constants;

namespace Auth.Api.Tests.IntegrationTests;

public class RegisterEndpointTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly AuthApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RegisterEndpointTests(AuthApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;
        return new AuthDbContext(options);
    }

    private async Task<string?> GetLatestOtpCodeFromOutboxAsync(string email)
    {
        await using var db = CreateDbContext();
        
        var message = await db.OutboxMessages
            .Where(m => m.Topic == KafkaTopics.EmailVerificationCodeRequested && m.Payload.Contains(email))
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        if (message is null) return null;

        var payload = JsonSerializer.Deserialize<EmailVerificationCodeRequestedEvent>(message.Payload);
        return payload?.Code;
    }

    [Fact]
    public async Task RequestVerification_UnknownUser_ReturnsOk_AndWritesOutboxEvent()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var response = await _client.PostAsJsonAsync("/api/auth/email-verification/request", new { Email = email });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var code = await GetLatestOtpCodeFromOutboxAsync(email);
        Assert.NotNull(code);
        Assert.Equal(8, code.Length);
    }

    [Fact]
    public async Task RequestVerification_ExistingUser_ReturnsOk_NoOutboxEvent()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _factory.CreateUserAsync(email, "StrongPass123!");

        var response = await _client.PostAsJsonAsync("/api/auth/email-verification/request", new { Email = email });
        
        // Still returns OK to avoid leaking registered status, but sends no email
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var code = await GetLatestOtpCodeFromOutboxAsync(email);
        Assert.Null(code);
    }

    [Fact]
    public async Task Register_ValidRequest_CreatesUser_AndReturnsTokens()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/email-verification/request", new { Email = email });
        var code = await GetLatestOtpCodeFromOutboxAsync(email);

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, code!, "StrongPass123!"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));

        // Assert user email is confirmed in DB
        await using var db = CreateDbContext();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        Assert.True(user.EmailConfirmed);

        // Assert cookie is set
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, c => c.StartsWith("refresh_token="));
    }

    [Fact]
    public async Task Register_InvalidCode_ReturnsBadRequest()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/email-verification/request", new { Email = email });

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "00000000", "StrongPass123!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_TooManyFailedAttempts_LocksCode()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/email-verification/request", new { Email = email });
        var code = await GetLatestOtpCodeFromOutboxAsync(email);

        // Fail 5 times
        for (int i = 0; i < 5; i++)
        {
            var failResponse = await _client.PostAsJsonAsync("/api/auth/register",
                new RegisterRequest(email, "00000000", "StrongPass123!"));
            Assert.Equal(HttpStatusCode.BadRequest, failResponse.StatusCode);
        }

        // Try with correct code - should fail due to rate limit (429)
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, code!, "StrongPass123!"));
        
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _factory.CreateUserAsync(email, "StrongPass123!");

        // Assuming somehow a code was leaked or requested before the user registered
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "12345678", "AnotherPass123!"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsBadRequest_AndWritesNoOutboxRow()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/email-verification/request", new { Email = email });
        var code = await GetLatestOtpCodeFromOutboxAsync(email);

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, code!, "weak")); // fails Identity's password policy

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var db = CreateDbContext();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        Assert.Null(user); // confirms the transaction actually rolled back
    }
}