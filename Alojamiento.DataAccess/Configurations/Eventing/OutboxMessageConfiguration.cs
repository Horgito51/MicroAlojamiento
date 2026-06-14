using Alojamiento.DataAccess.Entities.Eventing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alojamiento.DataAccess.Configurations.Eventing;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("OUTBOX_MESSAGES", "eventing");
        builder.HasKey(e => e.IdOutboxMessage);

        builder.Property(e => e.IdOutboxMessage).HasColumnName("id_outbox_message");
        builder.Property(e => e.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(150).IsRequired();
        builder.Property(e => e.EventVersion).HasColumnName("event_version").HasMaxLength(20).IsRequired();
        builder.Property(e => e.RoutingKey).HasColumnName("routing_key").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Payload).HasColumnName("payload").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(e => e.CausationId).HasColumnName("causation_id");
        builder.Property(e => e.Source).HasColumnName("source").HasMaxLength(80).IsRequired();
        builder.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(120);
        builder.Property(e => e.OccurredOnUtc).HasColumnName("occurred_on_utc").IsRequired();
        builder.Property(e => e.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        builder.Property(e => e.PublishedOnUtc).HasColumnName("published_on_utc");
        builder.Property(e => e.PublishAttempts).HasColumnName("publish_attempts").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(e => e.LastError).HasColumnName("last_error").HasMaxLength(2000);

        builder.HasIndex(e => e.EventId).IsUnique();
        builder.HasIndex(e => new { e.Status, e.CreatedOnUtc });
        builder.HasIndex(e => e.CorrelationId);
    }
}

