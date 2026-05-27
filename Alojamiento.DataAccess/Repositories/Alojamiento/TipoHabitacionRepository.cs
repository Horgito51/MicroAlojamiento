using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alojamiento.DataAccess.Context;
using Alojamiento.DataAccess.Entities.Alojamiento;
using Alojamiento.DataAccess.Repositories.Interfaces.Alojamiento;

namespace Alojamiento.DataAccess.Repositories.Alojamiento
{
    public class TipoHabitacionRepository : RepositoryBase<TipoHabitacionEntity>, ITipoHabitacionRepository
    {
        public TipoHabitacionRepository(AlojamientoDbContext context) : base(context) { }

        public async Task<TipoHabitacionEntity?> GetByIdAsync(int id, CancellationToken ct = default)
            => await _dbSet.Include(th => th.TipoHabitacionImagenes).FirstOrDefaultAsync(th => th.IdTipoHabitacion == id, ct);

        public async Task<TipoHabitacionEntity?> GetByGuidAsync(Guid guid, CancellationToken ct = default)
            => await _dbSet.Include(th => th.TipoHabitacionImagenes).FirstOrDefaultAsync(th => th.TipoHabitacionGuid == guid, ct);

        public async Task<IEnumerable<TipoHabitacionEntity>> GetAllAsync(CancellationToken ct = default)
            => await _dbSet.Include(th => th.TipoHabitacionImagenes).ToListAsync(ct);

        public async Task<TipoHabitacionEntity> AddAsync(TipoHabitacionEntity entity, CancellationToken ct = default)
            => await base.AddAsync(entity, ct);

        public async Task UpdateAsync(TipoHabitacionEntity entity, CancellationToken ct = default)
            => await base.UpdateAsync(entity, ct);

        public async Task DeleteAsync(int id, CancellationToken ct = default)
            => await base.DeleteAsync(id, ct);

        public async Task<bool> ExistsByCodigoAsync(string codigo, CancellationToken ct = default)
            => await _dbSet.AnyAsync(th => th.CodigoTipoHabitacion == codigo, ct);

        public async Task<IEnumerable<TipoHabitacionEntity>> GetPublicosAsync(CancellationToken ct = default)
        {
            return await _dbSet
                .Include(th => th.TipoHabitacionImagenes)
                .Where(th => th.PermiteReservaPublica && th.EstadoTipoHabitacion == "ACT")
                .ToListAsync(ct);
        }
    }
}
