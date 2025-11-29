using System;

namespace Allva.Desktop.Models;

/// <summary>
/// Modelo de Cliente para operaciones de divisas
/// </summary>
public class ClienteModel
{
    public int IdCliente { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Nacionalidad { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = "DNI"; // DNI, NIE, Pasaporte
    public string NumeroDocumento { get; set; } = string.Empty;
    public DateTime? CaducidadDocumento { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public byte[]? ImagenDocumento { get; set; }
    public string NombreArchivoDocumento { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    
    // Propiedades calculadas
    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
    public string DocumentoCompleto => $"{TipoDocumento}: {NumeroDocumento}";
    public string CaducidadTexto => CaducidadDocumento?.ToString("dd/MM/yyyy") ?? "Sin fecha";
    public string FechaNacimientoTexto => FechaNacimiento?.ToString("dd/MM/yyyy") ?? "";
    public bool TieneImagenDocumento => ImagenDocumento != null && ImagenDocumento.Length > 0;
}

/// <summary>
/// Modelo de Divisa para mostrar en la lista
/// </summary>
public class DivisaModel
{
    public string Codigo { get; set; } = string.Empty; // USD, EUR, GBP
    public string Nombre { get; set; } = string.Empty; // Dólar USA, Euro, Libra
    public string Pais { get; set; } = string.Empty;
    public decimal TasaCambioOriginal { get; set; } // Tasa real del mercado
    public decimal TasaCambioConMargen { get; set; } // Tasa con margen aplicado (OCULTA al usuario)
    public bool EsFavorita { get; set; } = false;
    public DateTime UltimaActualizacion { get; set; } = DateTime.Now;
    
    // Solo mostrar la tasa CON margen aplicado (el usuario no ve la original)
    public string TasaTexto => $"1 {Codigo} = {TasaCambioConMargen:N4} EUR";
    public string NombreCompleto => $"{Pais}, {Nombre}";
}

/// <summary>
/// Modelo para el resultado de conversión
/// </summary>
public class ResultadoConversionModel
{
    public string DivisaOrigen { get; set; } = string.Empty;
    public string DivisaDestino { get; set; } = "EUR";
    public decimal CantidadOrigen { get; set; }
    public decimal CantidadDestino { get; set; }
    public decimal TasaAplicada { get; set; } // Tasa con margen (la que ve el usuario)
    
    // Campos internos (NUNCA mostrar al usuario)
    internal decimal TasaReal { get; set; }
    internal decimal MargenPorcentaje { get; set; }
    internal decimal BeneficioAllva { get; set; }
}