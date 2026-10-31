using Domain.Transactions;
using Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");
        
        builder.HasKey(w => w.Id);
        
        builder.Property(t => t.Amount)
            .HasColumnType("numeric(28,10)")
            .IsRequired();
        
        builder.Property(w => w.Currency)
            .HasMaxLength(3)
            .IsRequired();
        
        builder.Property(t => t.Type) 
            .HasMaxLength(20)
            .IsRequired();
        
        builder.HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(t => t.SourceWalletId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(t => t.IdempotencyKey)
            .IsUnique(); 
    }
}