using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Alojamiento.DataManagement.Alojamiento.Models;
using Alojamiento.DataManagement.Common;

namespace Alojamiento.DataManagement.Alojamiento.Interfaces
{
    public interface IHabitacionDataService
    {
        // CRUD básico
        Task<HabitacionDataModel> GetByIdAsync(int id, CancellationToken ct = default);
        Task<HabitacionDataModel> GetByGuidAsync(Guid guid, CancellationToken ct = default);
        Task<DataPagedResult<HabitacionDataModel>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default);
        Task<HabitacionDataModel> AddAsync(HabitacionDataModel model, CancellationToken ct = default);
        Task UpdateAsync(HabitacionDataModel model, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);

        // Operaciones específicas
        Task<DataPagedResult<HabitacionDataModel>> GetByFiltroAsync(HabitacionFiltroDataModel filtro, int pageNumber, int pageSize, CancellationToken ct = default);
        Task<IEnumerable<HabitacionDataModel>> GetBySucursalAsync(int idSucursal, CancellationToken ct = default);
        Task<IEnumerable<HabitacionDataModel>> GetByTipoHabitacionAsync(int idTipoHabitacion, CancellationToken ct = default);
        Task<IEnumerable<HabitacionDataModel>> GetDisponiblesAsync(int idSucursal, DateTime inicio, DateTime fin, CancellationToken ct = default);
        Task UpdateEstadoAsync(int id, string nuevoEstado, string usuario, CancellationToken ct = default);
    }
}