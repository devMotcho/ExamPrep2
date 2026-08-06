using Auth.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

public class PartnerTransactionConfiguration : IEntityTypeConfiguration<PartnerTransaction>
{
    public void Configure(EntityTypeBuilder<PartnerTransaction> builder)
    {
        builder.ToTable("PartnerTransactions");

        builder.HasKey(pt => pt.Id);
        
        builder.Property(pt => pt.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(pt => pt.Description)
            .HasMaxLength(255);

        builder.HasOne(pt => pt.Partner)
            .WithMany(u => u.PartnerTransactions)
            .HasForeignKey(pt => pt.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
