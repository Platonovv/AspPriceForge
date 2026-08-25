using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceForge.Domain.Images;
using PriceForge.Domain.Products;

namespace PriceForge.Infrastructure.Persistence.Configurations;

internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");

        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.Id).HasColumnName("id").IsRequired();
        builder.Property(pi => pi.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(pi => pi.Key).HasColumnName("key").IsRequired().HasMaxLength(512);
        builder.Property(pi => pi.ContentType).HasColumnName("content_type").IsRequired().HasMaxLength(20);
        builder.Property(pi => pi.SizeBytes).HasColumnName("size_bytes").IsRequired();
        builder.Property(pi => pi.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(pi => pi.ProductId).IsUnique();

        builder.HasOne<Product>().WithMany().HasForeignKey(c => c.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
