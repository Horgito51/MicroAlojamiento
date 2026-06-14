using Alojamiento.DataAccess.Entities.Eventing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alojamiento.DataAccess.Configurations.Eventing;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<InboxMessageEntity> builder)
    {
        builder.ToTable("INBOX_MESSAGES", "eventing");
        builder.HasKey(e => e.IdInboxMessage);

        builder.Property(e => e.IdInboxMessage).HasColumnName("id_inbox_message");
        builder.Property(e => e.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(150).IsRequired();
        builder.Property(e => e.EventVersion).HasColumnName("event_version").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Source).HasColumnName("source").HasMaxLength(80).IsRequired();
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(e => e.ReceivedOnUtc).HasColumnName("received_on_utc").IsRequired();
        builder.Property(e => e.ProcessedOnUtc).HasColumnName("processed_on_utc");
        builder.Property(e => e.ProcessAttempts).HasColumnName("process_attempts").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(e => e.LastError).HasColumnName("last_error").HasMaxLength(2000);

        builder.HasIndex(e => e.EventId).IsUnique();
        builder.HasIndex(e => new { e.Status, e.ReceivedOnUtc });
        builder.HasIndex(e => e.CorrelationId);
    }
}

