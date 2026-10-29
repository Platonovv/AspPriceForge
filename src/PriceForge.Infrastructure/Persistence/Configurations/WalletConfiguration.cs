using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceForge.Domain.Users;
using PriceForge.Domain.Wallets;

namespace PriceForge.Infrastructure.Persistence.Configurations;

internal sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets");
        builder.HasKey(w => w.UserId);

        builder.Property(w => w.UserId).HasColumnName("user_id").ValueGeneratedNever();
        builder.Property(w => w.CreatedAt).HasColumnName("created_at");
        builder.Property(w => w.LastEarnedAtUtc).HasColumnName("last_earned_at_utc");
        builder.Property(w => w.Balance).HasColumnName("balance");

        builder.Property<uint>("Version")
               .HasColumnName("xmin")
               .HasColumnType("xid")
               .ValueGeneratedOnAddOrUpdate()
               .IsConcurrencyToken();

        builder.HasOne<User>()
               .WithOne()
               .HasForeignKey<Wallet>(w => w.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
