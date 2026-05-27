using Alojamiento.API.Models.Requests.Public;
using Alojamiento.Business.DTOs.Booking;
using Alojamiento.Business.Exceptions;
using Alojamiento.Business.Interfaces.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alojamiento.API.Controllers.V1.Booking
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/v1/accommodations")]
    public class AccommodationsController : ControllerBase
    {
        private readonly IBookingAccommodationService _bookingService;

        public AccommodationsController(IBookingAccommodationService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet("search")]
        public async Task<ActionResult<BookingPagedResponseDTO<AccommodationSearchItemDTO>>> Search(
            [FromQuery(Name = "Destino")] string? destino = null,
            [FromQuery(Name = "fechaInicio")] DateTime? fechaInicio = null,
            [FromQuery(Name = "fechaFin")] DateTime? fechaFin = null,
            [FromQuery(Name = "NumAdultos")] int? numAdultos = null,
            [FromQuery(Name = "NumNinos")] int? numNinos = null,
            [FromQuery(Name = "NumHabitaciones")] int? numHabitaciones = null,
            [FromQuery(Name = "TipoAlojamiento")] string? tipoAlojamiento = null,
            [FromQuery(Name = "PrecioMin")] decimal? precioMin = null,
            [FromQuery(Name = "PrecioMax")] decimal? precioMax = null,
            [FromQuery(Name = "CategoriaViaje")] string? categoriaViaje = null,
            [FromQuery(Name = "OrdenarPor")] string? ordenarPor = null,
            [FromQuery(Name = "Pagina")] int pagina = 1,
            [FromQuery(Name = "Limite")] int limite = 20)
        {
            PublicRequestGuard.RejectUnsupportedQueryParameters(Request.Query, SearchQueryParameters);
            var query = new AccommodationSearchQueryDTO
            {
                Destino = destino,
                FechaEntrada = fechaInicio,
                FechaSalida = fechaFin,
                NumAdultos = numAdultos,
                NumNinos = numNinos,
                NumHabitaciones = numHabitaciones,
                TipoAlojamiento = tipoAlojamiento,
                PrecioMin = precioMin,
                PrecioMax = precioMax,
                CategoriaViaje = categoriaViaje,
                OrdenarPor = ordenarPor,
                Pagina = pagina,
                Limite = limite
            };

            return Ok(await _bookingService.SearchAsync(query, HttpContext.RequestAborted));
        }

        [HttpGet("{sucursalGuid}")]
        public async Task<ActionResult<AccommodationDetailResponseDTO>> GetByGuid(
            string sucursalGuid,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            PublicRequestGuard.RejectUnsupportedQueryParameters(Request.Query, DetailQueryParameters);
            return Ok(await _bookingService.GetDetailAsync(ParseGuid(sucursalGuid, "sucursalGuid"), fechaInicio, fechaFin, HttpContext.RequestAborted));
        }

        [HttpGet("{sucursalGuid}/reviews")]
        public async Task<ActionResult<BookingPagedResponseDTO<AccommodationReviewDTO>>> GetReviews(
            string sucursalGuid,
            [FromQuery] int pagina = 1,
            [FromQuery] int limite = 10)
        {
            PublicRequestGuard.RejectUnsupportedQueryParameters(Request.Query, ReviewsQueryParameters);
            return Ok(await _bookingService.GetReviewsAsync(ParseGuid(sucursalGuid, "sucursalGuid"), pagina, limite, HttpContext.RequestAborted));
        }

        [HttpGet("/api/v1/alojamiento/sucursales/{sucursalGuid}/valoraciones")]
        public async Task<ActionResult<BookingPagedResponseDTO<AccommodationReviewDTO>>> GetValoracionesBySucursal(
            string sucursalGuid,
            [FromQuery] int pagina = 1,
            [FromQuery] int limite = 10)
        {
            PublicRequestGuard.RejectUnsupportedQueryParameters(Request.Query, ReviewsQueryParameters);
            return Ok(await _bookingService.GetReviewsAsync(ParseGuid(sucursalGuid, "sucursalGuid"), pagina, limite, HttpContext.RequestAborted));
        }

        private static Guid ParseGuid(string value, string parameterName)
        {
            if (!Guid.TryParse(value, out var guid) || guid == Guid.Empty)
            {
                throw new ValidationException("PUB-GUID-PATH-001", $"{parameterName} debe ser un UUID valido.");
            }

            return guid;
        }

        private static readonly HashSet<string> SearchQueryParameters = new(StringComparer.OrdinalIgnoreCase)
        {
            "Destino", "fechaInicio", "fechaFin", "NumAdultos", "NumNinos", "NumHabitaciones",
            "TipoAlojamiento", "PrecioMin", "PrecioMax", "CategoriaViaje", "OrdenarPor", "Pagina",
            "Limite", "api-version"
        };

        private static readonly HashSet<string> DetailQueryParameters = new(StringComparer.OrdinalIgnoreCase)
        {
            "fechaInicio", "fechaFin"
        };

        private static readonly HashSet<string> ReviewsQueryParameters = new(StringComparer.OrdinalIgnoreCase)
        {
            "pagina", "limite", "api-version"
        };
    }
}
