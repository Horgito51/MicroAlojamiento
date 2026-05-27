using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alojamiento.Business.Common;
using Alojamiento.Business.DTOs.Valoraciones;
using Alojamiento.Business.Exceptions;
using Alojamiento.Business.Interfaces.Valoraciones;
using Alojamiento.Business.Mappers.Valoraciones;
using Alojamiento.DataAccess.Context;
using Alojamiento.DataManagement.Valoraciones.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Alojamiento.Business.Services.Valoraciones
{
    public class ValoracionService : IValoracionService
    {
        private static readonly HashSet<string> EstadosPermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "PEN", "PUB", "OCU", "REP"
        };

        private static readonly HashSet<string> TiposViajePermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "pareja", "familia", "negocios", "amigos", "solo"
        };

        private readonly IValoracionDataService _valoracionDataService;
        private readonly AlojamientoDbContext _context;

        public ValoracionService(IValoracionDataService valoracionDataService, AlojamientoDbContext context)
        {
            _valoracionDataService = valoracionDataService;
            _context = context;
        }

        public async Task<ValoracionDTO> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var dataModel = await _valoracionDataService.GetByIdAsync(id, ct);
            if (dataModel == null)
                throw new NotFoundException("VAL-001", $"No se encontro la valoracion con ID {id}.");
            return dataModel.ToDto();
        }

        public async Task<ValoracionDTO> GetByGuidAsync(Guid guid, CancellationToken ct = default)
        {
            var dataModel = await _valoracionDataService.GetByGuidAsync(guid, ct);
            if (dataModel == null)
                throw new NotFoundException("VAL-002", $"No se encontro la valoracion con GUID {guid}.");
            return dataModel.ToDto();
        }

        public async Task<PagedResult<ValoracionDTO>> GetByFiltroAsync(ValoracionFiltroDTO filtro, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var pagedData = await _valoracionDataService.GetByFiltroAsync(filtro.ToDataModel(), pageNumber, pageSize, ct);
            return new PagedResult<ValoracionDTO>
            {
                Items = pagedData.Items.ToDtoList(),
                TotalCount = pagedData.TotalCount,
                PageNumber = pagedData.PageNumber,
                PageSize = pagedData.PageSize
            };
        }

        public async Task<ValoracionDTO> CreateAsync(ValoracionDTO valoracionDto, CancellationToken ct = default)
        {
            static void ValidateScore(string field, decimal score)
            {
                if (score < 0 || score > 10)
                    throw new BusinessException($"La puntuacion '{field}' debe estar entre 0 y 10.");
            }

            await ValidateSucursalHabitacionAsync(valoracionDto.IdSucursal, valoracionDto.IdHabitacion, ct);

            var scores = new List<decimal>();
            if (valoracionDto.PuntuacionLimpieza.HasValue) scores.Add(valoracionDto.PuntuacionLimpieza.Value);
            if (valoracionDto.PuntuacionConfort.HasValue) scores.Add(valoracionDto.PuntuacionConfort.Value);
            if (valoracionDto.PuntuacionUbicacion.HasValue) scores.Add(valoracionDto.PuntuacionUbicacion.Value);
            if (valoracionDto.PuntuacionInstalaciones.HasValue) scores.Add(valoracionDto.PuntuacionInstalaciones.Value);
            if (valoracionDto.PuntuacionPersonal.HasValue) scores.Add(valoracionDto.PuntuacionPersonal.Value);
            if (valoracionDto.PuntuacionCalidadPrecio.HasValue) scores.Add(valoracionDto.PuntuacionCalidadPrecio.Value);

            if (scores.Count == 0)
                throw new ValidationException("VAL-004", "Se requiere al menos una puntuacion para registrar la valoracion.");

            valoracionDto.PuntuacionGeneral = Math.Round(scores.Sum() / scores.Count, 1, MidpointRounding.AwayFromZero);
            ValidateScore(nameof(valoracionDto.PuntuacionGeneral), valoracionDto.PuntuacionGeneral);

            if (valoracionDto.PuntuacionLimpieza.HasValue)
                ValidateScore(nameof(valoracionDto.PuntuacionLimpieza), valoracionDto.PuntuacionLimpieza.Value);
            if (valoracionDto.PuntuacionConfort.HasValue)
                ValidateScore(nameof(valoracionDto.PuntuacionConfort), valoracionDto.PuntuacionConfort.Value);
            if (valoracionDto.PuntuacionUbicacion.HasValue)
                ValidateScore(nameof(valoracionDto.PuntuacionUbicacion), valoracionDto.PuntuacionUbicacion.Value);
            if (valoracionDto.PuntuacionInstalaciones.HasValue)
                ValidateScore(nameof(valoracionDto.PuntuacionInstalaciones), valoracionDto.PuntuacionInstalaciones.Value);
            if (valoracionDto.PuntuacionPersonal.HasValue)
                ValidateScore(nameof(valoracionDto.PuntuacionPersonal), valoracionDto.PuntuacionPersonal.Value);
            if (valoracionDto.PuntuacionCalidadPrecio.HasValue)
                ValidateScore(nameof(valoracionDto.PuntuacionCalidadPrecio), valoracionDto.PuntuacionCalidadPrecio.Value);

            valoracionDto.EstadoValoracion = NormalizeEstado(string.IsNullOrWhiteSpace(valoracionDto.EstadoValoracion)
                ? "PEN"
                : valoracionDto.EstadoValoracion);
            valoracionDto.PublicadaEnPortal = valoracionDto.EstadoValoracion == "PUB";
            valoracionDto.TipoViaje = NormalizeTipoViaje(valoracionDto.TipoViaje);

            var dataModel = valoracionDto.ToDataModel();
            var created = await _valoracionDataService.AddAsync(dataModel, ct);
            return created.ToDto();
        }

        public async Task UpdateAsync(ValoracionDTO valoracionDto, CancellationToken ct = default)
        {
            var existing = await _valoracionDataService.GetByIdAsync(valoracionDto.IdValoracion, ct);
            if (existing == null)
                throw new NotFoundException("VAL-004", $"No se encontro la valoracion con ID {valoracionDto.IdValoracion}.");
            var dataModel = valoracionDto.ToDataModel();
            await _valoracionDataService.UpdateAsync(dataModel, ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var existing = await _valoracionDataService.GetByIdAsync(id, ct);
            if (existing == null)
                throw new NotFoundException("VAL-005", $"No se encontro la valoracion con ID {id}.");
            await _valoracionDataService.DeleteAsync(id, ct);
        }

        public async Task ModerarAsync(int idValoracion, string nuevoEstado, string motivo, string moderador, CancellationToken ct = default)
        {
            var existing = await _valoracionDataService.GetByIdAsync(idValoracion, ct);
            if (existing == null)
                throw new NotFoundException("VAL-006", $"No se encontro la valoracion con ID {idValoracion}.");
            nuevoEstado = NormalizeEstado(nuevoEstado);
            await _valoracionDataService.ModerarAsync(idValoracion, nuevoEstado, motivo, moderador, ct);
        }

        public async Task ResponderAsync(int idValoracion, string respuesta, string usuario, CancellationToken ct = default)
        {
            var existing = await _valoracionDataService.GetByIdAsync(idValoracion, ct);
            if (existing == null)
                throw new NotFoundException("VAL-007", $"No se encontro la valoracion con ID {idValoracion}.");
            await _valoracionDataService.ResponderAsync(idValoracion, respuesta, usuario, ct);
        }

        public async Task<bool> ExistsByEstadiaAsync(int idEstadia, CancellationToken ct = default)
        {
            return await _valoracionDataService.ExistsByEstadiaAsync(idEstadia, ct);
        }

        private async Task ValidateSucursalHabitacionAsync(int idSucursal, int? idHabitacion, CancellationToken ct)
        {
            var sucursalExists = await _context.Sucursales
                .AsNoTracking()
                .AnyAsync(s => s.IdSucursal == idSucursal && !s.EsEliminado, ct);

            if (!sucursalExists)
                throw new ValidationException("VAL-008", $"No existe una sucursal activa asociada al ID {idSucursal}.");

            if (!idHabitacion.HasValue)
                return;

            var habitacion = await _context.Habitaciones
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.IdHabitacion == idHabitacion.Value && !h.EsEliminado, ct);

            if (habitacion == null)
                throw new ValidationException("VAL-009", $"No existe una habitacion activa asociada al ID {idHabitacion.Value}.");

            if (habitacion.IdSucursal != idSucursal)
                throw new ValidationException("VAL-010", "La habitacion de la valoracion no pertenece a la sucursal indicada.");
        }

        private static string NormalizeEstado(string? estado)
        {
            var normalized = (estado ?? string.Empty).Trim().ToUpperInvariant();
            if (!EstadosPermitidos.Contains(normalized))
                throw new ValidationException("VAL-011", "Estado de valoracion invalido. Use PEN, PUB, OCU o REP.");

            return normalized;
        }

        private static string NormalizeTipoViaje(string? tipoViaje)
        {
            var normalized = (tipoViaje ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
                return string.Empty;

            if (!TiposViajePermitidos.Contains(normalized))
                throw new ValidationException("VAL-012", "Tipo de viaje invalido. Use pareja, familia, negocios, amigos o solo.");

            return normalized;
        }
    }
}
