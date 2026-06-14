namespace Alojamiento.Contracts.Events;

public sealed record DisponibilidadBloqueadaIntegrationEvent : IntegrationEventBase
{
    public override string EventType => "alojamiento.disponibilidad.bloqueada";
    public Guid ReservaGuid { get; init; }
    public Guid SucursalGuid { get; init; }
    public DateTime FechaInicio { get; init; }
    public DateTime FechaFin { get; init; }
    public IReadOnlyList<Guid> HabitacionGuids { get; init; } = Array.Empty<Guid>();
}

