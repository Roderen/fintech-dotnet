using Domain.TopUps;
using Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

public class TopUpConfiguration : IEntityTypeConfiguration<TopUp>
{
    public void Configure(EntityTypeBuilder<TopUp> builder)
    {
        builder.ToTable("top_ups");
        
        builder.HasKey(w => w.Id);

        builder.HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(t => t.Amount)
            .HasColumnType("numeric(28,10)")
            .IsRequired();
        
        builder.Property(w => w.Currency)
            .HasMaxLength(3)
            .IsRequired();
        
        builder.Property(w => w.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Status).IsConcurrencyToken();
        builder.HasIndex(t => t.StripePaymentIntentId).IsUnique();

        builder.HasIndex(t => t.StripeSessionId)
            .IsUnique();
    }
}