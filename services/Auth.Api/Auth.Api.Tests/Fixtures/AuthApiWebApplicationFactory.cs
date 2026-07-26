using Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.TestHost;
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
}