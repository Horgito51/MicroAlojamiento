using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Alojamiento.DataAccess.Entities.Alojamiento;

namespace Alojamiento.DataAccess.Configurations.Alojamiento
{
    public class SucursalImagenConfiguration : IEntityTypeConfiguration<SucursalImagenEntity>
    {
        public void Configure(EntityTypeBuilder<SucursalImagenEntity> builder)
        {
            builder.ToTable("SUCURSAL_IMAGEN", "alojamiento");
            builder.HasKey(e => e.IdSucursalImagen);

            builder.Property(e => e.IdSucursalImagen).HasColumnName("id_sucursal_imagen").ValueGeneratedOnAdd();
            builder.Property(e => e.IdSucursal).HasColumnName("id_sucursal");
            builder.Property(e => e.UrlImagen).HasColumnName("url_imagen").HasMaxLength(500);
            builder.Property(e => e.DescripcionImagen).HasColumnName("descripcion_imagen").HasMaxLength(255);
            builder.Property(e => e.TipoImagen).HasColumnName("tipo_imagen").HasMaxLength(50);
            builder.Property(e => e.OrdenVisualizacion).HasColumnName("orden_visualizacion");
            builder.Property(e => e.EsPrincipal).HasColumnName("es_principal");
            builder.Property(e => e.FechaRegistroUtc).HasColumnName("fecha_registro_utc");
            builder.Property(e => e.CreadoPorUsuario).HasColumnName("creado_por_usuario").HasMaxLength(100);
            builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion();

            builder.HasOne(e => e.Sucursal)
                .WithMany(s => s.Imagenes)
                .HasForeignKey(e => e.IdSucursal)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(e => e.IdSucursal).HasDatabaseName("IX_SUCURSAL_IMAGENES_SUCURSAL");
            builder.HasCheckConstraint("CHK_SUCURSAL_IMAGEN_ORDEN", "[orden_visualizacion] > 0");
            builder.HasCheckConstraint("CHK_SUCURSAL_IMAGEN_PRINCIPAL", "[es_principal] IN (0,1)");
        }
    }
}
