using Alojamiento.DataAccess.Entities.Alojamiento;
using Alojamiento.DataAccess.Entities.Valoraciones;
using Microsoft.EntityFrameworkCore;

namespace Alojamiento.DataAccess.Context
{
    public class AlojamientoDbContext : DbContext
    {
        public AlojamientoDbContext(DbContextOptions<AlojamientoDbContext> options) : base(options)
        {
        }

        public DbSet<SucursalEntity> Sucursales => Set<SucursalEntity>();
        public DbSet<SucursalImagenEntity> SucursalImagenes => Set<SucursalImagenEntity>();
        public DbSet<TipoHabitacionEntity> TiposHabitacion => Set<TipoHabitacionEntity>();
        public DbSet<TipoHabitacionImagenEntity> TipoHabitacionImagenes => Set<TipoHabitacionImagenEntity>();
        public DbSet<CatalogoServicioEntity> CatalogoServicios => Set<CatalogoServicioEntity>();
        public DbSet<TipoHabitacionCatalogoEntity> TipoHabitacionCatalogos => Set<TipoHabitacionCatalogoEntity>();
        public DbSet<HabitacionEntity> Habitaciones => Set<HabitacionEntity>();
        public DbSet<TarifaEntity> Tarifas => Set<TarifaEntity>();
        public DbSet<ValoracionEntity> Valoraciones => Set<ValoracionEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlojamientoDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
