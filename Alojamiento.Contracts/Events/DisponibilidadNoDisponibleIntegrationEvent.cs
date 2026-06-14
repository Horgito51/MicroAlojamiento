namespace Alojamiento.Contracts.Events;

public sealed record DisponibilidadNoDisponibleIntegrationEvent : IntegrationEventBase
{
    public override string EventType => "alojamiento.disponibilidad.no_disponible";
    public Guid ReservaGuid { get; init; }
    public Guid SucursalGuid { get; init; }
    public Guid? TipoHabitacionGuid { get; init; }
    public DateTime FechaInicio { get; init; }
    public DateTime FechaFin { get; init; }
    public string Motivo { get; init; } = string.Empty;
}

