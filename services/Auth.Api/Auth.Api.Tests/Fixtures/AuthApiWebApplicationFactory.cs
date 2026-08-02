using Auth.Infrastructure.Identity;
using Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Auth.Api.Tests.Fixtures;

public class AuthApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("authdb_test")
        .WithUsername("app")
        .WithPassword("app")
        .Build();

    private string _privateKeyPath = null!;
    private string _publicKeyPath = null!;

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        (_privateKeyPath, _publicKeyPath) = TestRsaKeys.GenerateTempKeyPair();

        // Apply real migrations against the fresh container - proves they work end to end
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        await using var db = new AuthDbContext(options);
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        TestRsaKeys.Cleanup(_privateKeyPath, _publicKeyPath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AuthDb"] = _postgres.GetConnectionString(),
                ["Jwt:Issuer"] = "examprep-auth-test",
                ["Jwt:Audience"] = "examprep-test",
                ["Jwt:PrivateKeyPath"] = _privateKeyPath,
                ["Jwt:PublicKeyPath"] = _publicKeyPath
            });
        });
    }

    public async Task ManuallyVerifyEmailAsync(string email)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            await userManager.ConfirmEmailAsync(user, token);
        }
    }

    public async Task<User> CreateUserAsync(string email, string password, params string[] roles)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = new User { UserName = email, Email = email, EmailConfirmed = true };
        await userManager.CreateAsync(user, password);
        
        foreach (var role in roles)
        {
            await userManager.AddToRoleAsync(user, role);
        }
        return user;
    }

    public async Task<User> CreateUnverifiedUserAsync(string email, string password)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = new User { UserName = email, Email = email, EmailConfirmed = false };
        await userManager.CreateAsync(user, password);
        return user;
    }
}