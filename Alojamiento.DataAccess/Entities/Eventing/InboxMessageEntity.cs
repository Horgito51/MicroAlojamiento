namespace Alojamiento.DataAccess.Entities.Eventing;

public class InboxMessageEntity
{
    public long IdInboxMessage { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventVersion { get; set; } = "v1";
    public string Source { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public DateTime ReceivedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedOnUtc { get; set; }
    public int ProcessAttempts { get; set; }
    public string Status { get; set; } = "REC";
    public string? LastError { get; set; }
}

