using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceForge.Domain.Products;
using PriceForge.Domain.Promotions;

namespace PriceForge.Infrastructure.Persistence.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.Percent).HasColumnName("percent").HasColumnType("decimal(10,2)");
        builder.Property(x => x.StartAt).HasColumnName("start_at");
        builder.Property(x => x.EndAt).HasColumnName("end_at");

        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId);
    }
}
