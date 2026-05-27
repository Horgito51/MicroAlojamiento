using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Alojamiento.DataAccess.Repositories.Interfaces.Alojamiento;
using Alojamiento.DataManagement.Alojamiento.Interfaces;
using Alojamiento.DataManagement.Alojamiento.Mappers;
using Alojamiento.DataManagement.Alojamiento.Models;
using Alojamiento.DataManagement.UnitOfWork;

namespace Alojamiento.DataManagement.Alojamiento.Services
{
    public class CatalogoServicioDataService : ICatalogoServicioDataService
    {
        private readonly ICatalogoServicioRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CatalogoServicioDataService(ICatalogoServicioRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CatalogoServicioDataModel?> GetByIdAsync(int id, CancellationToken ct = default)
            => (await _repository.GetByIdAsync(id, ct)).ToModel();

        public async Task<CatalogoServicioDataModel?> GetByGuidAsync(Guid guid, CancellationToken ct = default)
            => (await _repository.GetByGuidAsync(guid, ct)).ToModel();

        public async Task<IEnumerable<CatalogoServicioDataModel>> GetAllAsync(CancellationToken ct = default)
            => (await _repository.GetAllAsync(ct)).ToModelList();

        public async Task<CatalogoServicioDataModel> AddAsync(CatalogoServicioDataModel model, CancellationToken ct = default)
        {
            var entity = model.ToEntity()!;
            if (entity.CatalogoGuid == Guid.Empty) entity.CatalogoGuid = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(entity.CreadoPorUsuario)) entity.CreadoPorUsuario = "Sistema";
            if (string.IsNullOrWhiteSpace(entity.ServicioOrigen)) entity.ServicioOrigen = "habitaciones-service";
            entity.FechaRegistroUtc = DateTime.UtcNow;
            var added = await _repository.AddAsync(entity, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return added.ToModel()!;
        }

        public async Task UpdateAsync(CatalogoServicioDataModel model, CancellationToken ct = default)
        {
            await _repository.UpdateAsync(model.ToEntity()!, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            await _repository.DeleteAsync(id, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
