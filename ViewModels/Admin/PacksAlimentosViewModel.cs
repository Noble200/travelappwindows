using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using Allva.Desktop.Models;

namespace Allva.Desktop.ViewModels.Admin
{
    public partial class PacksAlimentosViewModel : ObservableObject
    {
        private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

        // ============================================
        // PROPIEDADES OBSERVABLES
        // ============================================

        [ObservableProperty]
        private ObservableCollection<PackAlimento> _packs = new();

        [ObservableProperty]
        private PackAlimento? _packSeleccionado;

        [ObservableProperty]
        private bool _estaCargando;

        [ObservableProperty]
        private string _mensajeEstado = string.Empty;

        [ObservableProperty]
        private bool _hayMensaje;

        [ObservableProperty]
        private bool _mensajeEsError;

        // Panel de creacion/edicion
        [ObservableProperty]
        private bool _mostrarPanelEdicion;

        [ObservableProperty]
        private bool _modoEdicion;

        [ObservableProperty]
        private string _tituloPanel = "Nuevo Pack de Alimentos";

        // Datos del formulario
        [ObservableProperty]
        private string _nombrePack = string.Empty;

        [ObservableProperty]
        private string _descripcionPack = string.Empty;

        [ObservableProperty]
        private byte[]? _imagenPoster;

        [ObservableProperty]
        private string? _nombreImagenPoster;

        [ObservableProperty]
        private ObservableCollection<PackAlimentoProducto> _productosActuales = new();

        [ObservableProperty]
        private ObservableCollection<PackAlimentoImagen> _imagenesActuales = new();

        // Formulario de nuevo producto
        [ObservableProperty]
        private string _nuevoProductoNombre = string.Empty;

        [ObservableProperty]
        private string _nuevoProductoDescripcion = string.Empty;

        [ObservableProperty]
        private string _nuevoProductoDetalles = string.Empty;

        [ObservableProperty]
        private string _nuevoProductoCantidadTexto = "1";

        [ObservableProperty]
        private string _nuevoProductoUnidad = "unidad";

        [ObservableProperty]
        private byte[]? _nuevoProductoImagen;

        [ObservableProperty]
        private string? _nuevoProductoImagenNombre;

        // ASIGNACION A COMERCIOS (integrada en creacion)
        [ObservableProperty]
        private bool _asignarATodosLosComercios = true;

        [ObservableProperty]
        private ObservableCollection<ComercioConPrecio> _comerciosParaAsignar = new();

        [ObservableProperty]
        private string _precioGeneralTexto = "0.00";

        [ObservableProperty]
        private string _divisaGeneral = "EUR";

        // Modal de detalles del producto
        [ObservableProperty]
        private bool _mostrarModalDetalles;

        [ObservableProperty]
        private PackAlimentoProducto? _productoParaVerDetalles;

        // Modal de imagen ampliada
        [ObservableProperty]
        private bool _mostrarModalImagen;

        [ObservableProperty]
        private byte[]? _imagenAmpliada;

        [ObservableProperty]
        private string? _imagenAmpliadaNombre;

        // Unidades de medida disponibles
        public ObservableCollection<string> UnidadesMedida { get; } = new()
        {
            "unidad", "paquete", "kg", "g", "l", "ml", "caja", "docena"
        };

        // Divisas disponibles
        public ObservableCollection<string> DivisasDisponibles { get; } = new() { "EUR", "USD" };

        private int _packIdEnEdicion;

        // ============================================
        // PROPIEDADES CALCULADAS PARA UI
        // ============================================

        public IBrush MensajeBackground => MensajeEsError 
            ? new SolidColorBrush(Color.Parse("#dc3545")) 
            : new SolidColorBrush(Color.Parse("#28a745"));

        public string TextoBotonGuardar => ModoEdicion ? "Actualizar Pack" : "Crear Pack";

        public bool TieneImagenProductoNuevo => NuevoProductoImagen != null && NuevoProductoImagen.Length > 0;

        public bool MostrarSeleccionComercios => !AsignarATodosLosComercios;

        // ============================================
        // CONSTRUCTOR
        // ============================================

        public PacksAlimentosViewModel()
        {
            _ = CargarPacksAsync();
        }

        // Notificar cambios
        partial void OnModoEdicionChanged(bool value)
        {
            OnPropertyChanged(nameof(TextoBotonGuardar));
        }

        partial void OnMensajeEsErrorChanged(bool value)
        {
            OnPropertyChanged(nameof(MensajeBackground));
        }

        partial void OnNuevoProductoImagenChanged(byte[]? value)
        {
            OnPropertyChanged(nameof(TieneImagenProductoNuevo));
        }

        partial void OnAsignarATodosLosComerciosChanged(bool value)
        {
            OnPropertyChanged(nameof(MostrarSeleccionComercios));
        }

        // ============================================
        // COMANDOS PRINCIPALES
        // ============================================

        [RelayCommand]
        private async Task NuevoPackAsync()
        {
            LimpiarFormulario();
            ModoEdicion = false;
            TituloPanel = "Nuevo Pack de Alimentos";
            await CargarComerciosAsync();
            MostrarPanelEdicion = true;
        }

        [RelayCommand]
        private async Task EditarPackAsync(PackAlimento? pack)
        {
            if (pack == null) return;

            _packIdEnEdicion = pack.IdPack;
            NombrePack = pack.NombrePack;
            DescripcionPack = pack.Descripcion ?? string.Empty;
            ImagenPoster = pack.ImagenPoster;
            NombreImagenPoster = pack.ImagenPosterNombre;

            ProductosActuales.Clear();
            foreach (var p in pack.Productos)
            {
                ProductosActuales.Add(p);
            }

            ImagenesActuales.Clear();
            foreach (var img in pack.Imagenes)
            {
                ImagenesActuales.Add(img);
            }

            await CargarComerciosAsync();
            await CargarAsignacionesExistentesAsync(pack.IdPack);

            ModoEdicion = true;
            TituloPanel = $"Editar: {pack.NombrePack}";
            MostrarPanelEdicion = true;
        }

        [RelayCommand]
        private void CancelarEdicion()
        {
            LimpiarFormulario();
            MostrarPanelEdicion = false;
        }

        [RelayCommand]
        private async Task GuardarPackAsync()
        {
            if (string.IsNullOrWhiteSpace(NombrePack))
            {
                MostrarMensaje("El nombre del pack es requerido", true);
                return;
            }

            if (ProductosActuales.Count == 0)
            {
                MostrarMensaje("Debe agregar al menos un producto al pack", true);
                return;
            }

            // Validar precio
            if (!decimal.TryParse(PrecioGeneralTexto.Replace(",", "."), System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out decimal precioGeneral) || precioGeneral <= 0)
            {
                MostrarMensaje("El precio debe ser mayor a 0", true);
                return;
            }

            // Validar comercios seleccionados
            if (!AsignarATodosLosComercios)
            {
                var comerciosSeleccionados = ComerciosParaAsignar.Where(c => c.Seleccionado).ToList();
                if (comerciosSeleccionados.Count == 0)
                {
                    MostrarMensaje("Debe seleccionar al menos un comercio", true);
                    return;
                }
            }

            EstaCargando = true;

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                int idPack;

                if (ModoEdicion)
                {
                    var updateQuery = @"
                        UPDATE packs_alimentos 
                        SET nombre_pack = @nombre,
                            descripcion = @descripcion,
                            imagen_poster = @imagen,
                            imagen_poster_nombre = @imagenNombre,
                            fecha_modificacion = CURRENT_TIMESTAMP
                        WHERE id_pack = @idPack";

                    await using var cmd = new NpgsqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@nombre", NombrePack);
                    cmd.Parameters.AddWithValue("@descripcion", (object?)DescripcionPack ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@imagen", (object?)ImagenPoster ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@imagenNombre", (object?)NombreImagenPoster ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@idPack", _packIdEnEdicion);
                    await cmd.ExecuteNonQueryAsync();

                    idPack = _packIdEnEdicion;

                    // Limpiar datos anteriores
                    await using var deleteCmd = new NpgsqlCommand(
                        "DELETE FROM pack_alimentos_productos WHERE id_pack = @idPack", conn);
                    deleteCmd.Parameters.AddWithValue("@idPack", idPack);
                    await deleteCmd.ExecuteNonQueryAsync();

                    await using var deleteImgCmd = new NpgsqlCommand(
                        "DELETE FROM pack_alimentos_imagenes WHERE id_pack = @idPack", conn);
                    deleteImgCmd.Parameters.AddWithValue("@idPack", idPack);
                    await deleteImgCmd.ExecuteNonQueryAsync();

                    // Limpiar asignaciones anteriores
                    await using var deleteAsigGlobal = new NpgsqlCommand(
                        "DELETE FROM pack_alimentos_asignacion_global WHERE id_pack = @idPack", conn);
                    deleteAsigGlobal.Parameters.AddWithValue("@idPack", idPack);
                    await deleteAsigGlobal.ExecuteNonQueryAsync();

                    await using var deleteAsigCom = new NpgsqlCommand(
                        "DELETE FROM pack_alimentos_asignacion_comercios WHERE id_pack = @idPack", conn);
                    deleteAsigCom.Parameters.AddWithValue("@idPack", idPack);
                    await deleteAsigCom.ExecuteNonQueryAsync();

                    await using var deletePrecios = new NpgsqlCommand(
                        "DELETE FROM pack_alimentos_precios WHERE id_pack = @idPack", conn);
                    deletePrecios.Parameters.AddWithValue("@idPack", idPack);
                    await deletePrecios.ExecuteNonQueryAsync();
                }
                else
                {
                    var insertQuery = @"
                        INSERT INTO packs_alimentos (nombre_pack, descripcion, imagen_poster, imagen_poster_nombre)
                        VALUES (@nombre, @descripcion, @imagen, @imagenNombre)
                        RETURNING id_pack";

                    await using var cmd = new NpgsqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@nombre", NombrePack);
                    cmd.Parameters.AddWithValue("@descripcion", (object?)DescripcionPack ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@imagen", (object?)ImagenPoster ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@imagenNombre", (object?)NombreImagenPoster ?? DBNull.Value);

                    var result = await cmd.ExecuteScalarAsync();
                    idPack = Convert.ToInt32(result);
                }

                // Insertar productos
                int orden = 0;
                foreach (var producto in ProductosActuales)
                {
                    var insertProd = @"
                        INSERT INTO pack_alimentos_productos 
                        (id_pack, nombre_producto, descripcion, detalles, cantidad, unidad_medida, orden, imagen, imagen_nombre, imagen_tipo)
                        VALUES (@idPack, @nombre, @descripcion, @detalles, @cantidad, @unidad, @orden, @imagen, @imagenNombre, @imagenTipo)";

                    await using var prodCmd = new NpgsqlCommand(insertProd, conn);
                    prodCmd.Parameters.AddWithValue("@idPack", idPack);
                    prodCmd.Parameters.AddWithValue("@nombre", producto.NombreProducto);
                    prodCmd.Parameters.AddWithValue("@descripcion", (object?)producto.Descripcion ?? DBNull.Value);
                    prodCmd.Parameters.AddWithValue("@detalles", (object?)producto.Detalles ?? DBNull.Value);
                    prodCmd.Parameters.AddWithValue("@cantidad", producto.Cantidad);
                    prodCmd.Parameters.AddWithValue("@unidad", producto.UnidadMedida);
                    prodCmd.Parameters.AddWithValue("@orden", orden++);
                    prodCmd.Parameters.AddWithValue("@imagen", (object?)producto.Imagen ?? DBNull.Value);
                    prodCmd.Parameters.AddWithValue("@imagenNombre", (object?)producto.ImagenNombre ?? DBNull.Value);
                    prodCmd.Parameters.AddWithValue("@imagenTipo", (object?)producto.ImagenTipo ?? DBNull.Value);
                    await prodCmd.ExecuteNonQueryAsync();
                }

                // Insertar imagenes adicionales
                orden = 0;
                foreach (var imagen in ImagenesActuales)
                {
                    var insertImg = @"
                        INSERT INTO pack_alimentos_imagenes 
                        (id_pack, imagen_contenido, imagen_nombre, imagen_tipo, descripcion, orden)
                        VALUES (@idPack, @contenido, @nombre, @tipo, @descripcion, @orden)";

                    await using var imgCmd = new NpgsqlCommand(insertImg, conn);
                    imgCmd.Parameters.AddWithValue("@idPack", idPack);
                    imgCmd.Parameters.AddWithValue("@contenido", imagen.ImagenContenido);
                    imgCmd.Parameters.AddWithValue("@nombre", (object?)imagen.ImagenNombre ?? DBNull.Value);
                    imgCmd.Parameters.AddWithValue("@tipo", (object?)imagen.ImagenTipo ?? DBNull.Value);
                    imgCmd.Parameters.AddWithValue("@descripcion", (object?)imagen.Descripcion ?? DBNull.Value);
                    imgCmd.Parameters.AddWithValue("@orden", orden++);
                    await imgCmd.ExecuteNonQueryAsync();
                }

                // Crear precio
                var insertPrecio = @"
                    INSERT INTO pack_alimentos_precios (id_pack, divisa, precio)
                    VALUES (@idPack, @divisa, @precio)
                    RETURNING id_precio";

                await using var precioCmd = new NpgsqlCommand(insertPrecio, conn);
                precioCmd.Parameters.AddWithValue("@idPack", idPack);
                precioCmd.Parameters.AddWithValue("@divisa", DivisaGeneral);
                precioCmd.Parameters.AddWithValue("@precio", precioGeneral);
                var idPrecio = Convert.ToInt32(await precioCmd.ExecuteScalarAsync());

                // Crear asignaciones
                if (AsignarATodosLosComercios)
                {
                    var insertGlobal = @"
                        INSERT INTO pack_alimentos_asignacion_global (id_pack, id_precio)
                        VALUES (@idPack, @idPrecio)";

                    await using var globalCmd = new NpgsqlCommand(insertGlobal, conn);
                    globalCmd.Parameters.AddWithValue("@idPack", idPack);
                    globalCmd.Parameters.AddWithValue("@idPrecio", idPrecio);
                    await globalCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    foreach (var comercio in ComerciosParaAsignar.Where(c => c.Seleccionado))
                    {
                        var insertCom = @"
                            INSERT INTO pack_alimentos_asignacion_comercios (id_pack, id_comercio, id_precio)
                            VALUES (@idPack, @idComercio, @idPrecio)";

                        await using var comCmd = new NpgsqlCommand(insertCom, conn);
                        comCmd.Parameters.AddWithValue("@idPack", idPack);
                        comCmd.Parameters.AddWithValue("@idComercio", comercio.IdComercio);
                        comCmd.Parameters.AddWithValue("@idPrecio", idPrecio);
                        await comCmd.ExecuteNonQueryAsync();
                    }
                }

                MostrarMensaje(ModoEdicion ? "Pack actualizado correctamente" : "Pack creado correctamente", false);
                MostrarPanelEdicion = false;
                LimpiarFormulario();
                await CargarPacksAsync();
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al guardar: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        [RelayCommand]
        private async Task EliminarPackAsync(PackAlimento? pack)
        {
            if (pack == null) return;

            EstaCargando = true;

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(
                    "DELETE FROM packs_alimentos WHERE id_pack = @idPack", conn);
                cmd.Parameters.AddWithValue("@idPack", pack.IdPack);
                await cmd.ExecuteNonQueryAsync();

                MostrarMensaje("Pack eliminado correctamente", false);
                await CargarPacksAsync();
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al eliminar: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        // ============================================
        // COMANDOS DE PRODUCTOS
        // ============================================

        [RelayCommand]
        private void AgregarProducto()
        {
            if (string.IsNullOrWhiteSpace(NuevoProductoNombre))
            {
                MostrarMensaje("El nombre del producto es requerido", true);
                return;
            }

            if (!int.TryParse(NuevoProductoCantidadTexto, out int cantidad) || cantidad <= 0)
            {
                MostrarMensaje("La cantidad debe ser un numero valido mayor a 0", true);
                return;
            }

            var producto = new PackAlimentoProducto
            {
                NombreProducto = NuevoProductoNombre,
                Descripcion = NuevoProductoDescripcion,
                Detalles = NuevoProductoDetalles,
                Cantidad = cantidad,
                UnidadMedida = NuevoProductoUnidad,
                Orden = ProductosActuales.Count,
                Imagen = NuevoProductoImagen,
                ImagenNombre = NuevoProductoImagenNombre,
                ImagenTipo = NuevoProductoImagenNombre != null ? Path.GetExtension(NuevoProductoImagenNombre) : null
            };

            ProductosActuales.Add(producto);
            LimpiarFormularioProducto();
            MostrarMensaje("Producto agregado al pack", false);
        }

        [RelayCommand]
        private void EliminarProducto(PackAlimentoProducto? producto)
        {
            if (producto == null) return;
            ProductosActuales.Remove(producto);
        }

        [RelayCommand]
        private void MoverProductoArriba(PackAlimentoProducto? producto)
        {
            if (producto == null) return;
            var index = ProductosActuales.IndexOf(producto);
            if (index > 0)
            {
                ProductosActuales.Move(index, index - 1);
            }
        }

        [RelayCommand]
        private void MoverProductoAbajo(PackAlimentoProducto? producto)
        {
            if (producto == null) return;
            var index = ProductosActuales.IndexOf(producto);
            if (index < ProductosActuales.Count - 1)
            {
                ProductosActuales.Move(index, index + 1);
            }
        }

        [RelayCommand]
        private async Task SeleccionarImagenProductoAsync(Window? window)
        {
            if (window == null) return;

            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar imagen del producto",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Imagenes") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif" } }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                await using var stream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                NuevoProductoImagen = ms.ToArray();
                NuevoProductoImagenNombre = file.Name;
            }
        }

        [RelayCommand]
        private void QuitarImagenProducto()
        {
            NuevoProductoImagen = null;
            NuevoProductoImagenNombre = null;
        }

        // ============================================
        // COMANDOS DE MODALES
        // ============================================

        [RelayCommand]
        private void VerDetallesProducto(PackAlimentoProducto? producto)
        {
            if (producto == null) return;
            ProductoParaVerDetalles = producto;
            MostrarModalDetalles = true;
        }

        [RelayCommand]
        private void CerrarModalDetalles()
        {
            MostrarModalDetalles = false;
            ProductoParaVerDetalles = null;
        }

        [RelayCommand]
        private void VerImagenAmpliada(PackAlimentoProducto? producto)
        {
            if (producto?.Imagen == null) return;
            ImagenAmpliada = producto.Imagen;
            ImagenAmpliadaNombre = producto.ImagenNombre ?? producto.NombreProducto;
            MostrarModalImagen = true;
        }

        [RelayCommand]
        private void VerImagenAmpliadaGeneral(byte[]? imagen)
        {
            if (imagen == null || imagen.Length == 0) return;
            ImagenAmpliada = imagen;
            ImagenAmpliadaNombre = "Imagen";
            MostrarModalImagen = true;
        }

        [RelayCommand]
        private void CerrarModalImagen()
        {
            MostrarModalImagen = false;
            ImagenAmpliada = null;
            ImagenAmpliadaNombre = null;
        }

        // ============================================
        // COMANDOS DE IMAGENES DEL PACK
        // ============================================

        [RelayCommand]
        private async Task SeleccionarImagenPosterAsync(Window? window)
        {
            if (window == null) return;

            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar imagen poster",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Imagenes") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif" } }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                await using var stream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                ImagenPoster = ms.ToArray();
                NombreImagenPoster = file.Name;
            }
        }

        [RelayCommand]
        private async Task AgregarImagenAdicionalAsync(Window? window)
        {
            if (window == null) return;

            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar imagenes",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Imagenes") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif" } }
                }
            });

            foreach (var file in files)
            {
                await using var stream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                var imagen = new PackAlimentoImagen
                {
                    ImagenContenido = ms.ToArray(),
                    ImagenNombre = file.Name,
                    ImagenTipo = Path.GetExtension(file.Name),
                    Orden = ImagenesActuales.Count
                };

                ImagenesActuales.Add(imagen);
            }
        }

        [RelayCommand]
        private void EliminarImagen(PackAlimentoImagen? imagen)
        {
            if (imagen == null) return;
            ImagenesActuales.Remove(imagen);
        }

        [RelayCommand]
        private void EliminarImagenPoster()
        {
            ImagenPoster = null;
            NombreImagenPoster = null;
        }

        // ============================================
        // METODOS PRIVADOS
        // ============================================

        private async Task CargarPacksAsync()
        {
            EstaCargando = true;

            try
            {
                Packs.Clear();

                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var query = @"
                    SELECT id_pack, nombre_pack, descripcion, imagen_poster, 
                           imagen_poster_nombre, activo, fecha_creacion
                    FROM packs_alimentos
                    ORDER BY fecha_creacion DESC";

                await using var cmd = new NpgsqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                var packs = new System.Collections.Generic.List<PackAlimento>();

                while (await reader.ReadAsync())
                {
                    packs.Add(new PackAlimento
                    {
                        IdPack = reader.GetInt32(0),
                        NombrePack = reader.GetString(1),
                        Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2),
                        ImagenPoster = reader.IsDBNull(3) ? null : (byte[])reader["imagen_poster"],
                        ImagenPosterNombre = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Activo = reader.GetBoolean(5),
                        FechaCreacion = reader.GetDateTime(6)
                    });
                }

                await reader.CloseAsync();

                foreach (var pack in packs)
                {
                    var prodQuery = @"
                        SELECT id_producto, nombre_producto, descripcion, detalles, cantidad, unidad_medida, orden, imagen, imagen_nombre, imagen_tipo
                        FROM pack_alimentos_productos
                        WHERE id_pack = @idPack
                        ORDER BY orden";

                    await using var prodCmd = new NpgsqlCommand(prodQuery, conn);
                    prodCmd.Parameters.AddWithValue("@idPack", pack.IdPack);
                    await using var prodReader = await prodCmd.ExecuteReaderAsync();

                    while (await prodReader.ReadAsync())
                    {
                        pack.Productos.Add(new PackAlimentoProducto
                        {
                            IdProducto = prodReader.GetInt32(0),
                            IdPack = pack.IdPack,
                            NombreProducto = prodReader.GetString(1),
                            Descripcion = prodReader.IsDBNull(2) ? null : prodReader.GetString(2),
                            Detalles = prodReader.IsDBNull(3) ? null : prodReader.GetString(3),
                            Cantidad = prodReader.GetInt32(4),
                            UnidadMedida = prodReader.GetString(5),
                            Orden = prodReader.GetInt32(6),
                            Imagen = prodReader.IsDBNull(7) ? null : (byte[])prodReader["imagen"],
                            ImagenNombre = prodReader.IsDBNull(8) ? null : prodReader.GetString(8),
                            ImagenTipo = prodReader.IsDBNull(9) ? null : prodReader.GetString(9)
                        });
                    }

                    await prodReader.CloseAsync();

                    var imgQuery = @"
                        SELECT id_imagen, imagen_contenido, imagen_nombre, imagen_tipo, descripcion, orden
                        FROM pack_alimentos_imagenes
                        WHERE id_pack = @idPack
                        ORDER BY orden";

                    await using var imgCmd = new NpgsqlCommand(imgQuery, conn);
                    imgCmd.Parameters.AddWithValue("@idPack", pack.IdPack);
                    await using var imgReader = await imgCmd.ExecuteReaderAsync();

                    while (await imgReader.ReadAsync())
                    {
                        pack.Imagenes.Add(new PackAlimentoImagen
                        {
                            IdImagen = imgReader.GetInt32(0),
                            IdPack = pack.IdPack,
                            ImagenContenido = (byte[])imgReader["imagen_contenido"],
                            ImagenNombre = imgReader.IsDBNull(2) ? null : imgReader.GetString(2),
                            ImagenTipo = imgReader.IsDBNull(3) ? null : imgReader.GetString(3),
                            Descripcion = imgReader.IsDBNull(4) ? null : imgReader.GetString(4),
                            Orden = imgReader.GetInt32(5)
                        });
                    }

                    Packs.Add(pack);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar packs: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private async Task CargarComerciosAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                ComerciosParaAsignar.Clear();

                var query = "SELECT id_comercio, nombre_comercio FROM comercios WHERE activo = true ORDER BY nombre_comercio";
                await using var cmd = new NpgsqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    ComerciosParaAsignar.Add(new ComercioConPrecio
                    {
                        IdComercio = reader.GetInt32(0),
                        NombreComercio = reader.GetString(1),
                        Seleccionado = false
                    });
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar comercios: {ex.Message}", true);
            }
        }

        private async Task CargarAsignacionesExistentesAsync(int idPack)
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                // Verificar si es asignacion global
                var globalQuery = @"
                    SELECT ag.id_asignacion, p.divisa, p.precio
                    FROM pack_alimentos_asignacion_global ag
                    INNER JOIN pack_alimentos_precios p ON ag.id_precio = p.id_precio
                    WHERE ag.id_pack = @idPack AND ag.activo = true
                    LIMIT 1";

                await using var globalCmd = new NpgsqlCommand(globalQuery, conn);
                globalCmd.Parameters.AddWithValue("@idPack", idPack);
                await using var globalReader = await globalCmd.ExecuteReaderAsync();

                if (await globalReader.ReadAsync())
                {
                    AsignarATodosLosComercios = true;
                    DivisaGeneral = globalReader.GetString(1);
                    PrecioGeneralTexto = globalReader.GetDecimal(2).ToString("F2");
                    await globalReader.CloseAsync();
                    return;
                }
                await globalReader.CloseAsync();

                // Si no es global, cargar comercios asignados
                AsignarATodosLosComercios = false;

                var comQuery = @"
                    SELECT ac.id_comercio, p.divisa, p.precio
                    FROM pack_alimentos_asignacion_comercios ac
                    INNER JOIN pack_alimentos_precios p ON ac.id_precio = p.id_precio
                    WHERE ac.id_pack = @idPack AND ac.activo = true";

                await using var comCmd = new NpgsqlCommand(comQuery, conn);
                comCmd.Parameters.AddWithValue("@idPack", idPack);
                await using var comReader = await comCmd.ExecuteReaderAsync();

                bool first = true;
                while (await comReader.ReadAsync())
                {
                    var idComercio = comReader.GetInt32(0);
                    var comercio = ComerciosParaAsignar.FirstOrDefault(c => c.IdComercio == idComercio);
                    if (comercio != null)
                    {
                        comercio.Seleccionado = true;
                    }

                    if (first)
                    {
                        DivisaGeneral = comReader.GetString(1);
                        PrecioGeneralTexto = comReader.GetDecimal(2).ToString("F2");
                        first = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar asignaciones: {ex.Message}", true);
            }
        }

        private void LimpiarFormulario()
        {
            _packIdEnEdicion = 0;
            NombrePack = string.Empty;
            DescripcionPack = string.Empty;
            ImagenPoster = null;
            NombreImagenPoster = null;
            ProductosActuales.Clear();
            ImagenesActuales.Clear();
            LimpiarFormularioProducto();

            // Asignacion
            AsignarATodosLosComercios = true;
            PrecioGeneralTexto = "0.00";
            DivisaGeneral = "EUR";
            foreach (var c in ComerciosParaAsignar)
            {
                c.Seleccionado = false;
            }
        }

        private void LimpiarFormularioProducto()
        {
            NuevoProductoNombre = string.Empty;
            NuevoProductoDescripcion = string.Empty;
            NuevoProductoDetalles = string.Empty;
            NuevoProductoCantidadTexto = "1";
            NuevoProductoUnidad = "unidad";
            NuevoProductoImagen = null;
            NuevoProductoImagenNombre = null;
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

    // Clase auxiliar para comercio con seleccion
    public class ComercioConPrecio : ObservableObject
    {
        public int IdComercio { get; set; }
        public string NombreComercio { get; set; } = string.Empty;

        private bool _seleccionado;
        public bool Seleccionado
        {
            get => _seleccionado;
            set => SetProperty(ref _seleccionado, value);
        }
    }
}