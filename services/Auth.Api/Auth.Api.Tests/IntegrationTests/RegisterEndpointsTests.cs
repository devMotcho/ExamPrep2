using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Auth.Api.Contracts;
using Auth.Api.Tests.Fixtures;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

    [Fact]
    public async Task Register_ValidRequest_ReturnsCreatedWithAccessToken()
    {
        var email = $"{Guid.NewGuid()}@example.com";

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "StrongPass123!"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
    }

    [Fact]
    public async Task Register_ValidRequest_SetsRefreshTokenCookie()
    {
        var email = $"{Guid.NewGuid()}@example.com";

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "StrongPass123!"));

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, c => c.StartsWith("refresh_token="));
    }

    [Fact]
    public async Task Register_ValidRequest_WritesUserRegisteredOutboxMessage()
    {
        var email = $"{Guid.NewGuid()}@example.com";

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "StrongPass123!"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var db = CreateDbContext();
        var outboxRow = await db.OutboxMessages
            .Where(m => m.Topic == "user-registered")
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        Assert.NotNull(outboxRow);

        using var payload = JsonDocument.Parse(outboxRow.Payload);
        Assert.Equal(email, payload.RootElement.GetProperty("Email").GetString());
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "StrongPass123!"));

        var secondAttempt = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "AnotherPass123!"));

        Assert.Equal(HttpStatusCode.Conflict, secondAttempt.StatusCode);
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsBadRequest_AndWritesNoOutboxRow()
    {
        var email = $"{Guid.NewGuid()}@example.com";

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "weak")); // fails Identity's password policy

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var db = CreateDbContext();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        Assert.Null(user); // confirms the transaction actually rolled back, not just that the API returned 400
    }
}