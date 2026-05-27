using Alojamiento.Business.Interfaces.Alojamiento;
using Alojamiento.Contracts.Grpc.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Alojamiento.API.GrpcServices;

public class AlojamientoGrpcService : Alojamiento.Contracts.Grpc.V1.AlojamientoGrpc.AlojamientoGrpcBase
{
    private readonly ISucursalService _sucursalService;
    private readonly ITipoHabitacionService _tipoHabitacionService;
    private readonly IHabitacionService _habitacionService;
    private readonly ITarifaService _tarifaService;

    public AlojamientoGrpcService(
        ISucursalService sucursalService,
        ITipoHabitacionService tipoHabitacionService,
        IHabitacionService habitacionService,
        ITarifaService tarifaService)
    {
        _sucursalService = sucursalService;
        _tipoHabitacionService = tipoHabitacionService;
        _habitacionService = habitacionService;
        _tarifaService = tarifaService;
    }

    public override async Task<Sucursal> GetSucursalByGuid(GuidRequest request, ServerCallContext context)
    {
        try
        {
            return ToGrpc(await _sucursalService.GetByGuidAsync(Guid.Parse(request.Guid), context.CancellationToken));
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<Sucursal> GetSucursalById(IdRequest request, ServerCallContext context)
    {
        try
        {
            return ToGrpc(await _sucursalService.GetByIdAsync(request.Id, context.CancellationToken));
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<TipoHabitacion> GetTipoHabitacionByGuid(GuidRequest request, ServerCallContext context)
    {
        try
        {
            return ToGrpc(await _tipoHabitacionService.GetByGuidAsync(Guid.Parse(request.Guid), context.CancellationToken));
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<Habitacion> GetHabitacionByGuid(GuidRequest request, ServerCallContext context)
    {
        try
        {
            return ToGrpc(await _habitacionService.GetByGuidAsync(Guid.Parse(request.Guid), context.CancellationToken));
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<HabitacionList> GetHabitacionesDisponibles(HabitacionesDisponiblesRequest request, ServerCallContext context)
    {
        try
        {
            var items = await _habitacionService.GetDisponiblesAsync(
                request.IdSucursal,
                request.FechaInicio.ToDateTime(),
                request.FechaFin.ToDateTime(),
                context.CancellationToken);

            var response = new HabitacionList();
            response.Items.AddRange(items.Select(ToGrpc));
            return response;
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<Empty> SetHabitacionEstado(SetHabitacionEstadoRequest request, ServerCallContext context)
    {
        try
        {
            await _habitacionService.UpdateEstadoAsync(
                request.IdHabitacion,
                request.NuevoEstado,
                string.IsNullOrWhiteSpace(request.Usuario) ? "reservas-service" : request.Usuario,
                context.CancellationToken);

            return new Empty();
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<TarifaResponse> GetTarifaVigenteRango(GetTarifaVigenteRangoRequest request, ServerCallContext context)
    {
        try
        {
            var tarifa = await _tarifaService.GetTarifaVigenteRangoOrDefaultAsync(
                request.IdSucursal,
                request.IdTipoHabitacion,
                request.FechaInicio.ToDateTime(),
                request.FechaFin.ToDateTime(),
                request.Canal,
                context.CancellationToken);

            return tarifa is null
                ? new TarifaResponse { Encontrada = false }
                : new TarifaResponse
                {
                    Encontrada = true,
                    Tarifa = ToGrpc(tarifa)
                };
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    private static Sucursal ToGrpc(Alojamiento.Business.DTOs.Alojamiento.SucursalDTO dto)
    {
        return new Sucursal
        {
            IdSucursal = dto.IdSucursal,
            SucursalGuid = dto.SucursalGuid.ToString(),
            CodigoSucursal = dto.CodigoSucursal ?? string.Empty,
            NombreSucursal = dto.NombreSucursal ?? string.Empty,
            DescripcionSucursal = dto.DescripcionSucursal ?? string.Empty,
            TipoAlojamiento = dto.TipoAlojamiento ?? string.Empty,
            Estrellas = dto.Estrellas,
            Pais = dto.Pais ?? string.Empty,
            Provincia = dto.Provincia ?? string.Empty,
            Ciudad = dto.Ciudad ?? string.Empty,
            Direccion = dto.Direccion ?? string.Empty,
            Telefono = dto.Telefono ?? string.Empty,
            Correo = dto.Correo ?? string.Empty,
            Latitud = dto.Latitud?.ToString() ?? string.Empty,
            Longitud = dto.Longitud?.ToString() ?? string.Empty,
            HoraCheckin = dto.HoraCheckin ?? string.Empty,
            HoraCheckout = dto.HoraCheckout ?? string.Empty,
            AceptaNinos = dto.AceptaNinos,
            PermiteMascotas = dto.PermiteMascotas,
            SePermiteFumar = dto.SePermiteFumar,
            EstadoSucursal = dto.EstadoSucursal ?? string.Empty,
            RowVersion = Google.Protobuf.ByteString.CopyFrom(dto.RowVersion ?? Array.Empty<byte>())
        };
    }

    private static TipoHabitacion ToGrpc(Alojamiento.Business.DTOs.Alojamiento.TipoHabitacionDTO dto)
    {
        return new TipoHabitacion
        {
            IdTipoHabitacion = dto.IdTipoHabitacion,
            TipoHabitacionGuid = dto.TipoHabitacionGuid.ToString(),
            CodigoTipoHabitacion = dto.CodigoTipoHabitacion ?? string.Empty,
            NombreTipoHabitacion = dto.NombreTipoHabitacion ?? string.Empty,
            Descripcion = dto.Descripcion ?? string.Empty,
            CapacidadAdultos = dto.CapacidadAdultos,
            CapacidadNinos = dto.CapacidadNinos,
            CapacidadTotal = dto.CapacidadTotal,
            TipoCama = dto.TipoCama ?? string.Empty,
            AreaM2 = dto.AreaM2?.ToString() ?? string.Empty,
            PermiteEventos = dto.PermiteEventos,
            PermiteReservaPublica = dto.PermiteReservaPublica,
            EstadoTipoHabitacion = dto.EstadoTipoHabitacion ?? string.Empty,
            RowVersion = Google.Protobuf.ByteString.CopyFrom(dto.RowVersion ?? Array.Empty<byte>())
        };
    }

    private static Habitacion ToGrpc(Alojamiento.Business.DTOs.Alojamiento.HabitacionDTO dto)
    {
        return new Habitacion
        {
            IdHabitacion = dto.IdHabitacion,
            HabitacionGuid = dto.HabitacionGuid.ToString(),
            IdSucursal = dto.IdSucursal,
            IdTipoHabitacion = dto.IdTipoHabitacion,
            NumeroHabitacion = dto.NumeroHabitacion ?? string.Empty,
            Piso = dto.Piso,
            CapacidadHabitacion = dto.CapacidadHabitacion,
            PrecioBase = dto.PrecioBase.ToString("0.##"),
            DescripcionHabitacion = dto.DescripcionHabitacion ?? string.Empty,
            EstadoHabitacion = dto.EstadoHabitacion ?? string.Empty,
            EsEliminado = dto.EsEliminado,
            RowVersion = Google.Protobuf.ByteString.CopyFrom(dto.RowVersion ?? Array.Empty<byte>())
        };
    }

    private static Tarifa ToGrpc(Alojamiento.Business.DTOs.Alojamiento.TarifaDTO dto)
    {
        return new Tarifa
        {
            IdTarifa = dto.IdTarifa,
            TarifaGuid = dto.TarifaGuid.ToString(),
            CodigoTarifa = dto.CodigoTarifa ?? string.Empty,
            IdSucursal = dto.IdSucursal,
            IdTipoHabitacion = dto.IdTipoHabitacion,
            CanalTarifa = dto.CanalTarifa ?? string.Empty,
            FechaInicio = Timestamp.FromDateTime(dto.FechaInicio.ToUniversalTime()),
            FechaFin = Timestamp.FromDateTime(dto.FechaFin.ToUniversalTime()),
            PrecioPorNoche = dto.PrecioPorNoche.ToString("0.##"),
            PorcentajeIva = dto.PorcentajeIva.ToString("0.##"),
            PermitePortalPublico = dto.PermitePortalPublico,
            EstadoTarifa = dto.EstadoTarifa ?? string.Empty
        };
    }
}
