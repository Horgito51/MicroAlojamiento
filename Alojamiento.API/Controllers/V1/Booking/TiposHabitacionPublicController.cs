using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Alojamiento.API.Models.Responses.Public;
using Alojamiento.Business.Interfaces.Alojamiento;

namespace Alojamiento.API.Controllers.V1.Booking
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/v1/public/tipos-habitacion")]
    public class TiposHabitacionPublicController : ControllerBase
    {
        private readonly ITipoHabitacionService _tipoHabitacionService;

        public TiposHabitacionPublicController(ITipoHabitacionService tipoHabitacionService)
        {
            _tipoHabitacionService = tipoHabitacionService;
        }

        [HttpGet("{tipoHabitacionGuid:guid}")]
        public async Task<ActionResult<TipoHabitacionPublicDto>> GetByGuid(Guid tipoHabitacionGuid)
        {
            var tipo = await _tipoHabitacionService.GetByGuidAsync(tipoHabitacionGuid);
            return Ok(tipo.ToPublicDto());
        }
    }
}
