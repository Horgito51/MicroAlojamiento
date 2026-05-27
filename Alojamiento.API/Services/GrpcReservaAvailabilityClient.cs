using Alojamiento.Business.Interfaces.Booking;
using Grpc.Core;
using Reservas.Contracts.Grpc.V1;

namespace Alojamiento.API.Services;

public sealed class GrpcReservaAvailabilityClient : IReservaAvailabilityClient
{
    private static readonly string[] ActiveReservationStates = { "PEN", "CON", "EMI" };
    private const int PageSize = 500;

    private readonly ReservaGrpc.ReservaGrpcClient _client;
    private readonly ILogger<GrpcReservaAvailabilityClient> _logger;

    public GrpcReservaAvailabilityClient(
        ReservaGrpc.ReservaGrpcClient client,
        ILogger<GrpcReservaAvailabilityClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<IReadOnlySet<int>> GetHabitacionesReservadasAsync(
        int idSucursal,
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken ct = default)
    {
        var reservadas = new HashSet<int>();

        try
        {
            foreach (var estado in ActiveReservationStates)
            {
                var pageNumber = 1;
                while (true)
                {
                    var page = await _client.ListReservasAsync(
                        new ReservaFiltroRequest
                        {
                            Page = new PageRequest { PageNumber = pageNumber, PageSize = PageSize },
                            IdSucursal = idSucursal,
                            EstadoReserva = estado,
                            EsEliminado = false
                        },
                        cancellationToken: ct);

                    foreach (var reserva in page.Items)
                    {
                        if (!RangesOverlap(reserva.FechaInicio.ToDateTime(), reserva.FechaFin.ToDateTime(), fechaInicio, fechaFin))
                            continue;

                        foreach (var habitacion in reserva.Habitaciones)
                        {
                            if (habitacion.IdHabitacion > 0 &&
                                RangesOverlap(habitacion.FechaInicio.ToDateTime(), habitacion.FechaFin.ToDateTime(), fechaInicio, fechaFin))
                            {
                                reservadas.Add(habitacion.IdHabitacion);
                            }
                        }
                    }

                    if (page.Items.Count == 0 || page.PageNumber * page.PageSize >= page.TotalCount)
                        break;

                    pageNumber++;
                }
            }
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(
                ex,
                "No se pudo consultar disponibilidad en Reservas para sucursal {IdSucursal} entre {FechaInicio:yyyy-MM-dd} y {FechaFin:yyyy-MM-dd}. Se usara disponibilidad local.",
                idSucursal,
                fechaInicio,
                fechaFin);
        }

        return reservadas;
    }

    private static bool RangesOverlap(DateTime existingStart, DateTime existingEnd, DateTime requestedStart, DateTime requestedEnd)
        => existingStart.Date < requestedEnd.Date && existingEnd.Date > requestedStart.Date;
}

public sealed class NullReservaAvailabilityClient : IReservaAvailabilityClient
{
    public Task<IReadOnlySet<int>> GetHabitacionesReservadasAsync(
        int idSucursal,
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlySet<int>>(new HashSet<int>());
}
