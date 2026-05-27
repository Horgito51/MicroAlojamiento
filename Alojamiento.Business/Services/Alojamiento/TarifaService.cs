using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alojamiento.Business.DTOs.Alojamiento;
using Alojamiento.Business.Exceptions;
using Alojamiento.Business.Interfaces.Alojamiento;
using Alojamiento.Business.Mappers.Alojamiento;
using Alojamiento.Business.Validators.Alojamiento; // Si existiera TarifaValidator, de lo contrario se puede omitir o crear básico
using Alojamiento.DataAccess.Context;
using Alojamiento.DataManagement.Alojamiento.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Alojamiento.Business.Services.Alojamiento
{
    public class TarifaService : ITarifaService
    {
        private readonly ITarifaDataService _tarifaDataService;
        private readonly AlojamientoDbContext _context;

        public TarifaService(ITarifaDataService tarifaDataService, AlojamientoDbContext context)
        {
            _tarifaDataService = tarifaDataService;
            _context = context;
        }

        public async Task<TarifaDTO> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var dataModel = await _tarifaDataService.GetByIdAsync(id, ct);
            if (dataModel == null)
                throw new NotFoundException("TAR-001", $"No se encontró la tarifa con ID {id}.");
            return dataModel.ToDto();
        }

        public async Task<TarifaDTO> GetByGuidAsync(Guid guid, CancellationToken ct = default)
        {
            var dataModel = await _tarifaDataService.GetByGuidAsync(guid, ct);
            if (dataModel == null)
                throw new NotFoundException("TAR-002", $"No se encontró la tarifa con GUID {guid}.");
            return dataModel.ToDto();
        }

        public async Task<IEnumerable<TarifaDTO>> GetAllAsync(CancellationToken ct = default)
        {
            var pagedResult = await _tarifaDataService.GetAllPagedAsync(1, int.MaxValue, ct);
            return pagedResult.Items.ToDtoList();
        }

        public async Task<TarifaDTO> CreateAsync(TarifaCreateDTO tarifaCreateDto, CancellationToken ct = default)
        {
            var tarifaDto = new TarifaDTO
            {
                CodigoTarifa = tarifaCreateDto.CodigoTarifa?.Trim() ?? string.Empty,
                IdSucursal = tarifaCreateDto.IdSucursal,
                IdTipoHabitacion = tarifaCreateDto.IdTipoHabitacion,
                NombreTarifa = tarifaCreateDto.NombreTarifa?.Trim() ?? string.Empty,
                CanalTarifa = tarifaCreateDto.CanalTarifa?.Trim() ?? string.Empty,
                FechaInicio = tarifaCreateDto.FechaInicio,
                FechaFin = tarifaCreateDto.FechaFin,
                PrecioPorNoche = tarifaCreateDto.PrecioPorNoche,
                PorcentajeIva = tarifaCreateDto.PorcentajeIva,
                MinNoches = tarifaCreateDto.MinNoches,
                MaxNoches = tarifaCreateDto.MaxNoches,
                PermitePortalPublico = tarifaCreateDto.PermitePortalPublico,
                Prioridad = tarifaCreateDto.Prioridad,
                EstadoTarifa = (tarifaCreateDto.EstadoTarifa ?? string.Empty).Trim().ToUpperInvariant()
            };

            TarifaValidator.Validate(tarifaDto);
            await EnsureSucursalExisteAsync(tarifaDto.IdSucursal, ct);
            await EnsureTipoHabitacionExisteAsync(tarifaDto.IdTipoHabitacion, ct);
            await EnsureCodigoUnicoAsync(tarifaDto.CodigoTarifa, null, ct);
            await EnsureSinSolapamientoAsync(tarifaDto.IdSucursal, tarifaDto.IdTipoHabitacion, tarifaDto.CanalTarifa, tarifaDto.FechaInicio, tarifaDto.FechaFin, null, ct);

            var dataModel = tarifaDto.ToDataModel();
            var created = await _tarifaDataService.AddAsync(dataModel, ct);
            return created.ToDto();
        }

public async Task UpdateAsync(TarifaUpdateDTO tarifaUpdateDto, CancellationToken ct = default)
        {
            var existing = await _tarifaDataService.GetByIdAsync(tarifaUpdateDto.IdTarifa, ct);
            if (existing == null)
                throw new NotFoundException("TAR-004", $"No se encontró la tarifa con ID {tarifaUpdateDto.IdTarifa}.");

            var tarifaDto = new TarifaDTO
            {
                IdTarifa = tarifaUpdateDto.IdTarifa,
                CodigoTarifa = tarifaUpdateDto.CodigoTarifa?.Trim() ?? string.Empty,
                IdSucursal = tarifaUpdateDto.IdSucursal,
                IdTipoHabitacion = tarifaUpdateDto.IdTipoHabitacion,
                NombreTarifa = tarifaUpdateDto.NombreTarifa?.Trim() ?? string.Empty,
                CanalTarifa = tarifaUpdateDto.CanalTarifa?.Trim() ?? string.Empty,
                FechaInicio = tarifaUpdateDto.FechaInicio,
                FechaFin = tarifaUpdateDto.FechaFin,
                PrecioPorNoche = tarifaUpdateDto.PrecioPorNoche,
                PorcentajeIva = tarifaUpdateDto.PorcentajeIva,
                MinNoches = tarifaUpdateDto.MinNoches,
                MaxNoches = tarifaUpdateDto.MaxNoches,
                PermitePortalPublico = tarifaUpdateDto.PermitePortalPublico,
                Prioridad = tarifaUpdateDto.Prioridad,
                EstadoTarifa = (tarifaUpdateDto.EstadoTarifa ?? string.Empty).Trim().ToUpperInvariant()
            };

            TarifaValidator.Validate(tarifaDto);
            await EnsureSucursalExisteAsync(tarifaDto.IdSucursal, ct);
            await EnsureTipoHabitacionExisteAsync(tarifaDto.IdTipoHabitacion, ct);
            await EnsureCodigoUnicoAsync(tarifaDto.CodigoTarifa, tarifaDto.IdTarifa, ct);
            await EnsureSinSolapamientoAsync(tarifaDto.IdSucursal, tarifaDto.IdTipoHabitacion, tarifaDto.CanalTarifa, tarifaDto.FechaInicio, tarifaDto.FechaFin, tarifaDto.IdTarifa, ct);

            existing.CodigoTarifa = tarifaDto.CodigoTarifa;
            existing.IdSucursal = tarifaDto.IdSucursal;
            existing.IdTipoHabitacion = tarifaDto.IdTipoHabitacion;
            existing.NombreTarifa = tarifaDto.NombreTarifa;
            existing.CanalTarifa = tarifaDto.CanalTarifa;
            existing.FechaInicio = tarifaDto.FechaInicio;
            existing.FechaFin = tarifaDto.FechaFin;
            existing.PrecioPorNoche = tarifaDto.PrecioPorNoche;
            existing.PorcentajeIva = tarifaDto.PorcentajeIva;
            existing.MinNoches = tarifaDto.MinNoches;
            existing.MaxNoches = tarifaDto.MaxNoches;
            existing.PermitePortalPublico = tarifaDto.PermitePortalPublico;
            existing.Prioridad = tarifaDto.Prioridad;
            existing.EstadoTarifa = tarifaDto.EstadoTarifa;
            existing.ModificadoPorUsuario = "Sistema";
            existing.FechaModificacionUtc = DateTime.UtcNow;

            await _tarifaDataService.UpdateAsync(existing, ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var existing = await _tarifaDataService.GetByIdAsync(id, ct);
            if (existing == null)
                throw new NotFoundException("TAR-005", $"No se encontró la tarifa con ID {id}.");
            await _tarifaDataService.DeleteAsync(id, ct);
        }

        public async Task<IEnumerable<TarifaDTO>> GetBySucursalAsync(int idSucursal, CancellationToken ct = default)
        {
            var dataModels = await _tarifaDataService.GetBySucursalAsync(idSucursal, ct);
            return dataModels.ToDtoList();
        }

        public async Task<TarifaDTO> GetTarifaVigenteAsync(int idSucursal, int idTipoHabitacion, DateTime fecha, string? canal = null, CancellationToken ct = default)
        {
            var dataModel = await _tarifaDataService.GetTarifaVigenteAsync(idSucursal, idTipoHabitacion, fecha, canal, ct);
            if (dataModel == null)
                throw new NotFoundException("TAR-006", "No hay tarifa vigente para los parámetros especificados.");
            return dataModel.ToDto();
        }

        public async Task<TarifaDTO?> GetTarifaVigenteRangoOrDefaultAsync(int idSucursal, int idTipoHabitacion, DateTime fechaInicio, DateTime fechaFin, string? canal = null, CancellationToken ct = default)
        {
            var dataModel = await _tarifaDataService.GetTarifaVigenteRangoAsync(idSucursal, idTipoHabitacion, fechaInicio, fechaFin, canal, ct);
            return dataModel?.ToDto();
        }

        public async Task DesactivarAsync(int id, string usuario, CancellationToken ct = default)
        {
            var existing = await _tarifaDataService.GetByIdAsync(id, ct);
            if (existing == null)
                throw new NotFoundException("TAR-007", $"No se encontró la tarifa con ID {id}.");
            await _tarifaDataService.DesactivarAsync(id, usuario, ct);
        }

        private async Task EnsureSucursalExisteAsync(int idSucursal, CancellationToken ct)
        {
            var exists = await _context.Sucursales.AnyAsync(s =>
                s.IdSucursal == idSucursal &&
                s.EstadoSucursal != "INA" &&
                !s.EsEliminado, ct);

            if (!exists)
                throw new NotFoundException("SUC-001", $"No se encontro la sucursal con ID {idSucursal}.");
        }

        private async Task EnsureTipoHabitacionExisteAsync(int idTipoHabitacion, CancellationToken ct)
        {
            var exists = await _context.TiposHabitacion.AnyAsync(t =>
                t.IdTipoHabitacion == idTipoHabitacion &&
                t.EstadoTipoHabitacion != "INA" &&
                !t.EsEliminado, ct);

            if (!exists)
                throw new NotFoundException("TIP-001", $"No se encontro el tipo de habitacion con ID {idTipoHabitacion}.");
        }

        private async Task EnsureCodigoUnicoAsync(string codigoTarifa, int? idTarifa, CancellationToken ct)
        {
            var normalized = (codigoTarifa ?? string.Empty).Trim().ToUpperInvariant();
            var exists = await _context.Tarifas.AnyAsync(t =>
                t.CodigoTarifa.Trim().ToUpper() == normalized &&
                (!idTarifa.HasValue || t.IdTarifa != idTarifa.Value) &&
                !t.EsEliminado, ct);

            if (exists)
                throw new ConflictException("Ya existe una tarifa registrada con ese codigo.");
        }

        private async Task EnsureSinSolapamientoAsync(
            int idSucursal,
            int idTipoHabitacion,
            string canalTarifa,
            DateTime fechaInicio,
            DateTime fechaFin,
            int? idTarifa,
            CancellationToken ct)
        {
            var canal = (canalTarifa ?? string.Empty).Trim().ToUpperInvariant();
            var overlaps = await _context.Tarifas.AnyAsync(t =>
                t.IdSucursal == idSucursal &&
                t.IdTipoHabitacion == idTipoHabitacion &&
                t.CanalTarifa.Trim().ToUpper() == canal &&
                t.EstadoTarifa == "ACT" &&
                !t.EsEliminado &&
                (!idTarifa.HasValue || t.IdTarifa != idTarifa.Value) &&
                fechaInicio <= t.FechaFin &&
                fechaFin >= t.FechaInicio, ct);

            if (overlaps)
                throw new ConflictException("Ya existe una tarifa activa solapada para la sucursal, tipo de habitacion y canal indicados.");
        }
    }
}
