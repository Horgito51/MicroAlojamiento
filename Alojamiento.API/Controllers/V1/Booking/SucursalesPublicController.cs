using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Alojamiento.API.Models.Responses.Public;
using Alojamiento.Business.Interfaces.Alojamiento;

namespace Alojamiento.API.Controllers.V1.Booking
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/v1/public/sucursales")]
    public class SucursalesPublicController : ControllerBase
    {
        private readonly ISucursalService _sucursalService;

        public SucursalesPublicController(ISucursalService sucursalService)
        {
            _sucursalService = sucursalService;
        }

        [HttpGet("{sucursalGuid:guid}")]
        public async Task<ActionResult<SucursalPublicDto>> GetByGuid(Guid sucursalGuid)
        {
            var sucursal = await _sucursalService.GetByGuidAsync(sucursalGuid);
            return Ok(sucursal.ToPublicDto());
        }
    }
}
