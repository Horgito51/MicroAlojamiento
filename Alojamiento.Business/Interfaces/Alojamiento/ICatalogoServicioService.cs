using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Alojamiento.Business.DTOs.Alojamiento;

namespace Alojamiento.Business.Interfaces.Alojamiento
{
    public interface ICatalogoServicioService
    {
        Task<IEnumerable<CatalogoServicioDTO>> GetAllAsync(CancellationToken ct = default);
        Task<CatalogoServicioDTO> GetByIdAsync(int id, CancellationToken ct = default);
        Task<CatalogoServicioDTO> CreateAsync(CatalogoServicioCreateDTO dto, CancellationToken ct = default);
        Task UpdateAsync(CatalogoServicioUpdateDTO dto, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}
