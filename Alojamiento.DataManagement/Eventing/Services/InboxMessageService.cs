using Alojamiento.DataAccess.Context;
using Alojamiento.DataAccess.Entities.Eventing;
using Alojamiento.DataManagement.Eventing.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Alojamiento.DataManagement.Eventing.Services;

public sealed class InboxMessageService : IInboxMessageService
{
    private readonly AlojamientoDbContext _context;

    public InboxMessageService(AlojamientoDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryRegisterReceivedAsync(
        InboxMessageEntity message,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.InboxMessages
            .AnyAsync(existing => existing.EventId == message.EventId, cancellationToken);

        if (exists)
        {
            return false;
        }

        message.ReceivedOnUtc = DateTime.UtcNow;
        message.Status = string.IsNullOrWhiteSpace(message.Status) ? "REC" : message.Status;
        await _context.InboxMessages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var message = await GetByEventIdAsync(eventId, cancellationToken);
        message.Status = "PRO";
        message.ProcessedOnUtc = DateTime.UtcNow;
        message.LastError = null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid eventId, string error, CancellationToken cancellationToken = default)
    {
        var message = await GetByEventIdAsync(eventId, cancellationToken);
        message.Status = "ERR";
        message.ProcessAttempts++;
        message.LastError = TrimError(error);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<InboxMessageEntity> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _context.InboxMessages.FirstAsync(message => message.EventId == eventId, cancellationToken);
    }

    private static string TrimError(string error)
    {
        return string.IsNullOrWhiteSpace(error)
            ? "Error no especificado."
            : error.Length <= 2000 ? error : error[..2000];
    }
}

