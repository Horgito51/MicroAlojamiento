using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using Alojamiento.Business.DTOs.Alojamiento;
using Alojamiento.Business.Exceptions;
using Alojamiento.Business.Interfaces.Alojamiento;
using Alojamiento.Business.Mappers.Alojamiento;
using Alojamiento.DataAccess.Context;
using Alojamiento.DataManagement.Alojamiento.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Alojamiento.Business.Services.Alojamiento
{
    public class CatalogoServicioService : ICatalogoServicioService
    {
        private readonly ICatalogoServicioDataService _dataService;
        private readonly AlojamientoDbContext _context;

        public CatalogoServicioService(ICatalogoServicioDataService dataService, AlojamientoDbContext context)
        {
            _dataService = dataService;
            _context = context;
        }

        public async Task<IEnumerable<CatalogoServicioDTO>> GetAllAsync(CancellationToken ct = default)
            => (await _dataService.GetAllAsync(ct)).ToDtoList();

        public async Task<CatalogoServicioDTO> GetByIdAsync(int id, CancellationToken ct = default)
            => (await _dataService.GetByIdAsync(id, ct)).ToDto()
               ?? throw new NotFoundException("CAT-001", $"No se encontró el catálogo de servicio con ID {id}.");

        public async Task<CatalogoServicioDTO> CreateAsync(CatalogoServicioCreateDTO dto, CancellationToken ct = default)
        {
            var tiposValidos = new[] { "AME", "SRV" };
            if (string.IsNullOrWhiteSpace(dto.TipoCatalogo) || !tiposValidos.Contains(dto.TipoCatalogo.ToUpper()))
                throw new ValidationException("CAT-002", "El tipo de catálogo debe ser 'AME' (amenidad) o 'SRV' (servicio).");

            dto.TipoCatalogo = dto.TipoCatalogo.ToUpper();
            await EnsureSucursalExisteAsync(dto.IdSucursal, ct);
            await EnsureCodigoUnicoAsync(dto.CodigoCatalogo, null, ct);

            var created = await _dataService.AddAsync(dto.ToDataModel()!, ct);
            return created.ToDto()!;
        }

public async Task UpdateAsync(CatalogoServicioUpdateDTO dto, CancellationToken ct = default)
        {
            var existing = await _dataService.GetByIdAsync(dto.IdCatalogo, ct);
            if (existing == null)
                throw new NotFoundException("CAT-001", $"No se encontró el catálogo de servicio con ID {dto.IdCatalogo}.");

            var tiposValidos = new[] { "AME", "SRV" };
            if (!string.IsNullOrWhiteSpace(dto.TipoCatalogo) && !tiposValidos.Contains(dto.TipoCatalogo.ToUpper()))
                throw new ValidationException("CAT-003", "El tipo de catálogo debe ser 'AME' (amenidad) o 'SRV' (servicio).");

            if (!string.IsNullOrWhiteSpace(dto.TipoCatalogo))
                dto.TipoCatalogo = dto.TipoCatalogo.ToUpper();

            if (existing.EsEliminado)
                throw new ConflictException("No se puede actualizar un item eliminado logicamente.");
            await EnsureSucursalExisteAsync(dto.IdSucursal, ct);
            await EnsureCodigoUnicoAsync(dto.CodigoCatalogo, dto.IdCatalogo, ct);

            existing.IdSucursal = dto.IdSucursal ?? existing.IdSucursal;
            existing.CodigoCatalogo = dto.CodigoCatalogo;
            existing.NombreCatalogo = dto.NombreCatalogo;
            existing.TipoCatalogo = dto.TipoCatalogo;
            existing.CategoriaCatalogo = dto.CategoriaCatalogo ?? string.Empty;
            existing.DescripcionCatalogo = dto.DescripcionCatalogo ?? string.Empty;
            existing.PrecioBase = dto.PrecioBase;
            existing.AplicaIva = dto.AplicaIva;
            existing.Disponible24h = dto.Disponible24h;
            existing.HoraInicio = dto.HoraInicio;
            existing.HoraFin = dto.HoraFin;
            existing.IconoUrl = dto.IconoUrl ?? string.Empty;
            existing.EstadoCatalogo = dto.EstadoCatalogo;
            existing.ModificadoPorUsuario = "Sistema";
            existing.FechaModificacionUtc = DateTime.UtcNow;

            await _dataService.UpdateAsync(existing, ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            _ = await GetByIdAsync(id, ct);
            await _dataService.DeleteAsync(id, ct);
        }

        private async Task EnsureSucursalExisteAsync(int? idSucursal, CancellationToken ct)
        {
            if (!idSucursal.HasValue)
                return;

            var exists = await _context.Sucursales.AnyAsync(s =>
                s.IdSucursal == idSucursal.Value &&
                s.EstadoSucursal != "INA" &&
                !s.EsEliminado, ct);

            if (!exists)
                throw new NotFoundException("SUC-001", $"No se encontro la sucursal con ID {idSucursal.Value}.");
        }

        private async Task EnsureCodigoUnicoAsync(string codigoCatalogo, int? idCatalogo, CancellationToken ct)
        {
            var normalized = (codigoCatalogo ?? string.Empty).Trim().ToUpperInvariant();
            var exists = await _context.CatalogoServicios.AnyAsync(c =>
                c.CodigoCatalogo.Trim().ToUpper() == normalized &&
                (!idCatalogo.HasValue || c.IdCatalogo != idCatalogo.Value) &&
                !c.EsEliminado, ct);

            if (exists)
                throw new ConflictException("Ya existe un item de catalogo registrado con ese codigo.");
        }
    }
}
