using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceForge.Domain.Products;

namespace PriceForge.Infrastructure.Persistence.Configurations;

internal sealed class ProductComponentConfiguration : IEntityTypeConfiguration<ProductComponent>
{
    public void Configure(EntityTypeBuilder<ProductComponent> builder)
    {
        builder.ToTable("product_components");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.ParentId).HasColumnName("parent_id");
        builder.Property(c => c.ChildId).HasColumnName("child_id");
        builder.Property(c => c.Quantity).HasColumnName("quantity");

        // Запрет дубликата ребра (parent, child) — один товар не содержит компонент дважды
        builder.HasIndex(c => new {c.ParentId, c.ChildId}).IsUnique();

        // FK на родителя — каскад: удалил товар → удалились его рёбра
        builder.HasOne<Product>().WithMany().HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Cascade);

        // FK на ребёнка — restrict: нельзя удалить товар, если он используется как компонент
        builder.HasOne<Product>().WithMany().HasForeignKey(c => c.ChildId).OnDelete(DeleteBehavior.Restrict);
    }
}
