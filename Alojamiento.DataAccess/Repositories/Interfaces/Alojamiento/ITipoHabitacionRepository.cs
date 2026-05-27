using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Alojamiento.DataAccess.Entities.Alojamiento;

namespace Alojamiento.DataAccess.Repositories.Interfaces.Alojamiento
{
    public interface ITipoHabitacionRepository
    {
        // CRUD b�sico
        Task<TipoHabitacionEntity?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<TipoHabitacionEntity?> GetByGuidAsync(Guid guid, CancellationToken ct = default);
        Task<IEnumerable<TipoHabitacionEntity>> GetAllAsync(CancellationToken ct = default);
        Task<TipoHabitacionEntity> AddAsync(TipoHabitacionEntity entity, CancellationToken ct = default);
        Task UpdateAsync(TipoHabitacionEntity entity, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);

        // Operaciones de escritura
        Task<bool> ExistsByCodigoAsync(string codigo, CancellationToken ct = default);

        // Nuevo m�todo
        Task<IEnumerable<TipoHabitacionEntity>> GetPublicosAsync(CancellationToken ct = default);
    }
}
