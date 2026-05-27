using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Alojamiento.Business.Common;
using Alojamiento.Business.DTOs.Alojamiento;
using Alojamiento.Business.Exceptions;
using Alojamiento.Business.Interfaces.Alojamiento;
using Alojamiento.Business.Mappers.Alojamiento;
using Alojamiento.Business.Validators.Alojamiento;
using Alojamiento.DataAccess.Context;
using Alojamiento.DataManagement.Alojamiento.Interfaces;

namespace Alojamiento.Business.Services.Alojamiento
{
    public class HabitacionService : IHabitacionService
    {
        private readonly IHabitacionDataService _habitacionDataService;
        private readonly ITarifaService _tarifaService;
        private readonly AlojamientoDbContext _context;

        public HabitacionService(IHabitacionDataService habitacionDataService, ITarifaService tarifaService, AlojamientoDbContext context)
        {
            _habitacionDataService = habitacionDataService;
            _tarifaService = tarifaService;
            _context = context;
        }

        private async Task<decimal> GetPrecioVigente(int idSucursal, int idTipoHabitacion, decimal precioBaseActual)
        {
            try
            {
                var tarifa = await _tarifaService.GetTarifaVigenteAsync(idSucursal, idTipoHabitacion, DateTime.UtcNow);
                return tarifa.PrecioPorNoche;
            }
            catch
            {
                return precioBaseActual;
            }
        }

        public async Task<HabitacionDTO> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var dataModel = await _habitacionDataService.GetByIdAsync(id, ct);
            if (dataModel == null)
                throw new NotFoundException("HAB-001", $"No se encontró la habitación con ID {id}.");
            
            var dto = dataModel.ToDto();
            dto.PrecioBase = await GetPrecioVigente(dto.IdSucursal, dto.IdTipoHabitacion, dto.PrecioBase);
            return dto;
        }

        public async Task<HabitacionDTO> GetByGuidAsync(Guid guid, CancellationToken ct = default)
        {
            var dataModel = await _habitacionDataService.GetByGuidAsync(guid, ct);
            if (dataModel == null)
                throw new NotFoundException("HAB-002", $"No se encontró la habitación con GUID {guid}.");
            
            var dto = dataModel.ToDto();
            dto.PrecioBase = await GetPrecioVigente(dto.IdSucursal, dto.IdTipoHabitacion, dto.PrecioBase);
            return dto;
        }

        public async Task<IEnumerable<HabitacionDTO>> GetAllAsync(CancellationToken ct = default)
        {
            var pagedResult = await _habitacionDataService.GetAllPagedAsync(1, int.MaxValue, ct);
            var items = pagedResult.Items.ToDtoList();
            
            foreach (var item in items)
            {
                item.PrecioBase = await GetPrecioVigente(item.IdSucursal, item.IdTipoHabitacion, item.PrecioBase);
            }
            
            return items;
        }

        public async Task<HabitacionDTO> CreateAsync(HabitacionCreateDTO habitacionCreateDto, CancellationToken ct = default)
        {
            var habitacionDto = new HabitacionDTO
            {
                IdSucursal = habitacionCreateDto.IdSucursal,
                IdTipoHabitacion = habitacionCreateDto.IdTipoHabitacion,
                NumeroHabitacion = habitacionCreateDto.NumeroHabitacion,
                Piso = habitacionCreateDto.Piso,
                CapacidadHabitacion = habitacionCreateDto.CapacidadHabitacion,
                PrecioBase = habitacionCreateDto.PrecioBase,
                DescripcionHabitacion = habitacionCreateDto.DescripcionHabitacion ?? string.Empty,
                EstadoHabitacion = habitacionCreateDto.EstadoHabitacion
            };

            HabitacionValidator.Validate(habitacionDto);
            await EnsureSucursalExisteAsync(habitacionDto.IdSucursal, ct);
            await EnsureTipoHabitacionExisteAsync(habitacionDto.IdTipoHabitacion, ct);
            await EnsureNumeroUnicoEnSucursalAsync(habitacionDto.IdSucursal, habitacionDto.NumeroHabitacion, null, ct);
            var dataModel = habitacionDto.ToDataModel();
            var created = await _habitacionDataService.AddAsync(dataModel, ct);
            return created.ToDto();
        }

        public async Task UpdateAsync(HabitacionUpdateDTO habitacionUpdateDto, CancellationToken ct = default)
        {
            var existing = await _habitacionDataService.GetByIdAsync(habitacionUpdateDto.IdHabitacion, ct);
            if (existing == null)
                throw new NotFoundException("HAB-003", $"No se encontró la habitación con ID {habitacionUpdateDto.IdHabitacion}.");

            existing.IdSucursal = habitacionUpdateDto.IdSucursal;
            existing.IdTipoHabitacion = habitacionUpdateDto.IdTipoHabitacion;
            existing.NumeroHabitacion = habitacionUpdateDto.NumeroHabitacion;
            existing.Piso = habitacionUpdateDto.Piso;
            existing.CapacidadHabitacion = habitacionUpdateDto.CapacidadHabitacion;
            existing.PrecioBase = habitacionUpdateDto.PrecioBase;
            existing.DescripcionHabitacion = habitacionUpdateDto.DescripcionHabitacion ?? string.Empty;
            existing.EstadoHabitacion = habitacionUpdateDto.EstadoHabitacion;

            HabitacionValidator.Validate(existing.ToDto());
            await EnsureSucursalExisteAsync(existing.IdSucursal, ct);
            await EnsureTipoHabitacionExisteAsync(existing.IdTipoHabitacion, ct);
            await EnsureNumeroUnicoEnSucursalAsync(existing.IdSucursal, existing.NumeroHabitacion, existing.IdHabitacion, ct);

            await _habitacionDataService.UpdateAsync(existing, ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var existing = await _habitacionDataService.GetByIdAsync(id, ct);
            if (existing == null)
                throw new NotFoundException("HAB-004", $"No se encontró la habitación con ID {id}.");
            await EnsureSinReservasActivasAsync(id, ct);
            await _habitacionDataService.DeleteAsync(id, ct);
        }

        public async Task<IEnumerable<HabitacionDTO>> GetBySucursalAsync(int idSucursal, CancellationToken ct = default)
        {
            var dataModels = await _habitacionDataService.GetBySucursalAsync(idSucursal, ct);
            var items = dataModels.ToDtoList();
            foreach (var item in items)
            {
                item.PrecioBase = await GetPrecioVigente(item.IdSucursal, item.IdTipoHabitacion, item.PrecioBase);
            }
            return items;
        }

        public async Task<IEnumerable<HabitacionDTO>> GetByTipoHabitacionAsync(int idTipoHabitacion, CancellationToken ct = default)
        {
            var dataModels = await _habitacionDataService.GetByTipoHabitacionAsync(idTipoHabitacion, ct);
            var items = dataModels.ToDtoList();
            foreach (var item in items)
            {
                item.PrecioBase = await GetPrecioVigente(item.IdSucursal, item.IdTipoHabitacion, item.PrecioBase);
            }
            return items;
        }

        public async Task UpdateEstadoAsync(int id, string nuevoEstado, string usuario, CancellationToken ct = default)
        {
            var estadoNormalizado = (nuevoEstado ?? string.Empty).Trim().ToUpperInvariant();
            var estadosValidos = new[] { "DIS", "OCU", "MNT", "FDS", "INA" };
            if (string.IsNullOrWhiteSpace(estadoNormalizado) || !estadosValidos.Contains(estadoNormalizado))
            {
                var errors = new Dictionary<string, string[]>
                {
                    ["NuevoEstado"] = new[] { $"Estado inválido. Valores permitidos: {string.Join(", ", estadosValidos)}." }
                };
                throw new ValidationException("HAB-006", errors);
            }

            var existing = await _habitacionDataService.GetByIdAsync(id, ct);
            if (existing == null)
                throw new NotFoundException("HAB-005", $"No se encontró la habitación con ID {id}.");

            await _habitacionDataService.UpdateEstadoAsync(id, estadoNormalizado, usuario, ct);
        }

        public async Task<IEnumerable<HabitacionDTO>> GetDisponiblesAsync(int idSucursal, DateTime inicio, DateTime fin, CancellationToken ct = default)
        {
            var dataModels = await _habitacionDataService.GetDisponiblesAsync(idSucursal, inicio, fin, ct);
            var estadosNoReservables = new[] { "MNT", "FDS", "OCU", "INA" };
            var items = dataModels
                .ToDtoList()
                .Where(h => !estadosNoReservables.Contains((h.EstadoHabitacion ?? string.Empty).Trim().ToUpperInvariant()))
                .ToList();
            foreach (var item in items)
            {
                item.PrecioBase = await GetPrecioVigente(item.IdSucursal, item.IdTipoHabitacion, item.PrecioBase);
            }
            return items;
        }

        private async Task EnsureSinReservasActivasAsync(int idHabitacion, CancellationToken ct)
        {
            await Task.CompletedTask;
        }

        private async Task EnsureNumeroUnicoEnSucursalAsync(int idSucursal, string numeroHabitacion, int? idHabitacion, CancellationToken ct)
        {
            var numeroNormalizado = (numeroHabitacion ?? string.Empty).Trim().ToUpperInvariant();
            var existe = await _context.Habitaciones.AnyAsync(h =>
                h.IdSucursal == idSucursal &&
                h.NumeroHabitacion.Trim().ToUpper() == numeroNormalizado &&
                (!idHabitacion.HasValue || h.IdHabitacion != idHabitacion.Value) &&
                !h.EsEliminado, ct);

            if (existe)
                throw new ConflictException("Ya existe una habitacion con ese numero en la sucursal.");
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
    }
}
