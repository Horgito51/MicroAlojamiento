using Alojamiento.Business.DTOs.Booking;
using Alojamiento.Business.DTOs.Alojamiento;
using Alojamiento.Business.Exceptions;
using Alojamiento.Business.Interfaces.Alojamiento;
using Alojamiento.Business.Interfaces.Booking;
using Alojamiento.DataAccess.Context;
using Alojamiento.DataAccess.Entities.Alojamiento;
using Alojamiento.DataAccess.Entities.Valoraciones;
using Microsoft.EntityFrameworkCore;

namespace Alojamiento.Business.Services.Booking
{
    public sealed class BookingAccommodationService : IBookingAccommodationService
    {
        private static readonly string[] PublicReviewStates = { "PUB" };

        private readonly AlojamientoDbContext _context;
        private readonly ISucursalService _sucursalService;
        private readonly IHabitacionService _habitacionService;
        private readonly ITarifaService _tarifaService;
        private readonly IReservaAvailabilityClient _reservaAvailabilityClient;

        public BookingAccommodationService(
            AlojamientoDbContext context,
            ISucursalService sucursalService,
            IHabitacionService habitacionService,
            ITarifaService tarifaService,
            IReservaAvailabilityClient reservaAvailabilityClient)
        {
            _context = context;
            _sucursalService = sucursalService;
            _habitacionService = habitacionService;
            _tarifaService = tarifaService;
            _reservaAvailabilityClient = reservaAvailabilityClient;
        }

        public async Task<BookingPagedResponseDTO<AccommodationSearchItemDTO>> SearchAsync(AccommodationSearchQueryDTO query, CancellationToken ct = default)
        {
            NormalizeSearchQuery(query);

            var sucursales = await _context.Sucursales
                .AsNoTracking()
                .Where(s => s.EstadoSucursal == "ACT" && !s.EsEliminado)
                .ToListAsync(ct);

            sucursales = ApplySucursalFilters(sucursales, query);
            var cards = new List<AccommodationSearchItemDTO>();

            foreach (var sucursal in sucursales)
            {
                var card = await BuildSearchItemAsync(sucursal, query.FechaEntrada, query.FechaSalida, ct);

                if (query.FechaEntrada.HasValue && card.HabitacionesDisponibles <= 0)
                    continue;
                if (query.NumHabitaciones.HasValue && card.HabitacionesDisponibles < query.NumHabitaciones.Value)
                    continue;
                if (query.PrecioMin.HasValue && (!card.PrecioDesde.HasValue || card.PrecioDesde.Value < query.PrecioMin.Value))
                    continue;
                if (query.PrecioMax.HasValue && (!card.PrecioDesde.HasValue || card.PrecioDesde.Value > query.PrecioMax.Value))
                    continue;

                cards.Add(card);
            }

            cards = await ApplyCapacityFiltersAsync(cards, query, ct);
            cards = ApplyOrdering(cards, query.OrdenarPor);

            return Page(cards, query.Pagina, query.Limite);
        }

        public async Task<AccommodationDetailResponseDTO> GetDetailAsync(Guid sucursalGuid, DateTime? fechaEntrada, DateTime? fechaSalida, CancellationToken ct = default)
        {
            if (sucursalGuid == Guid.Empty)
                throw new ValidationException("BOOK-ACC-001", "sucursalGuid es obligatorio.");

            ValidateOptionalDateRange(fechaEntrada, fechaSalida);

            var sucursal = await ResolveSucursalAsync(sucursalGuid, ct);
            var searchItem = await BuildSearchItemAsync(sucursal, fechaEntrada, fechaSalida, ct);
            var tipos = await GetTiposBySucursalAsync(sucursal.IdSucursal, ct);
            var preciosPorTipo = await GetPreciosBasePorTipoAsync(sucursal.IdSucursal, ct);
            var preciosAplicadosPorTipo = await GetPreciosAplicadosPorTipoAsync(sucursal.IdSucursal, tipos, fechaEntrada, fechaSalida, ct);
            var requiereTarifa = fechaEntrada.HasValue && fechaSalida.HasValue;
            var tiposVisibles = requiereTarifa
                ? tipos.Where(t => preciosAplicadosPorTipo.ContainsKey(t.IdTipoHabitacion)).ToList()
                : tipos;
            var disponibilidad = await BuildAvailabilityAsync(sucursal.IdSucursal, tiposVisibles, fechaEntrada, fechaSalida, ct);
            if (requiereTarifa)
            {
                var tiposConDisponibilidad = disponibilidad.PorTipoHabitacion
                    .Where(d => d.Disponibles > 0)
                    .Select(d => d.TipoHabitacionGuid)
                    .ToHashSet();

                tiposVisibles = tiposVisibles
                    .Where(t => tiposConDisponibilidad.Contains(t.TipoHabitacionGuid))
                    .ToList();

                disponibilidad.PorTipoHabitacion = disponibilidad.PorTipoHabitacion
                    .Where(d => tiposConDisponibilidad.Contains(d.TipoHabitacionGuid))
                    .ToList();
            }
            var imagenesSucursal = await GetImagenesSucursalAsync(sucursal.IdSucursal, ct);
            var imagenesPorTipo = await GetImagenesPorTipoAsync(tiposVisibles.Select(t => t.IdTipoHabitacion), ct);
            var tarifas = await GetTarifasActivasAsync(sucursal.IdSucursal, tiposVisibles, ct);
            var amenities = await GetCatalogNamesAsync(sucursal.IdSucursal, tiposVisibles.Select(t => t.IdTipoHabitacion), "AME", ct);

            return new AccommodationDetailResponseDTO
            {
                SucursalGuid = searchItem.SucursalGuid,
                Nombre = searchItem.Nombre,
                Ciudad = searchItem.Ciudad,
                Provincia = searchItem.Provincia,
                Pais = searchItem.Pais,
                Direccion = searchItem.Direccion,
                Descripcion = searchItem.Descripcion,
                Categoria = searchItem.Categoria,
                Estrellas = searchItem.Estrellas,
                TipoAlojamiento = searchItem.TipoAlojamiento,
                PrecioDesde = searchItem.PrecioDesde,
                Moneda = searchItem.Moneda,
                ImagenPrincipalUrl = searchItem.ImagenPrincipalUrl,
                PromedioValoracion = searchItem.PromedioValoracion,
                TotalValoraciones = searchItem.TotalValoraciones,
                HabitacionesDisponibles = searchItem.HabitacionesDisponibles,
                ServiciosDestacados = searchItem.ServiciosDestacados,
                HoraCheckIn = searchItem.HoraCheckIn,
                HoraCheckOut = searchItem.HoraCheckOut,
                AceptaNinos = searchItem.AceptaNinos,
                PermiteMascotas = searchItem.PermiteMascotas,
                DescripcionCompleta = FirstNonEmpty(sucursal.DescripcionSucursal, sucursal.DescripcionCorta),
                TiposHabitacion = tiposVisibles.Select(t =>
                {
                    var precioBase = preciosPorTipo.TryGetValue(t.IdTipoHabitacion, out var precio) ? precio : 0m;
                    var tieneTarifa = preciosAplicadosPorTipo.TryGetValue(t.IdTipoHabitacion, out var precioAplicado);
                    return new AccommodationRoomTypeDTO
                    {
                        TipoHabitacionGuid = t.TipoHabitacionGuid,
                        Nombre = t.NombreTipoHabitacion,
                        TipoCama = t.TipoCama,
                        CapacidadAdultos = t.CapacidadAdultos,
                        CapacidadNinos = t.CapacidadNinos,
                        AreaM2 = t.AreaM2,
                        PrecioBase = precioBase,
                        PrecioNocheAplicado = tieneTarifa ? precioAplicado!.Precio : precioBase,
                        TarifaGuid = tieneTarifa ? precioAplicado!.TarifaGuid : null,
                        OrigenPrecio = tieneTarifa ? "TARIFA" : "PRECIO_BASE",
                        Imagenes = imagenesPorTipo.TryGetValue(t.IdTipoHabitacion, out var imgs) ? imgs : new List<string>(),
                        DisponiblesEnRango = disponibilidad.PorTipoHabitacion.FirstOrDefault(d => d.TipoHabitacionGuid == t.TipoHabitacionGuid)?.Disponibles
                    };
                }).ToList(),
                TarifasActivas = tarifas,
                Amenities = amenities,
                Imagenes = imagenesSucursal,
                Politicas = new AccommodationPolicyDTO
                {
                    HoraCheckIn = sucursal.HoraCheckin,
                    HoraCheckOut = sucursal.HoraCheckout,
                    AceptaNinos = sucursal.AceptaNinos,
                    PermiteMascotas = sucursal.PermiteMascotas,
                    Politicas = BuildPolicyText(sucursal)
                },
                Disponibilidad = disponibilidad
            };
        }

        public async Task<BookingPagedResponseDTO<AccommodationReviewDTO>> GetReviewsAsync(Guid sucursalGuid, int pagina, int limite, CancellationToken ct = default)
        {
            if (sucursalGuid == Guid.Empty)
                throw new ValidationException("BOOK-REV-001", "sucursalGuid es obligatorio.");

            var sucursal = await ResolveSucursalAsync(sucursalGuid, ct);
            pagina = Math.Max(1, pagina);
            limite = Math.Clamp(limite, 1, 50);

            var query = BuildPublicReviewsQuery(sucursal.IdSucursal)
                .OrderByDescending(v => v.FechaRegistroUtc);

            var total = await query.CountAsync(ct);
            var valoraciones = await query.Skip((pagina - 1) * limite).Take(limite).ToListAsync(ct);
            var items = valoraciones.Select(v => new AccommodationReviewDTO
            {
                ValoracionGuid = v.ValoracionGuid,
                Puntuacion = v.PuntuacionGeneral,
                ComentarioPositivo = v.ComentarioPositivo,
                ComentarioNegativo = v.ComentarioNegativo,
                TipoViaje = v.TipoViaje,
                Fecha = v.FechaRegistroUtc,
                NombreVisibleCliente = v.ClienteGuid.HasValue ? $"Cliente {v.ClienteGuid.Value.ToString("N")[..6]}" : "Huesped",
                RespuestaPropiedad = v.RespuestaHotel
            }).ToList();
            return BuildPaged(items, pagina, limite, total);
        }

        public async Task<IEnumerable<HabitacionPublicListItemDTO>> GetHabitacionesDisponiblesAsync(
            Guid sucursalGuid,
            DateTime? fechaInicio,
            DateTime? fechaFin,
            Guid? tipoHabitacionGuid = null,
            CancellationToken ct = default)
        {
            if (sucursalGuid == Guid.Empty)
                throw new ValidationException("BOOK-HAB-001", "sucursalGuid es obligatorio.");
            if (tipoHabitacionGuid == Guid.Empty)
                throw new ValidationException("BOOK-HAB-004", "tipoHabitacionGuid debe ser un UUID valido cuando se envia.");
            ValidateOptionalDateRange(fechaInicio, fechaFin);

            var sucursal = await _sucursalService.GetByGuidAsync(sucursalGuid, ct);
            
            IEnumerable<HabitacionDTO> habitaciones;
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                ValidateRequiredDateRange(fechaInicio.Value, fechaFin.Value, "fechaInicio", "fechaFin");
                habitaciones = await _habitacionService.GetDisponiblesAsync(sucursal.IdSucursal, fechaInicio.Value, fechaFin.Value, ct);
                var reservadas = await GetHabitacionesReservadasAsync(sucursal.IdSucursal, fechaInicio.Value, fechaFin.Value, ct);
                habitaciones = habitaciones.Where(h => !reservadas.Contains(h.IdHabitacion)).ToList();
            }
            else
            {
                habitaciones = await _habitacionService.GetBySucursalAsync(sucursal.IdSucursal, ct);
            }

            var tipos = await _context.TiposHabitacion.AsNoTracking().ToDictionaryAsync(t => t.IdTipoHabitacion, ct);

            var list = new List<HabitacionPublicListItemDTO>();
            foreach (var h in habitaciones.Where(h => h.EstadoHabitacion == "DIS" && tipos.ContainsKey(h.IdTipoHabitacion)))
            {
                var tipo = tipos[h.IdTipoHabitacion];
                var tarifa = fechaInicio.HasValue && fechaFin.HasValue
                    ? await _tarifaService.GetTarifaVigenteRangoOrDefaultAsync(
                        h.IdSucursal,
                        h.IdTipoHabitacion,
                        fechaInicio.Value,
                        fechaFin.Value,
                        null,
                        ct)
                    : null;

                if (fechaInicio.HasValue && fechaFin.HasValue && tarifa is null)
                    continue;

                list.Add(new HabitacionPublicListItemDTO
                {
                    HabitacionGuid = h.HabitacionGuid,
                    TipoHabitacionGuid = tipo.TipoHabitacionGuid,
                    TipoNombre = tipo.NombreTipoHabitacion,
                    NumeroHabitacion = h.NumeroHabitacion,
                    Piso = h.Piso,
                    CapacidadAdultos = tipo.CapacidadAdultos,
                    CapacidadNinos = tipo.CapacidadNinos,
                    PrecioBase = h.PrecioBase,
                    PrecioNocheAplicado = tarifa?.PrecioPorNoche ?? h.PrecioBase,
                    TarifaGuid = tarifa?.TarifaGuid,
                    OrigenPrecio = tarifa is null ? "PRECIO_BASE" : "TARIFA",
                    EstadoHabitacion = h.EstadoHabitacion,
                    DisponibleEnRango = fechaInicio.HasValue && fechaFin.HasValue
                });
            }

            IEnumerable<HabitacionPublicListItemDTO> filteredList = list;

            if (tipoHabitacionGuid.HasValue && tipoHabitacionGuid.Value != Guid.Empty)
            {
                filteredList = filteredList.Where(h => h.TipoHabitacionGuid == tipoHabitacionGuid.Value);
            }

            return filteredList
                .OrderBy(h => h.NumeroHabitacion, NaturalStringComparer.Instance)
                .ThenBy(h => h.TipoNombre)
                .ToList();
        }

        private async Task<SucursalEntity> ResolveSucursalAsync(Guid sucursalGuid, CancellationToken ct)
        {
            var sucursal = await _context.Sucursales
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SucursalGuid == sucursalGuid && s.EstadoSucursal == "ACT" && !s.EsEliminado, ct);

            if (sucursal != null)
                return sucursal;

            throw new NotFoundException("BOOK-ACC-404", $"No se encontro sucursal publica asociada al GUID {sucursalGuid}.");
        }

        private static void NormalizeSearchQuery(AccommodationSearchQueryDTO query)
        {
            query.Pagina = Math.Max(1, query.Pagina);
            query.Limite = Math.Clamp(query.Limite, 1, 50);

            ValidateOptionalDateRange(query.FechaEntrada, query.FechaSalida);

            if (query.NumAdultos is < 0 || query.NumNinos is < 0 || query.NumHabitaciones is < 0)
                throw new ValidationException("BOOK-SEARCH-001", "Los filtros de capacidad no pueden ser negativos.");

            if (query.PrecioMin.HasValue && query.PrecioMax.HasValue && query.PrecioMax < query.PrecioMin)
                throw new ValidationException("BOOK-SEARCH-002", "precioMax debe ser mayor o igual a precioMin.");
        }

        private static void ValidateOptionalDateRange(DateTime? fechaEntrada, DateTime? fechaSalida)
        {
            if (fechaEntrada.HasValue != fechaSalida.HasValue)
                throw new ValidationException("BOOK-DATE-001", "fechaInicio y fechaFin deben enviarse juntas.");

            if (fechaEntrada.HasValue && fechaEntrada.Value.Date < DateTime.Today)
                throw new ValidationException("BOOK-DATE-005", "fechaInicio debe ser de hoy en adelante.");

            if (fechaEntrada.HasValue && fechaSalida <= fechaEntrada)
                throw new ValidationException("BOOK-DATE-002", "fechaFin debe ser posterior a fechaInicio.");
        }

        private static void ValidateRequiredDateRange(
            DateTime fechaEntrada,
            DateTime fechaSalida,
            string fechaEntradaNombre = "fechaEntrada",
            string fechaSalidaNombre = "fechaSalida")
        {
            if (fechaEntrada == default)
                throw new ValidationException("BOOK-DATE-003", $"{fechaEntradaNombre} es obligatoria.");
            if (fechaSalida == default)
                throw new ValidationException("BOOK-DATE-004", $"{fechaSalidaNombre} es obligatoria.");
            if (fechaEntrada.Date < DateTime.Today)
                throw new ValidationException("BOOK-DATE-005", $"{fechaEntradaNombre} debe ser de hoy en adelante.");
            if (fechaSalida <= fechaEntrada)
                throw new ValidationException("BOOK-DATE-002", $"{fechaSalidaNombre} debe ser posterior a {fechaEntradaNombre}.");
        }

        private static List<SucursalEntity> ApplySucursalFilters(List<SucursalEntity> sucursales, AccommodationSearchQueryDTO query)
        {
            if (!string.IsNullOrWhiteSpace(query.Destino))
            {
                var destino = query.Destino.Trim();
                sucursales = sucursales.Where(s =>
                    Contains(s.Ciudad, destino) ||
                    Contains(s.Provincia, destino) ||
                    Contains(s.Pais, destino) ||
                    Contains(s.NombreSucursal, destino) ||
                    Contains(s.Direccion, destino)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(query.TipoAlojamiento))
                sucursales = sucursales.Where(s => Contains(s.TipoAlojamiento, query.TipoAlojamiento)).ToList();

            if (!string.IsNullOrWhiteSpace(query.CategoriaViaje))
                sucursales = sucursales.Where(s => Contains(s.CategoriaViaje, query.CategoriaViaje)).ToList();

            return sucursales;
        }

        private async Task<List<AccommodationSearchItemDTO>> ApplyCapacityFiltersAsync(List<AccommodationSearchItemDTO> cards, AccommodationSearchQueryDTO query, CancellationToken ct)
        {
            if (!query.NumAdultos.HasValue && !query.NumNinos.HasValue)
                return cards;

            var adults = query.NumAdultos ?? 0;
            var children = query.NumNinos ?? 0;
            var sucursalGuids = cards.Select(c => c.SucursalGuid).ToList();
            var allowedSucursalGuids = await _context.Habitaciones
                .AsNoTracking()
                .Where(h => sucursalGuids.Contains(h.Sucursal.SucursalGuid)
                    && h.EstadoHabitacion == "DIS"
                    && !h.EsEliminado
                    && h.TipoHabitacion.EstadoTipoHabitacion == "ACT"
                    && h.TipoHabitacion.PermiteReservaPublica
                    && h.TipoHabitacion.CapacidadAdultos >= adults
                    && h.TipoHabitacion.CapacidadNinos >= children)
                .Select(h => h.Sucursal.SucursalGuid)
                .Distinct()
                .ToListAsync(ct);

            return cards.Where(c => allowedSucursalGuids.Contains(c.SucursalGuid)).ToList();
        }

        private static List<AccommodationSearchItemDTO> ApplyOrdering(List<AccommodationSearchItemDTO> cards, string? ordenarPor)
        {
            return (ordenarPor ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "precio_desc" or "precio-desc" => cards.OrderByDescending(c => c.PrecioDesde ?? decimal.MaxValue).ToList(),
                "valoracion" or "rating" => cards.OrderByDescending(c => c.PromedioValoracion ?? 0m).ThenBy(c => c.PrecioDesde ?? decimal.MaxValue).ToList(),
                "nombre" => cards.OrderBy(c => c.Nombre).ToList(),
                _ => cards.OrderBy(c => c.PrecioDesde ?? decimal.MaxValue).ThenByDescending(c => c.PromedioValoracion ?? 0m).ToList()
            };
        }

        private async Task<AccommodationSearchItemDTO> BuildSearchItemAsync(SucursalEntity sucursal, DateTime? fechaEntrada, DateTime? fechaSalida, CancellationToken ct)
        {
            var habitaciones = await _context.Habitaciones
                .AsNoTracking()
                .Where(h => h.IdSucursal == sucursal.IdSucursal && h.EstadoHabitacion == "DIS" && !h.EsEliminado)
                .ToListAsync(ct);

            if (fechaEntrada.HasValue && fechaSalida.HasValue)
            {
                var reservadas = await GetHabitacionesReservadasAsync(sucursal.IdSucursal, fechaEntrada.Value, fechaSalida.Value, ct);
                habitaciones = habitaciones.Where(h => !reservadas.Contains(h.IdHabitacion)).ToList();
                habitaciones = await FilterHabitacionesConTarifaAsync(habitaciones, fechaEntrada.Value, fechaSalida.Value, ct);
            }

            var tipoIds = habitaciones.Select(h => h.IdTipoHabitacion).Distinct().ToList();
            var imagenPrincipal = await GetImagenPrincipalSucursalAsync(sucursal.IdSucursal, ct);
            var servicios = await GetCatalogNamesAsync(sucursal.IdSucursal, tipoIds, "SRV", ct);
            var rating = await BuildPublicReviewsQuery(sucursal.IdSucursal)
                .GroupBy(v => v.IdSucursal)
                .Select(g => new { Promedio = g.Average(v => v.PuntuacionGeneral), Total = g.Count() })
                .FirstOrDefaultAsync(ct);

            return new AccommodationSearchItemDTO
            {
                SucursalGuid = sucursal.SucursalGuid,
                Nombre = sucursal.NombreSucursal,
                Ciudad = sucursal.Ciudad,
                Provincia = sucursal.Provincia,
                Pais = sucursal.Pais,
                Direccion = sucursal.Direccion,
                Descripcion = FirstNonEmpty(sucursal.DescripcionCorta, sucursal.DescripcionSucursal),
                Categoria = sucursal.CategoriaViaje,
                Estrellas = sucursal.Estrellas,
                TipoAlojamiento = sucursal.TipoAlojamiento,
                PrecioDesde = await GetPrecioDesdeAsync(habitaciones, fechaEntrada, fechaSalida, ct),
                ImagenPrincipalUrl = imagenPrincipal,
                PromedioValoracion = rating == null ? null : Math.Round(rating.Promedio, 1),
                TotalValoraciones = rating?.Total ?? 0,
                HabitacionesDisponibles = habitaciones.Count,
                ServiciosDestacados = servicios.Take(6).ToList(),
                HoraCheckIn = sucursal.HoraCheckin,
                HoraCheckOut = sucursal.HoraCheckout,
                AceptaNinos = sucursal.AceptaNinos,
                PermiteMascotas = sucursal.PermiteMascotas
            };
        }

        private async Task<List<TipoHabitacionEntity>> GetTiposBySucursalAsync(int idSucursal, CancellationToken ct)
        {
            return await _context.Habitaciones
                .AsNoTracking()
                .Where(h => h.IdSucursal == idSucursal && !h.EsEliminado)
                .Select(h => h.IdTipoHabitacion)
                .Distinct()
                .Join(_context.TiposHabitacion.AsNoTracking().Where(t => t.EstadoTipoHabitacion == "ACT" && t.PermiteReservaPublica && !t.EsEliminado),
                    id => id,
                    tipo => tipo.IdTipoHabitacion,
                    (_, tipo) => tipo)
                .OrderBy(t => t.NombreTipoHabitacion)
                .ToListAsync(ct);
        }

        private IQueryable<ValoracionEntity> BuildPublicReviewsQuery(int idSucursal)
        {
            return _context.Valoraciones
                .AsNoTracking()
                .Where(v => v.IdSucursal == idSucursal
                    && v.PublicadaEnPortal
                    && PublicReviewStates.Contains(v.EstadoValoracion)
                    && (!v.IdHabitacion.HasValue ||
                        _context.Habitaciones
                            .AsNoTracking()
                            .Any(h => h.IdHabitacion == v.IdHabitacion.Value && h.IdSucursal == idSucursal)));
        }

        private async Task<AccommodationAvailabilityDTO> BuildAvailabilityAsync(int idSucursal, List<TipoHabitacionEntity> tipos, DateTime? fechaEntrada, DateTime? fechaSalida, CancellationToken ct)
        {
            var start = fechaEntrada ?? DateTime.UtcNow.Date;
            var end = fechaSalida ?? start.AddDays(1);
            var habitaciones = await _context.Habitaciones
                .AsNoTracking()
                .Where(h => h.IdSucursal == idSucursal && h.EstadoHabitacion == "DIS" && !h.EsEliminado)
                .ToListAsync(ct);

            var reservadas = await GetHabitacionesReservadasAsync(idSucursal, start, end, ct);
            habitaciones = habitaciones.Where(h => !reservadas.Contains(h.IdHabitacion)).ToList();

            return new AccommodationAvailabilityDTO
            {
                FechaInicio = start,
                FechaFin = end,
                PorTipoHabitacion = tipos.Select(t => new AvailabilityByRoomTypeDTO
                {
                    TipoHabitacionGuid = t.TipoHabitacionGuid,
                    Nombre = t.NombreTipoHabitacion,
                    Disponibles = habitaciones.Count(h => h.IdTipoHabitacion == t.IdTipoHabitacion)
                }).ToList()
            };
        }

        private async Task<Dictionary<int, List<string>>> GetImagenesPorTipoAsync(IEnumerable<int> tipoIds, CancellationToken ct)
        {
            var ids = tipoIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<int, List<string>>();

            var images = await _context.TipoHabitacionImagenes
                .AsNoTracking()
                .Where(i => ids.Contains(i.IdTipoHabitacion))
                .OrderByDescending(i => i.EsPrincipal)
                .ThenBy(i => i.OrdenVisualizacion)
                .ToListAsync(ct);

            return images
                .GroupBy(i => i.IdTipoHabitacion)
                .ToDictionary(g => g.Key, g => g.Select(i => i.UrlImagen).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList());
        }

        private async Task<List<string>> GetImagenesSucursalAsync(int idSucursal, CancellationToken ct)
        {
            return await _context.SucursalImagenes
                .AsNoTracking()
                .Where(i => i.IdSucursal == idSucursal)
                .OrderByDescending(i => i.EsPrincipal)
                .ThenBy(i => i.OrdenVisualizacion)
                .Select(i => i.UrlImagen)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct()
                .ToListAsync(ct);
        }

        private async Task<string?> GetImagenPrincipalSucursalAsync(int idSucursal, CancellationToken ct)
        {
            return await _context.SucursalImagenes
                .AsNoTracking()
                .Where(i => i.IdSucursal == idSucursal)
                .OrderByDescending(i => i.EsPrincipal)
                .ThenBy(i => i.OrdenVisualizacion)
                .Select(i => i.UrlImagen)
                .FirstOrDefaultAsync(ct);
        }

        private async Task<List<string>> GetCatalogNamesAsync(int idSucursal, IEnumerable<int> tipoIds, string tipoCatalogo, CancellationToken ct)
        {
            var ids = tipoIds.Distinct().ToList();
            var normalizedType = tipoCatalogo.Trim().ToUpperInvariant();

            var sucursalCatalogos = _context.CatalogoServicios
                .AsNoTracking()
                .Where(c => c.IdSucursal == idSucursal
                    && c.TipoCatalogo == normalizedType
                    && c.EstadoCatalogo == "ACT"
                    && !c.EsEliminado)
                .Select(c => c.NombreCatalogo);

            var tipoCatalogos = _context.TipoHabitacionCatalogos
                .AsNoTracking()
                .Where(tc => ids.Contains(tc.IdTipoHabitacion)
                    && tc.CatalogoServicio.TipoCatalogo == normalizedType
                    && tc.CatalogoServicio.EstadoCatalogo == "ACT"
                    && !tc.CatalogoServicio.EsEliminado)
                .Select(tc => tc.CatalogoServicio.NombreCatalogo);

            return await sucursalCatalogos
                .Union(tipoCatalogos)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync(ct);
        }

        private async Task<List<AccommodationTariffDTO>> GetTarifasActivasAsync(int idSucursal, List<TipoHabitacionEntity> tipos, CancellationToken ct)
        {
            var tipoMap = tipos.ToDictionary(t => t.IdTipoHabitacion, t => t.TipoHabitacionGuid);
            var now = DateTime.UtcNow.Date;
            var tarifas = await _context.Tarifas
                .AsNoTracking()
                .Where(t => t.IdSucursal == idSucursal
                    && tipoMap.Keys.Contains(t.IdTipoHabitacion)
                    && t.EstadoTarifa == "ACT"
                    && !t.EsEliminado
                    && t.PermitePortalPublico
                    && t.FechaInicio.Date <= now
                    && t.FechaFin.Date >= now)
                .OrderBy(t => t.Prioridad)
                .ThenBy(t => t.PrecioPorNoche)
                .ToListAsync(ct);

            return tarifas.Select(t => new AccommodationTariffDTO
            {
                TarifaGuid = t.TarifaGuid,
                Nombre = t.NombreTarifa,
                PrecioPorNoche = t.PrecioPorNoche,
                FechaInicio = t.FechaInicio,
                FechaFin = t.FechaFin,
                MinNoches = t.MinNoches,
                TipoHabitacionGuid = tipoMap.TryGetValue(t.IdTipoHabitacion, out var guid) ? guid : null
            }).ToList();
        }

        private async Task<Dictionary<int, decimal>> GetPreciosBasePorTipoAsync(int idSucursal, CancellationToken ct)
        {
            return await _context.Habitaciones
                .AsNoTracking()
                .Where(h => h.IdSucursal == idSucursal && h.EstadoHabitacion == "DIS" && !h.EsEliminado)
                .GroupBy(h => h.IdTipoHabitacion)
                .Select(g => new { IdTipoHabitacion = g.Key, Precio = g.Min(h => h.PrecioBase) })
                .ToDictionaryAsync(g => g.IdTipoHabitacion, g => g.Precio, ct);
        }

        private async Task<Dictionary<int, AppliedPrice>> GetPreciosAplicadosPorTipoAsync(
            int idSucursal,
            List<TipoHabitacionEntity> tipos,
            DateTime? fechaInicio,
            DateTime? fechaFin,
            CancellationToken ct)
        {
            var result = new Dictionary<int, AppliedPrice>();
            if (!fechaInicio.HasValue || !fechaFin.HasValue)
                return result;

            foreach (var tipo in tipos)
            {
                var tarifa = await _tarifaService.GetTarifaVigenteRangoOrDefaultAsync(
                    idSucursal,
                    tipo.IdTipoHabitacion,
                    fechaInicio.Value,
                    fechaFin.Value,
                    null,
                    ct);

                if (tarifa != null)
                    result[tipo.IdTipoHabitacion] = new AppliedPrice(tarifa.PrecioPorNoche, tarifa.TarifaGuid);
            }

            return result;
        }

        private async Task<decimal?> GetPrecioDesdeAsync(
            List<HabitacionEntity> habitaciones,
            DateTime? fechaInicio,
            DateTime? fechaFin,
            CancellationToken ct)
        {
            if (habitaciones.Count == 0)
                return null;

            if (!fechaInicio.HasValue || !fechaFin.HasValue)
                return habitaciones.Min(h => h.PrecioBase);

            var precios = new List<decimal>();
            foreach (var h in habitaciones)
            {
                var tarifa = await _tarifaService.GetTarifaVigenteRangoOrDefaultAsync(
                    h.IdSucursal,
                    h.IdTipoHabitacion,
                    fechaInicio.Value,
                    fechaFin.Value,
                    null,
                    ct);

                if (tarifa != null)
                    precios.Add(tarifa.PrecioPorNoche);
            }

            return precios.Count == 0 ? null : precios.Min();
        }

        private async Task<List<HabitacionEntity>> FilterHabitacionesConTarifaAsync(
            List<HabitacionEntity> habitaciones,
            DateTime fechaInicio,
            DateTime fechaFin,
            CancellationToken ct)
        {
            var result = new List<HabitacionEntity>();

            foreach (var habitacion in habitaciones)
            {
                var tarifa = await _tarifaService.GetTarifaVigenteRangoOrDefaultAsync(
                    habitacion.IdSucursal,
                    habitacion.IdTipoHabitacion,
                    fechaInicio,
                    fechaFin,
                    null,
                    ct);

                if (tarifa != null)
                    result.Add(habitacion);
            }

            return result;
        }

        private async Task<IReadOnlySet<int>> GetHabitacionesReservadasAsync(
            int idSucursal,
            DateTime fechaInicio,
            DateTime fechaFin,
            CancellationToken ct)
        {
            if (fechaFin <= fechaInicio)
                return new HashSet<int>();

            return await _reservaAvailabilityClient.GetHabitacionesReservadasAsync(
                idSucursal,
                fechaInicio.Date,
                fechaFin.Date,
                ct);
        }

        private static BookingPagedResponseDTO<T> Page<T>(List<T> source, int pagina, int limite)
        {
            var total = source.Count;
            var items = source.Skip((pagina - 1) * limite).Take(limite).ToList();
            return BuildPaged(items, pagina, limite, total);
        }

        private static BookingPagedResponseDTO<T> BuildPaged<T>(List<T> items, int pagina, int limite, int total)
        {
            var totalPaginas = total == 0 ? 0 : (int)Math.Ceiling(total / (double)limite);
            return new BookingPagedResponseDTO<T>
            {
                Items = items,
                Pagina = pagina,
                Limite = limite,
                TotalResultados = total,
                TotalPaginas = totalPaginas,
                TieneAnterior = pagina > 1,
                TieneSiguiente = pagina < totalPaginas
            };
        }

        private static string BuildPolicyText(SucursalEntity sucursal)
        {
            var parts = new List<string>();
            if (sucursal.CheckinAnticipado) parts.Add("Check-in anticipado sujeto a disponibilidad.");
            if (sucursal.CheckoutTardio) parts.Add("Check-out tardio sujeto a disponibilidad.");
            if (sucursal.SePermiteFumar) parts.Add("Se permite fumar en areas autorizadas.");
            return parts.Count == 0 ? "Aplican politicas generales de la propiedad." : string.Join(" ", parts);
        }

        private static string? FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        private static bool Contains(string? source, string? value)
            => !string.IsNullOrWhiteSpace(source)
                && !string.IsNullOrWhiteSpace(value)
                && source.Contains(value, StringComparison.OrdinalIgnoreCase);

        private sealed class NaturalStringComparer : IComparer<string?>
        {
            public static readonly NaturalStringComparer Instance = new();

            public int Compare(string? x, string? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x is null) return -1;
                if (y is null) return 1;

                var ix = 0;
                var iy = 0;
                while (ix < x.Length && iy < y.Length)
                {
                    if (char.IsDigit(x[ix]) && char.IsDigit(y[iy]))
                    {
                        var sx = ix;
                        var sy = iy;
                        while (ix < x.Length && char.IsDigit(x[ix])) ix++;
                        while (iy < y.Length && char.IsDigit(y[iy])) iy++;

                        var nx = x[sx..ix].TrimStart('0');
                        var ny = y[sy..iy].TrimStart('0');
                        nx = nx.Length == 0 ? "0" : nx;
                        ny = ny.Length == 0 ? "0" : ny;

                        var lengthCompare = nx.Length.CompareTo(ny.Length);
                        if (lengthCompare != 0) return lengthCompare;

                        var numberCompare = string.CompareOrdinal(nx, ny);
                        if (numberCompare != 0) return numberCompare;
                    }
                    else
                    {
                        var cx = char.ToUpperInvariant(x[ix]);
                        var cy = char.ToUpperInvariant(y[iy]);
                        if (cx != cy) return cx.CompareTo(cy);
                        ix++;
                        iy++;
                    }
                }

                return x.Length.CompareTo(y.Length);
            }
        }

        private sealed record AppliedPrice(decimal Precio, Guid TarifaGuid);
    }
}
