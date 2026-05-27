using Alojamiento.API.Models.Responses.Public;
using Alojamiento.Business.DTOs.Alojamiento;
using Alojamiento.Business.DTOs.Valoraciones;
using System.Linq;

namespace Alojamiento.API.Models.Responses.Public
{
    public static class PublicDtoMapper
    {
        public static SucursalPublicDto ToPublicDto(this SucursalDTO dto) => new()
        {
            SucursalGuid = dto.SucursalGuid,
            CodigoSucursal = dto.CodigoSucursal,
            NombreSucursal = dto.NombreSucursal,
            DescripcionSucursal = dto.DescripcionSucursal,
            TipoAlojamiento = dto.TipoAlojamiento,
            Estrellas = dto.Estrellas,
            CategoriaViaje = dto.CategoriaViaje,
            Pais = dto.Pais,
            Provincia = dto.Provincia,
            Ciudad = dto.Ciudad,
            Direccion = dto.Direccion,
            Telefono = dto.Telefono,
            Correo = dto.Correo,
            HoraCheckin = dto.HoraCheckin,
            HoraCheckout = dto.HoraCheckout,
            CheckinAnticipado = dto.CheckinAnticipado,
            CheckoutTardio = dto.CheckoutTardio,
            AceptaNinos = dto.AceptaNinos,
            PermiteMascotas = dto.PermiteMascotas,
            SePermiteFumar = dto.SePermiteFumar,
            EstadoSucursal = dto.EstadoSucursal,
            Imagenes = dto.Imagenes.Select(i => i.ToPublicDto()).ToList()
        };

        public static TipoHabitacionPublicDto ToPublicDto(this TipoHabitacionDTO dto) => new()
        {
            TipoHabitacionGuid = dto.TipoHabitacionGuid,
            CodigoTipoHabitacion = dto.CodigoTipoHabitacion,
            NombreTipoHabitacion = dto.NombreTipoHabitacion,
            Descripcion = dto.Descripcion,
            CapacidadAdultos = dto.CapacidadAdultos,
            CapacidadNinos = dto.CapacidadNinos,
            CapacidadTotal = dto.CapacidadTotal,
            TipoCama = dto.TipoCama,
            AreaM2 = dto.AreaM2,
            PermiteEventos = dto.PermiteEventos,
            PermiteReservaPublica = dto.PermiteReservaPublica,
            EstadoTipoHabitacion = dto.EstadoTipoHabitacion,
            Imagenes = dto.Imagenes.Select(i => i.ToPublicDto()).ToList()
        };

        public static HabitacionPublicDto ToPublicDto(this HabitacionDTO dto, SucursalDTO sucursal, TipoHabitacionDTO tipo) => new()
        {
            HabitacionGuid = dto.HabitacionGuid,
            NumeroHabitacion = dto.NumeroHabitacion,
            Piso = dto.Piso,
            CapacidadHabitacion = dto.CapacidadHabitacion,
            PrecioBase = dto.PrecioBase,
            PrecioNocheAplicado = dto.PrecioBase,
            OrigenPrecio = "PRECIO_BASE",
            DescripcionHabitacion = dto.DescripcionHabitacion,
            EstadoHabitacion = dto.EstadoHabitacion,
            SucursalGuid = sucursal.SucursalGuid,
            TipoHabitacionGuid = tipo.TipoHabitacionGuid,
            ImagenUrl = dto.Imagenes.FirstOrDefault(i => i.EsPrincipal)?.UrlImagen,
            Imagenes = dto.Imagenes.Select(i => i.ToPublicDto()).ToList()
        };

        public static ImagenPublicDto ToPublicDto(this ImagenDTO dto) => new()
        {
            ImagenGuid = dto.ImagenGuid,
            UrlImagen = dto.UrlImagen,
            Descripcion = dto.Descripcion,
            Orden = dto.Orden,
            EsPrincipal = dto.EsPrincipal
        };

        public static ValoracionPublicDto ToPublicDto(this ValoracionDTO dto) => new()
        {
            ValoracionGuid = dto.ValoracionGuid,
            PuntuacionGeneral = dto.PuntuacionGeneral,
            PuntuacionLimpieza = dto.PuntuacionLimpieza,
            PuntuacionConfort = dto.PuntuacionConfort,
            PuntuacionUbicacion = dto.PuntuacionUbicacion,
            PuntuacionInstalaciones = dto.PuntuacionInstalaciones,
            PuntuacionPersonal = dto.PuntuacionPersonal,
            PuntuacionCalidadPrecio = dto.PuntuacionCalidadPrecio,
            ComentarioPositivo = dto.ComentarioPositivo,
            ComentarioNegativo = dto.ComentarioNegativo,
            TipoViaje = dto.TipoViaje,
            EstadoValoracion = dto.EstadoValoracion,
            PublicadaEnPortal = dto.PublicadaEnPortal,
            RespuestaHotel = dto.RespuestaHotel,
            FechaRespuestaUtc = dto.FechaRespuestaUtc,
            FechaRegistroUtc = dto.FechaRegistroUtc
        };
    }
}
