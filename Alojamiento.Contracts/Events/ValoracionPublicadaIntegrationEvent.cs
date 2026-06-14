namespace Alojamiento.Contracts.Events;

public sealed record ValoracionPublicadaIntegrationEvent : IntegrationEventBase
{
    public override string EventType => "alojamiento.valoracion.publicada";
    public Guid ValoracionGuid { get; init; }
    public Guid SucursalGuid { get; init; }
    public Guid? ReservaGuid { get; init; }
    public Guid? ClienteGuid { get; init; }
    public decimal Puntuacion { get; init; }
    public string? TipoViaje { get; init; }
    public DateTime FechaPublicacionUtc { get; init; }
}

