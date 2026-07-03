using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceForge.Domain.PromoCodes;

namespace PriceForge.Infrastructure.Persistence.Configurations;

internal sealed class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.ToTable("promo_codes");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Code).HasColumnName("code").HasConversion(c => c.Value, v => new Code(v));
        builder.Property(p => p.DiscountPercent).HasColumnName("discount_percent");
        builder.Property(p => p.ValidFrom).HasColumnName("valid_from");
        builder.Property(p => p.ValidTo).HasColumnName("valid_to");
        builder.Property(p => p.UsedCount).HasColumnName("used_count");
        builder.Property(p => p.UsedLimit).HasColumnName("used_limit");
        builder.Property(p => p.MinOrderAmount).HasColumnName("min_order_amount");

        builder.HasIndex(p => p.Code).IsUnique();
    }
}
