using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Auth.Api.Tests.IntegrationTests;

public class MigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("authdb_migration_test")
        .WithUsername("app")
        .WithPassword("app")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Migrations_ApplyCleanly_ToFreshDatabase()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using var db = new AuthDbContext(options);

        await db.Database.MigrateAsync(); // fails the test if any migration errors

        var tableNames = await db.Database
            .SqlQuery<string>($"SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public'")
            .ToListAsync();

        Assert.Contains("OutboxMessages", tableNames);
        Assert.Contains("RefreshTokens", tableNames);
        Assert.Contains("AspNetUsers", tableNames);
    }
}