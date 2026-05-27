using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Alojamiento.Business.DTOs.Alojamiento;
using Alojamiento.Business.Exceptions;
using Alojamiento.Business.Interfaces.Alojamiento;
using Alojamiento.Business.Mappers.Alojamiento;
using Alojamiento.DataAccess.Context;
using Alojamiento.DataAccess.Entities.Alojamiento;
using Alojamiento.DataManagement.Alojamiento.Interfaces;

namespace Alojamiento.Business.Services.Alojamiento
{
    public class TipoHabitacionService : ITipoHabitacionService
    {
        private readonly ITipoHabitacionDataService _tipoHabitacionDataService;
        private readonly AlojamientoDbContext _context;

        public TipoHabitacionService(ITipoHabitacionDataService tipoHabitacionDataService, AlojamientoDbContext context)
        {
            _tipoHabitacionDataService = tipoHabitacionDataService;
            _context = context;
        }

        public async Task<TipoHabitacionDTO> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var dataModel = await _tipoHabitacionDataService.GetByIdAsync(id, ct);
            if (dataModel == null)
                throw new NotFoundException("TIP-001", $"No se encontró el tipo de habitación con ID {id}.");
            return dataModel.ToDto();
        }

        public async Task<TipoHabitacionDTO> GetByGuidAsync(Guid guid, CancellationToken ct = default)
        {
            var dataModel = await _tipoHabitacionDataService.GetByGuidAsync(guid, ct);
            if (dataModel == null)
                throw new NotFoundException("TIP-002", $"No se encontró el tipo de habitación con GUID {guid}.");
            return dataModel.ToDto();
        }

        public async Task<IEnumerable<TipoHabitacionDTO>> GetAllAsync(CancellationToken ct = default)
        {
            var pagedResult = await _tipoHabitacionDataService.GetAllPagedAsync(1, int.MaxValue, ct);
            return pagedResult.Items.ToDtoList();
        }

        public async Task<TipoHabitacionDTO> CreateAsync(TipoHabitacionCreateDTO tipoCreateDto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tipoCreateDto.CodigoTipoHabitacion))
                throw new ValidationException("TIP-003", "El código del tipo de habitación es obligatorio.");
            if (string.IsNullOrWhiteSpace(tipoCreateDto.NombreTipoHabitacion))
                throw new ValidationException("TIP-003", "El nombre del tipo de habitación es obligatorio.");

            ValidateTipo(tipoCreateDto.CapacidadAdultos, tipoCreateDto.CapacidadNinos, tipoCreateDto.CapacidadTotal, tipoCreateDto.EstadoTipoHabitacion);
            await EnsureCodigoUnicoAsync(tipoCreateDto.CodigoTipoHabitacion, null, ct);
            await EnsureNombreUnicoAsync(tipoCreateDto.NombreTipoHabitacion, null, ct);

            var dataModel = tipoCreateDto.ToDataModel();
            var created = await _tipoHabitacionDataService.AddAsync(dataModel, ct);
            await ReplaceImagenesAsync(created.IdTipoHabitacion, tipoCreateDto.Imagenes, ct);
            return (await _tipoHabitacionDataService.GetByIdAsync(created.IdTipoHabitacion, ct)).ToDto();
        }

        public async Task UpdateAsync(TipoHabitacionUpdateDTO tipoUpdateDto, CancellationToken ct = default)
        {
            var existing = await _tipoHabitacionDataService.GetByIdAsync(tipoUpdateDto.IdTipoHabitacion, ct);
            if (existing == null)
                throw new NotFoundException("TIP-004", $"No se encontró el tipo de habitación con ID {tipoUpdateDto.IdTipoHabitacion}.");
            if (string.IsNullOrWhiteSpace(tipoUpdateDto.CodigoTipoHabitacion))
                throw new ValidationException("TIP-003", "El codigo del tipo de habitacion es obligatorio.");
            if (string.IsNullOrWhiteSpace(tipoUpdateDto.NombreTipoHabitacion))
                throw new ValidationException("TIP-003", "El nombre del tipo de habitacion es obligatorio.");
            ValidateTipo(tipoUpdateDto.CapacidadAdultos, tipoUpdateDto.CapacidadNinos, tipoUpdateDto.CapacidadTotal, tipoUpdateDto.EstadoTipoHabitacion);

            var codigoActual = (existing.CodigoTipoHabitacion ?? string.Empty).Trim().ToUpperInvariant();
            var codigoNuevo = (tipoUpdateDto.CodigoTipoHabitacion ?? string.Empty).Trim().ToUpperInvariant();
            if (codigoNuevo != codigoActual)
            {
                await EnsureCodigoUnicoAsync(tipoUpdateDto.CodigoTipoHabitacion, tipoUpdateDto.IdTipoHabitacion, ct);
            }

            var nombreActual = (existing.NombreTipoHabitacion ?? string.Empty).Trim().ToUpperInvariant();
            var nombreNuevo = (tipoUpdateDto.NombreTipoHabitacion ?? string.Empty).Trim().ToUpperInvariant();
            if (nombreNuevo != nombreActual)
            {
                await EnsureNombreUnicoAsync(tipoUpdateDto.NombreTipoHabitacion, tipoUpdateDto.IdTipoHabitacion, ct);
            }

            var dataModel = tipoUpdateDto.ToDataModel();
            await _tipoHabitacionDataService.UpdateAsync(dataModel, ct);
            await ReplaceImagenesAsync(tipoUpdateDto.IdTipoHabitacion, tipoUpdateDto.Imagenes, ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var existing = await _tipoHabitacionDataService.GetByIdAsync(id, ct);
            if (existing == null)
                throw new NotFoundException("TIP-005", $"No se encontró el tipo de habitación con ID {id}.");
            await _tipoHabitacionDataService.DeleteAsync(id, ct);
        }

        public async Task<IEnumerable<TipoHabitacionDTO>> GetPublicosAsync(CancellationToken ct = default)
        {
            var dataModels = await _tipoHabitacionDataService.GetPublicosAsync(ct);
            return dataModels.ToDtoList();
        }

        public async Task<bool> ExistsByCodigoAsync(string codigo, CancellationToken ct = default)
        {
            return await _tipoHabitacionDataService.ExistsByCodigoAsync(codigo, ct);
        }

        private static void ValidateTipo(int capacidadAdultos, int capacidadNinos, int capacidadTotal, string? estado)
        {
            var errors = new Dictionary<string, string[]>();
            if (capacidadAdultos <= 0)
                errors["CapacidadAdultos"] = new[] { "La capacidad de adultos debe ser mayor a cero." };
            if (capacidadNinos < 0)
                errors["CapacidadNinos"] = new[] { "La capacidad de ninos no puede ser negativa." };
            if (capacidadTotal <= 0)
                errors["CapacidadTotal"] = new[] { "La capacidad total debe ser mayor a cero." };
            if (capacidadAdultos + capacidadNinos != capacidadTotal)
                errors["CapacidadTotal"] = new[] { "La capacidad total debe coincidir con adultos + ninos." };

            var estadosValidos = new[] { "ACT", "INA" };
            if (!string.IsNullOrWhiteSpace(estado) && !estadosValidos.Contains(estado))
                errors["EstadoTipoHabitacion"] = new[] { $"Estado invalido. Valores permitidos: {string.Join(", ", estadosValidos)}." };

            if (errors.Count > 0)
                throw new ValidationException("TIP-VAL-001", errors);
        }

        private async Task EnsureCodigoUnicoAsync(string codigo, int? idTipoHabitacion, CancellationToken ct)
        {
            var normalized = (codigo ?? string.Empty).Trim().ToUpperInvariant();
            var exists = await _context.TiposHabitacion.AnyAsync(t =>
                t.CodigoTipoHabitacion.Trim().ToUpper() == normalized &&
                (!idTipoHabitacion.HasValue || t.IdTipoHabitacion != idTipoHabitacion.Value) &&
                !t.EsEliminado, ct);

            if (exists)
                throw new ConflictException("Ya existe un tipo de habitacion registrado con ese codigo.");
        }

        private async Task EnsureNombreUnicoAsync(string nombre, int? idTipoHabitacion, CancellationToken ct)
        {
            var normalized = (nombre ?? string.Empty).Trim().ToUpperInvariant();
            var exists = await _context.TiposHabitacion.AnyAsync(t =>
                t.NombreTipoHabitacion.Trim().ToUpper() == normalized &&
                (!idTipoHabitacion.HasValue || t.IdTipoHabitacion != idTipoHabitacion.Value) &&
                !t.EsEliminado, ct);

            if (exists)
                throw new ConflictException("Ya existe un tipo de habitacion registrado con ese nombre.");
        }

        private async Task ReplaceImagenesAsync(int idTipoHabitacion, List<ImagenDTO>? imagenes, CancellationToken ct)
        {
            if (imagenes == null)
                return;

            var existing = await _context.TipoHabitacionImagenes
                .Where(i => i.IdTipoHabitacion == idTipoHabitacion)
                .ToListAsync(ct);
            _context.TipoHabitacionImagenes.RemoveRange(existing);

            var clean = imagenes
                .Where(i => !string.IsNullOrWhiteSpace(i.UrlImagen))
                .OrderBy(i => i.Orden <= 0 ? int.MaxValue : i.Orden)
                .ToList();

            for (var index = 0; index < clean.Count; index++)
            {
                var image = clean[index];
                _context.TipoHabitacionImagenes.Add(new TipoHabitacionImagenEntity
                {
                    IdTipoHabitacion = idTipoHabitacion,
                    UrlImagen = image.UrlImagen.Trim(),
                    DescripcionImagen = image.Descripcion,
                    OrdenVisualizacion = image.Orden > 0 ? image.Orden : index + 1,
                    EsPrincipal = image.EsPrincipal || (!clean.Any(i => i.EsPrincipal) && index == 0),
                    FechaRegistroUtc = DateTime.UtcNow,
                    CreadoPorUsuario = "Sistema"
                });
            }

            await _context.SaveChangesAsync(ct);
        }
    }
}
