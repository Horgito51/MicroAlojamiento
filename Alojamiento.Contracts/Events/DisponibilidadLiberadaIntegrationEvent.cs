namespace Alojamiento.Contracts.Events;

public sealed record DisponibilidadLiberadaIntegrationEvent : IntegrationEventBase
{
    public override string EventType => "alojamiento.disponibilidad.liberada";
    public Guid ReservaGuid { get; init; }
    public Guid SucursalGuid { get; init; }
    public DateTime FechaLiberacionUtc { get; init; }
    public IReadOnlyList<Guid> HabitacionGuids { get; init; } = Array.Empty<Guid>();
}

