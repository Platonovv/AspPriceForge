using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Users;

namespace PriceForge.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Email)
               .HasColumnName("email")
               .HasMaxLength(256)
               .HasConversion(email => email.Value, value => new Email(value))
               .IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Tier)
               .HasColumnName("tier")
               .HasConversion<string>()
               .HasDefaultValue(CustomerTier.Regular)
               .IsRequired();

        builder.Property(x => x.IsEmailVerified).HasColumnName("is_email_verified").HasDefaultValue(false).IsRequired();

        builder.HasIndex(x => x.Email).IsUnique();
    }
}
