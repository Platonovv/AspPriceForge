using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceForge.Domain.Orders;
using PriceForge.Domain.Users;

namespace PriceForge.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.ItemsTotal).HasColumnName("items_total");
        builder.Property(x => x.Total).HasColumnName("total");
        builder.Property(x => x.PromoCodeName).HasColumnName("promo_code_name");
        builder.Property(x => x.TotalDiscountAmount).HasColumnName("total_discount_amount");
        builder.Property(x => x.DiscountPercent).HasColumnName("discount_percent");
        builder.Property(x => x.Currency).HasColumnName("currency").HasConversion<string>().IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>();

        builder.HasIndex(p => p.UserId);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(x => x.OrderItems,
                         item =>
                         {
                             item.ToTable("order_items");
                             item.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
                             item.Property(x => x.ProductName).HasColumnName("product_name");
                             item.Property(x => x.Quantity).HasColumnName("quantity");
                             item.Property(x => x.UnitPrice).HasColumnName("unit_price");
                         });

        builder.Metadata.FindNavigation(nameof(Order.OrderItems))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
