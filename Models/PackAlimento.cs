using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Allva.Desktop.Models
{
    /// <summary>
    /// Representa un pack de alimentos
    /// </summary>
    public class PackAlimento
    {
        public int IdPack { get; set; }
        public string NombrePack { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public byte[]? ImagenPoster { get; set; }
        public string? ImagenPosterNombre { get; set; }
        public string? ImagenPosterTipo { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? CreadoPor { get; set; }
        public int? ModificadoPor { get; set; }

        // Colecciones relacionadas
        public ObservableCollection<PackAlimentoProducto> Productos { get; set; } = new();
        public ObservableCollection<PackAlimentoImagen> Imagenes { get; set; } = new();
        public ObservableCollection<PackAlimentoPrecio> Precios { get; set; } = new();

        // Propiedades de visualizacion
        public string EstadoTexto => Activo ? "Activo" : "Inactivo";
        public int CantidadProductos => Productos?.Count ?? 0;
        public string ResumenProductos => $"{CantidadProductos} producto(s)";
    }

    /// <summary>
    /// Producto individual dentro de un pack
    /// </summary>
    public class PackAlimentoProducto
    {
        public int IdProducto { get; set; }
        public int IdPack { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Detalles { get; set; }
        public int Cantidad { get; set; } = 1;
        public string UnidadMedida { get; set; } = "unidad";
        public int Orden { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Imagen del producto
        public byte[]? Imagen { get; set; }
        public string? ImagenNombre { get; set; }
        public string? ImagenTipo { get; set; }

        // Propiedades de visualizacion
        public string CantidadConUnidad => $"{Cantidad} {UnidadMedida}";
        public bool TieneImagen => Imagen != null && Imagen.Length > 0;
        public bool TieneDetalles => !string.IsNullOrWhiteSpace(Detalles);
        public string ResumenDetalles => TieneDetalles 
            ? (Detalles!.Length > 50 ? Detalles.Substring(0, 50) + "..." : Detalles) 
            : "Sin detalles";
    }

    /// <summary>
    /// Imagen adicional del pack
    /// </summary>
    public class PackAlimentoImagen
    {
        public int IdImagen { get; set; }
        public int IdPack { get; set; }
        public byte[] ImagenContenido { get; set; } = Array.Empty<byte>();
        public string? ImagenNombre { get; set; }
        public string? ImagenTipo { get; set; }
        public string? Descripcion { get; set; }
        public int Orden { get; set; }
        public DateTime FechaSubida { get; set; }
    }

    /// <summary>
    /// Precio del pack en una divisa especifica
    /// </summary>
    public class PackAlimentoPrecio
    {
        public int IdPrecio { get; set; }
        public int IdPack { get; set; }
        public string Divisa { get; set; } = "EUR";
        public decimal Precio { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        // Propiedad de visualizacion
        public string PrecioFormateado => Divisa switch
        {
            "EUR" => $"{Precio:N2} EUR",
            "USD" => $"{Precio:N2} USD",
            _ => $"{Precio:N2} {Divisa}"
        };

        public string SimboloDivisa => Divisa switch
        {
            "EUR" => "EUR",
            "USD" => "$",
            _ => Divisa
        };
    }

    /// <summary>
    /// Tipo de asignacion del pack
    /// </summary>
    public enum TipoAsignacionPack
    {
        Global,      // Todos los comercios
        Comercio,    // Un comercio especifico
        Local        // Un local especifico
    }

    /// <summary>
    /// Asignacion de pack a un destino
    /// </summary>
    public class PackAlimentoAsignacion
    {
        public int IdAsignacion { get; set; }
        public int IdPack { get; set; }
        public TipoAsignacionPack TipoAsignacion { get; set; }
        public int? IdComercio { get; set; }
        public int? IdLocal { get; set; }
        public int IdPrecio { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaAsignacion { get; set; }
        public int? AsignadoPor { get; set; }

        // Info adicional para visualizacion
        public string? NombreComercio { get; set; }
        public string? NombreLocal { get; set; }
        public string? CodigoLocal { get; set; }
        public PackAlimentoPrecio? Precio { get; set; }

        public string DestinoTexto => TipoAsignacion switch
        {
            TipoAsignacionPack.Global => "Todos los comercios",
            TipoAsignacionPack.Comercio => NombreComercio ?? "Comercio",
            TipoAsignacionPack.Local => $"{NombreLocal} ({CodigoLocal})" ?? "Local",
            _ => "Desconocido"
        };
    }

    /// <summary>
    /// Comercio para seleccion
    /// </summary>
    public class ComercioSeleccion
    {
        public int IdComercio { get; set; }
        public string NombreComercio { get; set; } = string.Empty;
        public bool Seleccionado { get; set; }
    }

    /// <summary>
    /// Local para seleccion
    /// </summary>
    public class LocalSeleccion
    {
        public int IdLocal { get; set; }
        public int IdComercio { get; set; }
        public string NombreLocal { get; set; } = string.Empty;
        public string CodigoLocal { get; set; } = string.Empty;
        public string NombreComercio { get; set; } = string.Empty;
        public bool Seleccionado { get; set; }

        public string NombreCompleto => $"{NombreLocal} ({CodigoLocal}) - {NombreComercio}";
    }

    /// <summary>
    /// Divisa disponible para precios
    /// </summary>
    public class DivisaDisponible
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Simbolo { get; set; } = string.Empty;
    }
}