using System;

namespace Alojamiento.DataAccess.Entities.Alojamiento
{
    public class SucursalImagenEntity
    {
        public int IdSucursalImagen { get; set; }
        public int IdSucursal { get; set; }
        public string UrlImagen { get; set; } = string.Empty;
        public string? DescripcionImagen { get; set; }
        public string? TipoImagen { get; set; }
        public int OrdenVisualizacion { get; set; }
        public bool EsPrincipal { get; set; }
        public DateTime FechaRegistroUtc { get; set; }
        public string CreadoPorUsuario { get; set; } = string.Empty;
        public byte[] RowVersion { get; set; }

        public SucursalEntity Sucursal { get; set; }
    }
}
