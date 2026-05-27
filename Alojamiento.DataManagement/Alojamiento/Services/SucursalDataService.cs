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
    public class SucursalDataService : ISucursalDataService
    {
        private readonly ISucursalRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public SucursalDataService(ISucursalRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SucursalDataModel?> GetByIdAsync(int id, CancellationToken ct = default)
            => (await _repository.GetByIdAsync(id, ct)).ToModel();

        public async Task<SucursalDataModel?> GetByGuidAsync(Guid guid, CancellationToken ct = default)
            => (await _repository.GetByGuidAsync(guid, ct)).ToModel();

        public async Task<SucursalDataModel?> GetByCodigoAsync(string codigo, CancellationToken ct = default)
            => (await _repository.GetByCodigoAsync(codigo, ct)).ToModel();

        public async Task<IEnumerable<SucursalDataModel>> GetAllAsync(CancellationToken ct = default)
            => (await _repository.GetAllAsync(ct)).ToModelList();

        public async Task<SucursalDataModel> AddAsync(SucursalDataModel model, CancellationToken ct = default)
        {
            var entity = model.ToEntity()!;
            if (entity.SucursalGuid == Guid.Empty) entity.SucursalGuid = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(entity.CreadoPorUsuario)) entity.CreadoPorUsuario = "Sistema";
            if (string.IsNullOrWhiteSpace(entity.ServicioOrigen)) entity.ServicioOrigen = "sucursales-service";
            entity.FechaRegistroUtc = DateTime.UtcNow;
            var added = await _repository.AddAsync(entity, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return added.ToModel()!;
        }

        public async Task UpdateAsync(SucursalDataModel model, CancellationToken ct = default)
        {
            var entity = await _repository.GetByIdAsync(model.IdSucursal, ct);
            if (entity == null) return;

            // Actualizamos todos los campos editables de la sucursal.
            entity.CodigoSucursal = model.CodigoSucursal;
            entity.NombreSucursal = model.NombreSucursal;
            entity.DescripcionSucursal = model.DescripcionSucursal;
            entity.DescripcionCorta = model.DescripcionCorta;
            entity.TipoAlojamiento = model.TipoAlojamiento;
            entity.Estrellas = model.Estrellas;
            entity.CategoriaViaje = model.CategoriaViaje;
            entity.Pais = model.Pais;
            entity.Provincia = model.Provincia;
            entity.Ciudad = model.Ciudad;
            entity.Ubicacion = model.Ubicacion;
            entity.Direccion = model.Direccion;
            entity.CodigoPostal = model.CodigoPostal;
            entity.Telefono = model.Telefono;
            entity.Correo = model.Correo;
            entity.Latitud = model.Latitud;
            entity.Longitud = model.Longitud;
            entity.HoraCheckin = model.HoraCheckin;
            entity.HoraCheckout = model.HoraCheckout;
            entity.CheckinAnticipado = model.CheckinAnticipado;
            entity.CheckoutTardio = model.CheckoutTardio;
            entity.AceptaNinos = model.AceptaNinos;
            entity.EdadMinimaHuesped = model.EdadMinimaHuesped;
            entity.PermiteMascotas = model.PermiteMascotas;
            entity.SePermiteFumar = model.SePermiteFumar;
            entity.EstadoSucursal = model.EstadoSucursal;
            entity.ModificadoPorUsuario = model.ModificadoPorUsuario ?? "Sistema";
            entity.FechaModificacionUtc = DateTime.UtcNow;

            await _repository.UpdateAsync(entity, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task UpdatePoliticasAsync(int id, SucursalDataModel politicas, CancellationToken ct = default)
        {
            var entity = politicas.ToEntity()!;
            entity.ModificadoPorUsuario = politicas.ModificadoPorUsuario ?? "Sistema";
            await _repository.UpdatePoliticasAsync(id, entity, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task InhabilitarAsync(int id, string motivo, string usuario, CancellationToken ct = default)
        {
            await _repository.InhabilitarAsync(id, motivo, usuario, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            await _repository.DeleteAsync(id, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
