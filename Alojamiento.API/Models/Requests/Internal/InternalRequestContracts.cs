using System;
using System.Collections.Generic;
using Alojamiento.Business.DTOs.Alojamiento;
using Alojamiento.Business.DTOs.Valoraciones;

namespace Alojamiento.API.Models.Requests.Internal
{
    public sealed class ImagenRequest
    {
        public Guid ImagenGuid { get; set; }
        public string UrlImagen { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int Orden { get; set; }
        public bool EsPrincipal { get; set; }
    }

    public sealed class SucursalUpsertRequest
    {
        public string CodigoSucursal { get; set; } = string.Empty;
        public string NombreSucursal { get; set; } = string.Empty;
        public string? DescripcionSucursal { get; set; }
        public string? DescripcionCorta { get; set; }
        public string? TipoAlojamiento { get; set; }
        public int? Estrellas { get; set; }
        public string? CategoriaViaje { get; set; }
        public string? Pais { get; set; }
        public string? Provincia { get; set; }
        public string? Ciudad { get; set; }
        public string? Ubicacion { get; set; }
        public string? Direccion { get; set; }
        public string? CodigoPostal { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public string? HoraCheckin { get; set; }
        public string? HoraCheckout { get; set; }
        public bool CheckinAnticipado { get; set; }
        public bool CheckoutTardio { get; set; }
        public bool AceptaNinos { get; set; }
        public int? EdadMinimaHuesped { get; set; }
        public bool PermiteMascotas { get; set; }
        public bool SePermiteFumar { get; set; }
        public string EstadoSucursal { get; set; } = "ACT";
        public List<ImagenRequest>? Imagenes { get; set; }
    }

    public sealed class SucursalPoliticasPatchRequest
    {
        public string? HoraCheckin { get; set; }
        public string? HoraCheckout { get; set; }
        public bool PermiteMascotas { get; set; }
        public bool SePermiteFumar { get; set; }
        public bool AceptaNinos { get; set; }
        public bool CheckinAnticipado { get; set; }
        public bool CheckoutTardio { get; set; }
    }

    public sealed class InhabilitarRequest
    {
        public string Motivo { get; set; } = string.Empty;
    }

    public sealed class TipoHabitacionUpsertRequest
    {
        public string CodigoTipoHabitacion { get; set; } = string.Empty;
        public string NombreTipoHabitacion { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int CapacidadAdultos { get; set; }
        public int CapacidadNinos { get; set; }
        public int CapacidadTotal { get; set; }
        public string TipoCama { get; set; } = string.Empty;
        public decimal? AreaM2 { get; set; }
        public bool PermiteEventos { get; set; }
        public bool PermiteReservaPublica { get; set; }
        public string EstadoTipoHabitacion { get; set; } = "ACT";
        public List<ImagenRequest>? Imagenes { get; set; }
    }

    public class HabitacionCreateRequest
    {
        public int IdSucursal { get; set; }
        public int IdTipoHabitacion { get; set; }
        public string NumeroHabitacion { get; set; } = string.Empty;
        public int? Piso { get; set; }
        public int CapacidadHabitacion { get; set; }
        public decimal PrecioBase { get; set; }
        public string? DescripcionHabitacion { get; set; }
        public string EstadoHabitacion { get; set; } = "DIS";
    }

    public sealed class HabitacionUpdateRequest : HabitacionCreateRequest
    {
    }

    public sealed class HabitacionEstadoRequest
    {
        public string NuevoEstado { get; set; } = string.Empty;
    }

    public sealed class TarifaUpsertRequest
    {
        public string CodigoTarifa { get; set; } = string.Empty;
        public int IdSucursal { get; set; }
        public int IdTipoHabitacion { get; set; }
        public string NombreTarifa { get; set; } = string.Empty;
        public string CanalTarifa { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal PrecioPorNoche { get; set; }
        public decimal PorcentajeIva { get; set; }
        public int MinNoches { get; set; }
        public int? MaxNoches { get; set; }
        public bool PermitePortalPublico { get; set; }
        public int Prioridad { get; set; }
        public string EstadoTarifa { get; set; } = "ACT";
    }

    public sealed class CatalogoServicioUpsertRequest
    {
        public int? IdSucursal { get; set; }
        public string CodigoCatalogo { get; set; } = string.Empty;
        public string NombreCatalogo { get; set; } = string.Empty;
        public string TipoCatalogo { get; set; } = string.Empty;
        public string? CategoriaCatalogo { get; set; }
        public string? DescripcionCatalogo { get; set; }
        public decimal PrecioBase { get; set; }
        public bool AplicaIva { get; set; }
        public bool Disponible24h { get; set; }
        public TimeSpan? HoraInicio { get; set; }
        public TimeSpan? HoraFin { get; set; }
        public string? IconoUrl { get; set; }
        public string EstadoCatalogo { get; set; } = "ACT";
    }

    public sealed class ValoracionCreateRequest
    {
        public int IdEstadia { get; set; }
        public int IdCliente { get; set; }
        public int IdSucursal { get; set; }
        public int? IdHabitacion { get; set; }
        public decimal PuntuacionGeneral { get; set; }
        public decimal? PuntuacionLimpieza { get; set; }
        public decimal? PuntuacionConfort { get; set; }
        public decimal? PuntuacionUbicacion { get; set; }
        public decimal? PuntuacionInstalaciones { get; set; }
        public decimal? PuntuacionPersonal { get; set; }
        public decimal? PuntuacionCalidadPrecio { get; set; }
        public string? ComentarioPositivo { get; set; }
        public string? ComentarioNegativo { get; set; }
        public string? TipoViaje { get; set; }
    }

    public sealed class ValoracionModeracionRequest
    {
        public string NuevoEstado { get; set; } = string.Empty;
        public string? Motivo { get; set; }
    }

    public sealed class ValoracionRespuestaRequest
    {
        public string Respuesta { get; set; } = string.Empty;
    }

    public static class InternalRequestMapper
    {
        public static ImagenDTO ToDto(this ImagenRequest request) => new()
        {
            ImagenGuid = request.ImagenGuid,
            UrlImagen = request.UrlImagen,
            Descripcion = request.Descripcion,
            Orden = request.Orden,
            EsPrincipal = request.EsPrincipal
        };

        public static List<ImagenDTO>? ToImagenDtos(this List<ImagenRequest>? requests)
            => requests?.ConvertAll(request => request.ToDto());

        public static SucursalCreateDTO ToCreateDto(this SucursalUpsertRequest request) => new()
        {
            CodigoSucursal = request.CodigoSucursal,
            NombreSucursal = request.NombreSucursal,
            DescripcionSucursal = request.DescripcionSucursal,
            DescripcionCorta = request.DescripcionCorta,
            TipoAlojamiento = request.TipoAlojamiento,
            Estrellas = request.Estrellas,
            CategoriaViaje = request.CategoriaViaje,
            Pais = request.Pais,
            Provincia = request.Provincia,
            Ciudad = request.Ciudad,
            Ubicacion = request.Ubicacion,
            Direccion = request.Direccion,
            CodigoPostal = request.CodigoPostal,
            Telefono = request.Telefono,
            Correo = request.Correo,
            Latitud = request.Latitud,
            Longitud = request.Longitud,
            HoraCheckin = request.HoraCheckin,
            HoraCheckout = request.HoraCheckout,
            CheckinAnticipado = request.CheckinAnticipado,
            CheckoutTardio = request.CheckoutTardio,
            AceptaNinos = request.AceptaNinos,
            EdadMinimaHuesped = request.EdadMinimaHuesped,
            PermiteMascotas = request.PermiteMascotas,
            SePermiteFumar = request.SePermiteFumar,
            EstadoSucursal = request.EstadoSucursal,
            Imagenes = request.Imagenes.ToImagenDtos()
        };

        public static SucursalUpdateDTO ToUpdateDto(this SucursalUpsertRequest request, int id) => new()
        {
            IdSucursal = id,
            CodigoSucursal = request.CodigoSucursal,
            NombreSucursal = request.NombreSucursal,
            DescripcionSucursal = request.DescripcionSucursal,
            DescripcionCorta = request.DescripcionCorta,
            TipoAlojamiento = request.TipoAlojamiento,
            Estrellas = request.Estrellas,
            CategoriaViaje = request.CategoriaViaje,
            Pais = request.Pais,
            Provincia = request.Provincia,
            Ciudad = request.Ciudad,
            Ubicacion = request.Ubicacion,
            Direccion = request.Direccion,
            CodigoPostal = request.CodigoPostal,
            Telefono = request.Telefono,
            Correo = request.Correo,
            Latitud = request.Latitud,
            Longitud = request.Longitud,
            HoraCheckin = request.HoraCheckin,
            HoraCheckout = request.HoraCheckout,
            CheckinAnticipado = request.CheckinAnticipado,
            CheckoutTardio = request.CheckoutTardio,
            AceptaNinos = request.AceptaNinos,
            EdadMinimaHuesped = request.EdadMinimaHuesped,
            PermiteMascotas = request.PermiteMascotas,
            SePermiteFumar = request.SePermiteFumar,
            EstadoSucursal = request.EstadoSucursal,
            Imagenes = request.Imagenes.ToImagenDtos()
        };

        public static SucursalPoliticasUpdateDTO ToDto(this SucursalPoliticasPatchRequest request) => new()
        {
            HoraCheckin = request.HoraCheckin,
            HoraCheckout = request.HoraCheckout,
            PermiteMascotas = request.PermiteMascotas,
            SePermiteFumar = request.SePermiteFumar,
            AceptaNinos = request.AceptaNinos,
            CheckinAnticipado = request.CheckinAnticipado,
            CheckoutTardio = request.CheckoutTardio
        };

        public static TipoHabitacionCreateDTO ToCreateDto(this TipoHabitacionUpsertRequest request) => new()
        {
            CodigoTipoHabitacion = request.CodigoTipoHabitacion,
            NombreTipoHabitacion = request.NombreTipoHabitacion,
            Descripcion = request.Descripcion,
            CapacidadAdultos = request.CapacidadAdultos,
            CapacidadNinos = request.CapacidadNinos,
            CapacidadTotal = request.CapacidadTotal,
            TipoCama = request.TipoCama,
            AreaM2 = request.AreaM2,
            PermiteEventos = request.PermiteEventos,
            PermiteReservaPublica = request.PermiteReservaPublica,
            EstadoTipoHabitacion = request.EstadoTipoHabitacion,
            Imagenes = request.Imagenes.ToImagenDtos()
        };

        public static TipoHabitacionUpdateDTO ToUpdateDto(this TipoHabitacionUpsertRequest request, int id) => new()
        {
            IdTipoHabitacion = id,
            CodigoTipoHabitacion = request.CodigoTipoHabitacion,
            NombreTipoHabitacion = request.NombreTipoHabitacion,
            Descripcion = request.Descripcion,
            CapacidadAdultos = request.CapacidadAdultos,
            CapacidadNinos = request.CapacidadNinos,
            CapacidadTotal = request.CapacidadTotal,
            TipoCama = request.TipoCama,
            AreaM2 = request.AreaM2,
            PermiteEventos = request.PermiteEventos,
            PermiteReservaPublica = request.PermiteReservaPublica,
            EstadoTipoHabitacion = request.EstadoTipoHabitacion,
            Imagenes = request.Imagenes.ToImagenDtos()
        };

        public static HabitacionCreateDTO ToCreateDto(this HabitacionCreateRequest request) => new()
        {
            IdSucursal = request.IdSucursal,
            IdTipoHabitacion = request.IdTipoHabitacion,
            NumeroHabitacion = request.NumeroHabitacion,
            Piso = request.Piso,
            CapacidadHabitacion = request.CapacidadHabitacion,
            PrecioBase = request.PrecioBase,
            DescripcionHabitacion = request.DescripcionHabitacion,
            EstadoHabitacion = request.EstadoHabitacion
        };

        public static HabitacionUpdateDTO ToUpdateDto(this HabitacionUpdateRequest request, int id) => new()
        {
            IdHabitacion = id,
            IdSucursal = request.IdSucursal,
            IdTipoHabitacion = request.IdTipoHabitacion,
            NumeroHabitacion = request.NumeroHabitacion,
            Piso = request.Piso,
            CapacidadHabitacion = request.CapacidadHabitacion,
            PrecioBase = request.PrecioBase,
            DescripcionHabitacion = request.DescripcionHabitacion,
            EstadoHabitacion = request.EstadoHabitacion
        };

        public static TarifaCreateDTO ToCreateDto(this TarifaUpsertRequest request) => new()
        {
            CodigoTarifa = request.CodigoTarifa,
            IdSucursal = request.IdSucursal,
            IdTipoHabitacion = request.IdTipoHabitacion,
            NombreTarifa = request.NombreTarifa,
            CanalTarifa = request.CanalTarifa,
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            PrecioPorNoche = request.PrecioPorNoche,
            PorcentajeIva = request.PorcentajeIva,
            MinNoches = request.MinNoches,
            MaxNoches = request.MaxNoches,
            PermitePortalPublico = request.PermitePortalPublico,
            Prioridad = request.Prioridad,
            EstadoTarifa = request.EstadoTarifa
        };

        public static TarifaUpdateDTO ToUpdateDto(this TarifaUpsertRequest request, int id) => new()
        {
            IdTarifa = id,
            CodigoTarifa = request.CodigoTarifa,
            IdSucursal = request.IdSucursal,
            IdTipoHabitacion = request.IdTipoHabitacion,
            NombreTarifa = request.NombreTarifa,
            CanalTarifa = request.CanalTarifa,
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            PrecioPorNoche = request.PrecioPorNoche,
            PorcentajeIva = request.PorcentajeIva,
            MinNoches = request.MinNoches,
            MaxNoches = request.MaxNoches,
            PermitePortalPublico = request.PermitePortalPublico,
            Prioridad = request.Prioridad,
            EstadoTarifa = request.EstadoTarifa
        };

        public static CatalogoServicioCreateDTO ToCreateDto(this CatalogoServicioUpsertRequest request) => new()
        {
            IdSucursal = request.IdSucursal,
            CodigoCatalogo = request.CodigoCatalogo,
            NombreCatalogo = request.NombreCatalogo,
            TipoCatalogo = request.TipoCatalogo,
            CategoriaCatalogo = request.CategoriaCatalogo,
            DescripcionCatalogo = request.DescripcionCatalogo,
            PrecioBase = request.PrecioBase,
            AplicaIva = request.AplicaIva,
            Disponible24h = request.Disponible24h,
            HoraInicio = request.HoraInicio,
            HoraFin = request.HoraFin,
            IconoUrl = request.IconoUrl,
            EstadoCatalogo = request.EstadoCatalogo
        };

        public static CatalogoServicioUpdateDTO ToUpdateDto(this CatalogoServicioUpsertRequest request, int id) => new()
        {
            IdCatalogo = id,
            IdSucursal = request.IdSucursal,
            CodigoCatalogo = request.CodigoCatalogo,
            NombreCatalogo = request.NombreCatalogo,
            TipoCatalogo = request.TipoCatalogo,
            CategoriaCatalogo = request.CategoriaCatalogo,
            DescripcionCatalogo = request.DescripcionCatalogo,
            PrecioBase = request.PrecioBase,
            AplicaIva = request.AplicaIva,
            Disponible24h = request.Disponible24h,
            HoraInicio = request.HoraInicio,
            HoraFin = request.HoraFin,
            IconoUrl = request.IconoUrl,
            EstadoCatalogo = request.EstadoCatalogo
        };

        public static ValoracionDTO ToDto(this ValoracionCreateRequest request) => new()
        {
            IdEstadia = request.IdEstadia,
            IdCliente = request.IdCliente,
            IdSucursal = request.IdSucursal,
            IdHabitacion = request.IdHabitacion,
            PuntuacionGeneral = request.PuntuacionGeneral,
            PuntuacionLimpieza = request.PuntuacionLimpieza,
            PuntuacionConfort = request.PuntuacionConfort,
            PuntuacionUbicacion = request.PuntuacionUbicacion,
            PuntuacionInstalaciones = request.PuntuacionInstalaciones,
            PuntuacionPersonal = request.PuntuacionPersonal,
            PuntuacionCalidadPrecio = request.PuntuacionCalidadPrecio,
            ComentarioPositivo = request.ComentarioPositivo ?? string.Empty,
            ComentarioNegativo = request.ComentarioNegativo ?? string.Empty,
            TipoViaje = request.TipoViaje ?? string.Empty,
            EstadoValoracion = "PEN",
            PublicadaEnPortal = false,
            CreadoPorUsuario = "Sistema",
            ServicioOrigen = "Alojamiento"
        };
    }
}
