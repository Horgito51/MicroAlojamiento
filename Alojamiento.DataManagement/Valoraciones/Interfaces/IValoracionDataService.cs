using System;
using System.Threading;
using System.Threading.Tasks;
using Alojamiento.DataManagement.Valoraciones.Models;
using Alojamiento.DataManagement.Common;

namespace Alojamiento.DataManagement.Valoraciones.Interfaces
{
    public interface IValoracionDataService
    {
        Task<ValoracionDataModel> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ValoracionDataModel> GetByGuidAsync(Guid guid, CancellationToken ct = default);
        Task<DataPagedResult<ValoracionDataModel>> GetByFiltroAsync(ValoracionFiltroDataModel filtro, int pageNumber, int pageSize, CancellationToken ct = default);
        Task<ValoracionDataModel> AddAsync(ValoracionDataModel model, CancellationToken ct = default);
        Task UpdateAsync(ValoracionDataModel model, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);

        Task ModerarAsync(int idValoracion, string nuevoEstado, string motivo, string moderador, CancellationToken ct = default);
        Task ResponderAsync(int idValoracion, string respuesta, string usuario, CancellationToken ct = default);
        Task<bool> ExistsByEstadiaAsync(int idEstadia, CancellationToken ct = default);
    }
}