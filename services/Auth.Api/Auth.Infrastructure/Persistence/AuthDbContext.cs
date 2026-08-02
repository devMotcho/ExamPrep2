using Auth.Infrastructure.Identity;
using Auth.Infrastructure.Persistence.Outbox;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

public class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : IdentityDbContext<User>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();
    public DbSet<PasswordResetTicket> PasswordResetTickets => Set<PasswordResetTicket>();
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();
    public DbSet<PendingOAuthLink> PendingOAuthLinks => Set<PendingOAuthLink>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
