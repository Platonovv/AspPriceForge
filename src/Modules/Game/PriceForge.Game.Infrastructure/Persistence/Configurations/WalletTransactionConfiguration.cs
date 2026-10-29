using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceForge.Game.Domain.Wallets;

namespace PriceForge.Game.Infrastructure.Persistence.Configurations;

internal sealed class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("wallet_transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(t => t.UserId).HasColumnName("user_id");
        builder.Property(t => t.Amount).HasColumnName("amount");
        builder.Property(t => t.Reason).HasColumnName("reason").HasConversion<string>().HasMaxLength(32);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(t => new {t.UserId, t.CreatedAt});
    }
}
