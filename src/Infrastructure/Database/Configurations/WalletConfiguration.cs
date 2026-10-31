using Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets");
        
        builder.HasKey(w => w.Id);
        
        builder.Property(w => w.Balance)
            .IsConcurrencyToken()
            .HasColumnType("decimal(28, 10)")
            .IsRequired();
        
        builder.Property(w => w.Currency)
            .HasMaxLength(3)
            .IsRequired();
        
        builder.HasIndex(w => new { w.UserId, w.Currency })
            .IsUnique();
    }
}