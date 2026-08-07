using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Auth.Api.Contracts;
using Auth.Api.Tests.Fixtures;
using Auth.Application.Events;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Tests.IntegrationTests;

public class VerifyEmailEndpointTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly AuthApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public VerifyEmailEndpointTests(AuthApiWebApplicationFactory factory)
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
            .Where(m => m.Topic == ExamPrep.Shared.Constants.KafkaTopics.EmailVerificationCodeRequested && m.Payload.Contains(email))
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        if (message is null) return null;

        var payload = JsonSerializer.Deserialize<EmailVerificationCodeRequestedEvent>(message.Payload);
        return payload?.Code;
    }

    [Fact]
    public async Task RequestVerification_AlreadyVerified_ReturnsOk_WithSpecificMessage()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        // Create confirmed user
        await _factory.CreateUserAsync(email, "StrongPass123!");

        var response = await _client.PostAsJsonAsync("/api/auth/email-verification/request", new { Email = email });
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Email is already verified", body);

        var code = await GetLatestOtpCodeFromOutboxAsync(email);
        Assert.Null(code); // Code should NOT be sent
    }

    [Fact]
    public async Task RequestVerification_UnverifiedUser_ReturnsOk_SendsCode()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        // Create unconfirmed user
        await _factory.CreateUnverifiedUserAsync(email, "StrongPass123!");

        var response = await _client.PostAsJsonAsync("/api/auth/email-verification/request", new { Email = email });
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var code = await GetLatestOtpCodeFromOutboxAsync(email);
        Assert.NotNull(code); // Code SHOULD be sent
    }

    [Fact]
    public async Task VerifyEmail_ValidCode_ConfirmsEmail()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _factory.CreateUnverifiedUserAsync(email, "StrongPass123!");

        // Request code
        await _client.PostAsJsonAsync("/api/auth/email-verification/request", new { Email = email });
        var code = await GetLatestOtpCodeFromOutboxAsync(email);

        // Verify code
        var response = await _client.PostAsJsonAsync("/api/auth/email-verification/verify", 
            new VerifyEmailVerificationRequest(email, code!));

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, got {response.StatusCode}. Body: {responseBody}");

        // Ensure user is confirmed
        await using var db = CreateDbContext();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public async Task VerifyEmail_InvalidCode_ReturnsBadRequest()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _factory.CreateUnverifiedUserAsync(email, "StrongPass123!");

        // Request code
        await _client.PostAsJsonAsync("/api/auth/email-verification/request", new { Email = email });

        // Verify with wrong code
        var response = await _client.PostAsJsonAsync("/api/auth/email-verification/verify", 
            new VerifyEmailVerificationRequest(email, "00000000"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Ensure user is STILL unconfirmed
        await using var db = CreateDbContext();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public async Task VerifyEmail_AlreadyVerified_ReturnsOk()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        // User is already verified!
        await _factory.CreateUserAsync(email, "StrongPass123!");

        var response = await _client.PostAsJsonAsync("/api/auth/email-verification/verify", 
            new VerifyEmailVerificationRequest(email, "12345678"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Email is already verified", body);
    }
}
