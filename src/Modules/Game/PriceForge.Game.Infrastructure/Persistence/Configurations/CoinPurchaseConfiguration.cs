using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceForge.Game.Domain.Purchases;

namespace PriceForge.Game.Infrastructure.Persistence.Configurations;

internal sealed class CoinPurchaseConfiguration : IEntityTypeConfiguration<CoinPurchase>
{
    public void Configure(EntityTypeBuilder<CoinPurchase> builder)
    {
        builder.ToTable("coin_purchases");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(p => p.UserId).HasColumnName("user_id");
        builder.Property(p => p.ProductId).HasColumnName("product_id");
        builder.Property(p => p.CoinPrice).HasColumnName("coin_price");
        builder.Property(p => p.IdempotencyKey)
               .HasColumnName("idempotency_key")
               .HasMaxLength(CoinPurchase.MaxIdempotencyKeyLength)
               .IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(p => new {p.UserId, p.IdempotencyKey}).IsUnique();
        builder.HasIndex(p => p.ProductId);
    }
}
