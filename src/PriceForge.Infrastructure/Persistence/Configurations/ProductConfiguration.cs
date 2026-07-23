using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceForge.Domain.Products;

namespace PriceForge.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // 1. Таблица и ключ
        builder.ToTable("products");
        builder.HasKey(p => p.Id);

        // 2. Колонки
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();

        // 3. Sku через ValueConverter
        builder.Property(p => p.Sku)
               .HasColumnName("sku")
               .HasMaxLength(64)
               .IsRequired()
               .HasConversion(sku => sku.Value, value => new Sku(value));

        // Уникальный слепок версии (Конкурентный токен)
        builder.Property<uint>("Version")
               .HasColumnName("xmin")
               .HasColumnType("xid")
               .ValueGeneratedOnAddOrUpdate()
               .IsConcurrencyToken();

        // 4. Sku должен быть уникальным
        builder.HasIndex(p => p.Sku).IsUnique();

        // 5. Money как Owned Type
        builder.OwnsOne(p => p.BasePrice,
                        money =>
                        {
                            money.Property(m => m.Amount)
                                 .HasColumnName("base_price_amount")
                                 .HasColumnType("numeric(18,2)"); // decimal с точностью 18 разрядов, 2 после запятой

                            money.Property(m => m.Currency)
                                 .HasColumnName("base_price_currency")
                                 .HasConversion<string>() // enum → string в БД
                                 .HasMaxLength(3)
                                 .IsRequired();
                        });

        // Навигация: Product → его прямые компоненты
        builder.HasMany<ProductComponent>(nameof(Product.Components))
               .WithOne()
               .HasForeignKey(c => c.ParentId)
               .OnDelete(DeleteBehavior.Cascade);

        // EF должен писать в backing field _components, а не через свойство
        builder.Metadata.FindNavigation(nameof(Product.Components))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
