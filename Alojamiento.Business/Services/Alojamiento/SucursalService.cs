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
using Alojamiento.Business.Validators.Alojamiento;
using Alojamiento.DataAccess.Context;
using Alojamiento.DataAccess.Entities.Alojamiento;
using Alojamiento.DataManagement.Alojamiento.Interfaces;
using Alojamiento.DataManagement.Alojamiento.Models;

namespace Alojamiento.Business.Services.Alojamiento
{
    public class SucursalService : ISucursalService
    {
        private readonly ISucursalDataService _dataService;
        private readonly AlojamientoDbContext _context;

        public SucursalService(ISucursalDataService dataService, AlojamientoDbContext context)
        {
            _dataService = dataService;
            _context = context;
        }

        public async Task<IEnumerable<SucursalDTO>> GetAllAsync(string? estado = null, CancellationToken ct = default)
        {
            var all = (await _dataService.GetAllAsync(ct)).ToDtoList();

            if (!string.IsNullOrWhiteSpace(estado))
                return all.Where(s => s.EstadoSucursal == estado).ToList();

            return all.Where(s => s.EstadoSucursal != "INA").ToList();
        }

        public async Task<SucursalDTO> GetByIdAsync(int id, CancellationToken ct = default)
            => (await _dataService.GetByIdAsync(id, ct)).ToDto()
               ?? throw new NotFoundException("SUC-001", $"No se encontró la sucursal con ID {id}.");

        public async Task<SucursalDTO> GetByGuidAsync(Guid guid, CancellationToken ct = default)
            => (await _dataService.GetByGuidAsync(guid, ct)).ToDto()
               ?? throw new NotFoundException("SUC-001", $"No se encontrÃ³ la sucursal con GUID {guid}.");

        public async Task<SucursalDTO> CreateAsync(SucursalCreateDTO dto, CancellationToken ct = default)
        {
            SucursalValidator.ValidateCreate(dto);
            await EnsureCodigoUnicoAsync(dto.CodigoSucursal, null, ct);
            await EnsureNombreUnicoAsync(dto.NombreSucursal, null, ct);
            var created = await _dataService.AddAsync(dto.ToDataModel()!, ct);
            await ReplaceImagenesAsync(created.IdSucursal, dto.Imagenes, ct);
            return (await _dataService.GetByIdAsync(created.IdSucursal, ct)).ToDto()!;
        }

        public async Task UpdateAsync(SucursalUpdateDTO dto, CancellationToken ct = default)
        {
            _ = await GetByIdAsync(dto.IdSucursal, ct);
            SucursalValidator.ValidateUpdate(dto);
            await EnsureCodigoUnicoAsync(dto.CodigoSucursal, dto.IdSucursal, ct);
            await EnsureNombreUnicoAsync(dto.NombreSucursal, dto.IdSucursal, ct);
            await _dataService.UpdateAsync(dto.ToDataModel()!, ct);
            await ReplaceImagenesAsync(dto.IdSucursal, dto.Imagenes, ct);
        }

        public async Task UpdatePoliticasAsync(Guid sucursalGuid, SucursalPoliticasUpdateDTO dto, string usuario, CancellationToken ct = default)
        {
            var existing = await _dataService.GetByGuidAsync(sucursalGuid, ct);
            if (existing == null)
                throw new NotFoundException("SUC-001", $"No se encontrÃ³ la sucursal con GUID {sucursalGuid}.");

            var politicas = new SucursalDataModel
            {
                HoraCheckin = dto.HoraCheckin,
                HoraCheckout = dto.HoraCheckout,
                PermiteMascotas = dto.PermiteMascotas,
                SePermiteFumar = dto.SePermiteFumar,
                AceptaNinos = dto.AceptaNinos,
                CheckinAnticipado = dto.CheckinAnticipado,
                CheckoutTardio = dto.CheckoutTardio,
                ModificadoPorUsuario = usuario
            };

            await _dataService.UpdatePoliticasAsync(existing.IdSucursal, politicas, ct);
        }

        public async Task InhabilitarAsync(Guid sucursalGuid, string motivo, string usuario, CancellationToken ct = default)
        {
            var existing = await _dataService.GetByGuidAsync(sucursalGuid, ct);
            if (existing == null)
                throw new NotFoundException("SUC-001", $"No se encontrÃ³ la sucursal con GUID {sucursalGuid}.");

            await _dataService.InhabilitarAsync(existing.IdSucursal, motivo, usuario, ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            _ = await GetByIdAsync(id, ct);
            await EnsureSinHabitacionesActivasAsync(id, ct);
            await _dataService.DeleteAsync(id, ct);
        }

        public async Task DeleteAsync(Guid guid, CancellationToken ct = default)
        {
            var existing = await _dataService.GetByGuidAsync(guid, ct);
            if (existing == null)
                throw new NotFoundException("SUC-001", $"No se encontrÃ³ la sucursal con GUID {guid}.");

            await EnsureSinHabitacionesActivasAsync(existing.IdSucursal, ct);
            await _dataService.DeleteAsync(existing.IdSucursal, ct);
        }

        private async Task EnsureNombreUnicoAsync(string nombreSucursal, int? idSucursal, CancellationToken ct)
        {
            var normalized = (nombreSucursal ?? string.Empty).Trim().ToUpperInvariant();
            var exists = await _context.Sucursales.AnyAsync(s =>
                s.NombreSucursal.Trim().ToUpper() == normalized &&
                (!idSucursal.HasValue || s.IdSucursal != idSucursal.Value), ct);

            if (exists)
                throw new ConflictException("Ya existe una sucursal registrada con ese nombre.");
        }

        private async Task EnsureCodigoUnicoAsync(string codigoSucursal, int? idSucursal, CancellationToken ct)
        {
            var normalized = (codigoSucursal ?? string.Empty).Trim().ToUpperInvariant();
            var exists = await _context.Sucursales.AnyAsync(s =>
                s.CodigoSucursal.Trim().ToUpper() == normalized &&
                (!idSucursal.HasValue || s.IdSucursal != idSucursal.Value) &&
                !s.EsEliminado, ct);

            if (exists)
                throw new ConflictException("Ya existe una sucursal registrada con ese codigo.");
        }

        private async Task EnsureSinHabitacionesActivasAsync(int idSucursal, CancellationToken ct)
        {
            var tieneHabitacionesActivas = await _context.Habitaciones.AnyAsync(h =>
                h.IdSucursal == idSucursal &&
                h.EstadoHabitacion != "INA", ct);

            if (tieneHabitacionesActivas)
                throw new ConflictException("No se puede eliminar la sucursal porque tiene habitaciones activas asociadas.");
        }

        private async Task ReplaceImagenesAsync(int idSucursal, List<ImagenDTO>? imagenes, CancellationToken ct)
        {
            if (imagenes == null)
                return;

            var existing = await _context.SucursalImagenes
                .Where(i => i.IdSucursal == idSucursal)
                .ToListAsync(ct);
            _context.SucursalImagenes.RemoveRange(existing);

            var clean = imagenes
                .Where(i => !string.IsNullOrWhiteSpace(i.UrlImagen))
                .OrderBy(i => i.Orden <= 0 ? int.MaxValue : i.Orden)
                .ToList();

            for (var index = 0; index < clean.Count; index++)
            {
                var image = clean[index];
                _context.SucursalImagenes.Add(new SucursalImagenEntity
                {
                    IdSucursal = idSucursal,
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
