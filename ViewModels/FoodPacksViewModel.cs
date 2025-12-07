using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Allva.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel para el modulo de Pack de Alimentos en el Front-Office
    /// Muestra los packs disponibles para el comercio/local del usuario
    /// </summary>
    public partial class FoodPacksViewModel : ObservableObject
    {
        private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

        // ============================================
        // PROPIEDADES OBSERVABLES
        // ============================================

        [ObservableProperty]
        private ObservableCollection<PackAlimentoFront> _packsDisponibles = new();

        [ObservableProperty]
        private PackAlimentoFront? _packSeleccionado;

        [ObservableProperty]
        private bool _estaCargando;

        [ObservableProperty]
        private string _mensajeEstado = string.Empty;

        [ObservableProperty]
        private bool _hayMensaje;

        [ObservableProperty]
        private bool _mensajeEsError;

        [ObservableProperty]
        private bool _mostrarDetallePack;

        [ObservableProperty]
        private bool _mostrarModalImagen;

        [ObservableProperty]
        private byte[]? _imagenAmpliada;

        [ObservableProperty]
        private string? _imagenAmpliadaNombre;

        private int _idComercio = 0;
        private int _idLocal = 0;

        // ============================================
        // PROPIEDADES CALCULADAS
        // ============================================

        public IBrush MensajeBackground => MensajeEsError
            ? new SolidColorBrush(Color.Parse("#dc3545"))
            : new SolidColorBrush(Color.Parse("#28a745"));

        public bool HayPacks => PacksDisponibles.Count > 0;

        public bool NohayPacks => PacksDisponibles.Count == 0 && !EstaCargando;

        // ============================================
        // CONSTRUCTOR
        // ============================================

        public FoodPacksViewModel()
        {
            _ = CargarPacksDisponiblesAsync();
        }

        public FoodPacksViewModel(int idComercio, int idLocal)
        {
            _idComercio = idComercio;
            _idLocal = idLocal;
            _ = CargarPacksDisponiblesAsync();
        }

        // ============================================
        // METODOS PUBLICOS
        // ============================================

        public async Task InicializarAsync(int idComercio, int idLocal)
        {
            _idComercio = idComercio;
            _idLocal = idLocal;
            await CargarPacksDisponiblesAsync();
        }

        // ============================================
        // COMANDOS
        // ============================================

        [RelayCommand]
        private async Task RefrescarPacksAsync()
        {
            await CargarPacksDisponiblesAsync();
        }

        [RelayCommand]
        private void VerDetallePack(PackAlimentoFront? pack)
        {
            if (pack == null) return;
            PackSeleccionado = pack;
            MostrarDetallePack = true;
        }

        [RelayCommand]
        private void CerrarDetallePack()
        {
            MostrarDetallePack = false;
            PackSeleccionado = null;
        }

        [RelayCommand]
        private void VerImagenAmpliada(byte[]? imagen)
        {
            if (imagen == null || imagen.Length == 0) return;
            ImagenAmpliada = imagen;
            ImagenAmpliadaNombre = "Imagen del Pack";
            MostrarModalImagen = true;
        }

        [RelayCommand]
        private void CerrarModalImagen()
        {
            MostrarModalImagen = false;
            ImagenAmpliada = null;
            ImagenAmpliadaNombre = null;
        }

        [RelayCommand]
        private void ReservarPack(PackAlimentoFront? pack)
        {
            if (pack == null) return;

            // TODO: Implementar logica de reserva completa
            MostrarMensaje($"Pack '{pack.NombrePack}' reservado correctamente", false);
            CerrarDetallePack();
        }

        // ============================================
        // METODOS PRIVADOS
        // ============================================

        private async Task CargarPacksDisponiblesAsync()
        {
            EstaCargando = true;
            PacksDisponibles.Clear();

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                // Obtener packs asignados globalmente o al comercio especifico
                var query = @"
                    SELECT DISTINCT pa.id_pack, pa.nombre_pack, pa.descripcion, 
                           pa.imagen_poster, pa.imagen_poster_nombre,
                           COALESCE(pap.precio, pap_global.precio, 0) as precio,
                           COALESCE(pap.divisa, pap_global.divisa, 'EUR') as divisa
                    FROM packs_alimentos pa
                    LEFT JOIN pack_alimentos_asignacion_comercios paac 
                        ON pa.id_pack = paac.id_pack AND paac.id_comercio = @idComercio AND paac.activo = true
                    LEFT JOIN pack_alimentos_precios pap 
                        ON paac.id_precio = pap.id_precio
                    LEFT JOIN pack_alimentos_asignacion_global paag 
                        ON pa.id_pack = paag.id_pack AND paag.activo = true
                    LEFT JOIN pack_alimentos_precios pap_global 
                        ON paag.id_precio = pap_global.id_precio
                    WHERE pa.activo = true
                      AND (paac.id_asignacion IS NOT NULL OR paag.id_asignacion IS NOT NULL)
                    ORDER BY pa.nombre_pack";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idComercio", _idComercio > 0 ? _idComercio : 1);
                await using var reader = await cmd.ExecuteReaderAsync();

                var packs = new System.Collections.Generic.List<PackAlimentoFront>();

                while (await reader.ReadAsync())
                {
                    var pack = new PackAlimentoFront
                    {
                        IdPack = reader.GetInt32(0),
                        NombrePack = reader.GetString(1),
                        Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2),
                        ImagenPoster = reader.IsDBNull(3) ? null : (byte[])reader["imagen_poster"],
                        ImagenPosterNombre = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Precio = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                        Divisa = reader.IsDBNull(6) ? "EUR" : reader.GetString(6)
                    };
                    packs.Add(pack);
                }

                await reader.CloseAsync();

                // Cargar productos de cada pack
                foreach (var pack in packs)
                {
                    var prodQuery = @"
                        SELECT id_producto, nombre_producto, descripcion, detalles, 
                               cantidad, unidad_medida, imagen, imagen_nombre
                        FROM pack_alimentos_productos
                        WHERE id_pack = @idPack
                        ORDER BY orden";

                    await using var prodCmd = new NpgsqlCommand(prodQuery, conn);
                    prodCmd.Parameters.AddWithValue("@idPack", pack.IdPack);
                    await using var prodReader = await prodCmd.ExecuteReaderAsync();

                    while (await prodReader.ReadAsync())
                    {
                        pack.Productos.Add(new PackAlimentoProductoFront
                        {
                            IdProducto = prodReader.GetInt32(0),
                            NombreProducto = prodReader.GetString(1),
                            Descripcion = prodReader.IsDBNull(2) ? null : prodReader.GetString(2),
                            Detalles = prodReader.IsDBNull(3) ? null : prodReader.GetString(3),
                            Cantidad = prodReader.GetInt32(4),
                            UnidadMedida = prodReader.GetString(5),
                            Imagen = prodReader.IsDBNull(6) ? null : (byte[])prodReader["imagen"],
                            ImagenNombre = prodReader.IsDBNull(7) ? null : prodReader.GetString(7)
                        });
                    }

                    await prodReader.CloseAsync();

                    // Cargar imagenes adicionales
                    var imgQuery = @"
                        SELECT id_imagen, imagen_contenido, imagen_nombre
                        FROM pack_alimentos_imagenes
                        WHERE id_pack = @idPack
                        ORDER BY orden";

                    await using var imgCmd = new NpgsqlCommand(imgQuery, conn);
                    imgCmd.Parameters.AddWithValue("@idPack", pack.IdPack);
                    await using var imgReader = await imgCmd.ExecuteReaderAsync();

                    while (await imgReader.ReadAsync())
                    {
                        pack.ImagenesAdicionales.Add(new PackAlimentoImagenFront
                        {
                            IdImagen = imgReader.GetInt32(0),
                            ImagenContenido = (byte[])imgReader["imagen_contenido"],
                            ImagenNombre = imgReader.IsDBNull(2) ? null : imgReader.GetString(2)
                        });
                    }

                    PacksDisponibles.Add(pack);
                }

                OnPropertyChanged(nameof(HayPacks));
                OnPropertyChanged(nameof(NohayPacks));
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar packs: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
                OnPropertyChanged(nameof(HayPacks));
                OnPropertyChanged(nameof(NohayPacks));
            }
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            MensajeEstado = mensaje;
            MensajeEsError = esError;
            HayMensaje = true;

            Task.Delay(4000).ContinueWith(_ =>
            {
                HayMensaje = false;
            });
        }
    }

    // ============================================
    // MODELOS PARA EL FRONT-OFFICE
    // ============================================

    public class PackAlimentoFront
    {
        public int IdPack { get; set; }
        public string NombrePack { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public byte[]? ImagenPoster { get; set; }
        public string? ImagenPosterNombre { get; set; }
        public decimal Precio { get; set; }
        public string Divisa { get; set; } = "EUR";
        
        public ObservableCollection<PackAlimentoProductoFront> Productos { get; set; } = new();
        public ObservableCollection<PackAlimentoImagenFront> ImagenesAdicionales { get; set; } = new();

        // Propiedades calculadas
        public string PrecioFormateado => $"{Precio:N2} {Divisa}";
        public int CantidadProductos => Productos.Count;
        public bool TieneImagen => ImagenPoster != null && ImagenPoster.Length > 0;
        public bool TieneImagenesAdicionales => ImagenesAdicionales.Count > 0;
    }

    public class PackAlimentoProductoFront
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Detalles { get; set; }
        public int Cantidad { get; set; }
        public string UnidadMedida { get; set; } = "unidad";
        public byte[]? Imagen { get; set; }
        public string? ImagenNombre { get; set; }

        public string CantidadConUnidad => $"{Cantidad} {UnidadMedida}";
        public bool TieneImagen => Imagen != null && Imagen.Length > 0;
    }

    public class PackAlimentoImagenFront
    {
        public int IdImagen { get; set; }
        public byte[] ImagenContenido { get; set; } = Array.Empty<byte>();
        public string? ImagenNombre { get; set; }
    }
}