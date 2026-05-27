using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Alojamiento.API.Models.Requests.Public;
using Alojamiento.API.Models.Responses.Public;
using Alojamiento.Business.DTOs.Alojamiento;
using Alojamiento.Business.Exceptions;
using Alojamiento.Business.Interfaces.Alojamiento;

namespace Alojamiento.API.Controllers.V1.Booking
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/v1/public/habitaciones")]
    public class HabitacionesPublicController : ControllerBase
    {
        private readonly IHabitacionService _habitacionService;
        private readonly ISucursalService _sucursalService;
        private readonly ITipoHabitacionService _tipoHabitacionService;
        private readonly ITarifaService _tarifaService;

        public HabitacionesPublicController(
            IHabitacionService habitacionService, 
            ISucursalService sucursalService, 
            ITipoHabitacionService tipoHabitacionService,
            ITarifaService tarifaService)
        {
            _habitacionService = habitacionService;
            _sucursalService = sucursalService;
            _tipoHabitacionService = tipoHabitacionService;
            _tarifaService = tarifaService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<HabitacionPublicDto>>> GetAll(
            [FromQuery] DateTime? fechaInicio = null, 
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] Guid? sucursalGuid = null)
        {
            RejectIdQueryParameters();
            IEnumerable<HabitacionDTO> habitaciones;
            var sucursales = await _sucursalService.GetAllAsync();

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                if (fechaFin.Value <= fechaInicio.Value)
                    throw new ValidationException("HAB-PUB-001", "La fecha de fin debe ser posterior a la fecha de inicio.");

                if (sucursalGuid.HasValue)
                {
                    var sucursal = await _sucursalService.GetByGuidAsync(sucursalGuid.Value);
                    habitaciones = await _habitacionService.GetDisponiblesAsync(sucursal.IdSucursal, fechaInicio.Value, fechaFin.Value);
                }
                else
                {
                    var disponibles = new List<HabitacionDTO>();
                    foreach (var sucursal in sucursales)
                    {
                        disponibles.AddRange(await _habitacionService.GetDisponiblesAsync(sucursal.IdSucursal, fechaInicio.Value, fechaFin.Value));
                    }
                    habitaciones = disponibles;
                }
            }
            else
            {
                habitaciones = sucursalGuid.HasValue
                    ? await _habitacionService.GetBySucursalAsync((await _sucursalService.GetByGuidAsync(sucursalGuid.Value)).IdSucursal)
                    : await _habitacionService.GetAllAsync();
            }

            var tipos = await _tipoHabitacionService.GetAllAsync();
            
            var result = new List<HabitacionPublicDto>();
            foreach (var h in habitaciones.Where(h => h.EstadoHabitacion == "DIS"))
            {
                var sucursal = sucursales.FirstOrDefault(s => s.IdSucursal == h.IdSucursal);
                var tipo = tipos.FirstOrDefault(t => t.IdTipoHabitacion == h.IdTipoHabitacion);
                var tarifa = fechaInicio.HasValue && fechaFin.HasValue
                    ? await _tarifaService.GetTarifaVigenteRangoOrDefaultAsync(
                        h.IdSucursal,
                        h.IdTipoHabitacion,
                        fechaInicio.Value,
                        fechaFin.Value,
                        null,
                        HttpContext.RequestAborted)
                    : null;

                if (fechaInicio.HasValue && fechaFin.HasValue && tarifa is null)
                    continue;

                result.Add(new HabitacionPublicDto
                {
                    HabitacionGuid = h.HabitacionGuid,
                    NumeroHabitacion = h.NumeroHabitacion,
                    Piso = h.Piso,
                    CapacidadHabitacion = h.CapacidadHabitacion,
                    PrecioBase = h.PrecioBase,
                    PrecioNocheAplicado = tarifa?.PrecioPorNoche ?? h.PrecioBase,
                    TarifaGuid = tarifa?.TarifaGuid,
                    OrigenPrecio = tarifa is null ? "PRECIO_BASE" : "TARIFA",
                    DescripcionHabitacion = h.DescripcionHabitacion,
                    EstadoHabitacion = h.EstadoHabitacion,
                    SucursalGuid = sucursal?.SucursalGuid ?? Guid.Empty,
                    TipoHabitacionGuid = tipo?.TipoHabitacionGuid ?? Guid.Empty,
                    ImagenUrl = h.Imagenes.FirstOrDefault(i => i.EsPrincipal)?.UrlImagen,
                    Imagenes = h.Imagenes.Select(i => i.ToPublicDto()).ToList()
                });
            }

            return Ok(result);
        }

        private void RejectIdQueryParameters()
        {
            foreach (var key in Request.Query.Keys)
            {
                if (PublicRequestGuard.IsIdProperty(key))
                    throw new ValidationException("PUB-GUID-QUERY-001", $"El parametro '{key}' no esta permitido en endpoints publicos. Use GUIDs.");
            }
        }

        [HttpGet("{habitacionGuid:guid}")]
        public async Task<ActionResult<HabitacionPublicDto>> GetByGuid(Guid habitacionGuid)
        {
            var habitacion = await _habitacionService.GetByGuidAsync(habitacionGuid);
            var sucursal = await _sucursalService.GetByIdAsync(habitacion.IdSucursal);
            var tipo = await _tipoHabitacionService.GetByIdAsync(habitacion.IdTipoHabitacion);
            var tarifa = await _tarifaService.GetTarifaVigenteRangoOrDefaultAsync(
                habitacion.IdSucursal,
                habitacion.IdTipoHabitacion,
                DateTime.UtcNow.Date,
                DateTime.UtcNow.Date.AddDays(1),
                null,
                HttpContext.RequestAborted);
            
            var dto = new HabitacionPublicDto
            {
                HabitacionGuid = habitacion.HabitacionGuid,
                NumeroHabitacion = habitacion.NumeroHabitacion,
                Piso = habitacion.Piso,
                CapacidadHabitacion = habitacion.CapacidadHabitacion,
                PrecioBase = habitacion.PrecioBase,
                PrecioNocheAplicado = tarifa?.PrecioPorNoche ?? habitacion.PrecioBase,
                TarifaGuid = tarifa?.TarifaGuid,
                OrigenPrecio = tarifa is null ? "PRECIO_BASE" : "TARIFA",
                DescripcionHabitacion = habitacion.DescripcionHabitacion,
                EstadoHabitacion = habitacion.EstadoHabitacion,
                SucursalGuid = sucursal.SucursalGuid,
                TipoHabitacionGuid = tipo.TipoHabitacionGuid,
                ImagenUrl = habitacion.Imagenes.FirstOrDefault(i => i.EsPrincipal)?.UrlImagen,
                Imagenes = habitacion.Imagenes.Select(i => i.ToPublicDto()).ToList()
            };

            return Ok(dto);
        }
    }
}
