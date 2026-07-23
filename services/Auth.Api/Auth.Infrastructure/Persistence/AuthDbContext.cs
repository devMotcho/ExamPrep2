namespace Auth.Infrastructure.Persistence;

using Auth.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) 
            : IdentityDbContext<User>(options) 
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>()
            .HasOne(u => u.RefreshToken)
            .WithOne(rt => rt.User)
            .HasForeignKey<RefreshToken>(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<RefreshToken>()
            .HasIndex(rt => rt.TokenHash)
            .IsUnique();
    }
}
