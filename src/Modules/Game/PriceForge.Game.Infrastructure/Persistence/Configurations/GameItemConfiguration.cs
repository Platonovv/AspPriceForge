using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceForge.Game.Domain.Items;

namespace PriceForge.Game.Infrastructure.Persistence.Configurations;

internal sealed class GameItemConfiguration : IEntityTypeConfiguration<GameItem>
{
    public void Configure(EntityTypeBuilder<GameItem> builder)
    {
        builder.ToTable("game_items");
        builder.HasKey(i => i.ProductId);

        builder.Property(i => i.ProductId).HasColumnName("product_id").ValueGeneratedNever();
        builder.Property(i => i.CoinPrice).HasColumnName("coin_price");
    }
}
