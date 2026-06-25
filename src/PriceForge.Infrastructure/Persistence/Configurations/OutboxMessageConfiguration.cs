using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceForge.Application.Brokers;

namespace PriceForge.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Type).HasColumnName("type");
        builder.Property(p => p.Content).HasColumnName("content");
        builder.Property(p => p.OccurredAt).HasColumnName("occurred_at");
        builder.Property(p => p.PublishedAt).HasColumnName("published_at");

        builder.HasIndex(p => p.PublishedAt);
    }
}
