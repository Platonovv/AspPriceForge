using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceForge.Domain.Products;

namespace PriceForge.Infrastructure.Persistence.Configurations;

internal sealed class ProductClosureConfiguration : IEntityTypeConfiguration<ProductClosure>
{
    public void Configure(EntityTypeBuilder<ProductClosure> builder)
    {
        builder.ToTable("product_closure");

        // Composite PK на пару (ancestor, descendant)
        builder.HasKey(c => new {c.AncestorId, c.DescendantId});

        builder.Property(c => c.AncestorId).HasColumnName("ancestor_id");
        builder.Property(c => c.DescendantId).HasColumnName("descendant_id");
        builder.Property(c => c.Depth).HasColumnName("depth");

        // FK на ancestor — CASCADE: удалили продукт → закрыли все записи где он предок
        builder.HasOne<Product>().WithMany().HasForeignKey(c => c.AncestorId).OnDelete(DeleteBehavior.Cascade);

        // FK на descendant — тоже CASCADE: удалили продукт → закрыли записи где он потомок
        builder.HasOne<Product>().WithMany().HasForeignKey(c => c.DescendantId).OnDelete(DeleteBehavior.Cascade);

        // Индекс на descendant для запросов "найти всех предков узла"
        builder.HasIndex(c => c.DescendantId);

        // Индекс по (descendant, depth) для запросов "найди ближайшего предка"
        builder.HasIndex(c => new {c.DescendantId, c.Depth});
    }
}
