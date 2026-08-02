using Auth.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

public class PasswordResetTicketConfiguration : IEntityTypeConfiguration<PasswordResetTicket>
{
    public void Configure(EntityTypeBuilder<PasswordResetTicket> builder)
    {
        builder.HasIndex(t => t.TicketHash).IsUnique();
    }
}
