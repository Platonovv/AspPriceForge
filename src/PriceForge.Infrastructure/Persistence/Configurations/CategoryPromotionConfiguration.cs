using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceForge.Domain.Categories;

namespace PriceForge.Infrastructure.Persistence.Configurations;

public class CategoryPromotionConfiguration : IEntityTypeConfiguration<CategoryPromotion>
{
    public void Configure(EntityTypeBuilder<CategoryPromotion> builder)
    {
        builder.ToTable("category_promotions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.Property(x => x.Percent).HasColumnName("percent").HasColumnType("decimal(10,2)");
        builder.Property(x => x.StartAt).HasColumnName("start_at");
        builder.Property(x => x.EndAt).HasColumnName("end_at");


        builder.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId);
    }
}
