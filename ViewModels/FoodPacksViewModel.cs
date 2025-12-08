using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models;
using Npgsql;

namespace Allva.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel para el modulo de Pack de Alimentos en el Front-Office
    /// Muestra los packs disponibles para el comercio/local del usuario
    /// Incluye funcionalidad de busqueda de clientes
    /// </summary>
    public partial class FoodPacksViewModel : ObservableObject
    {
        private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

        // ============================================
        // PROPIEDADES OBSERVABLES - PACKS
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

        // ============================================
        // PROPIEDADES OBSERVABLES - CLIENTE
        // ============================================

        [ObservableProperty]
        private ClienteModel? _clienteSeleccionado;

        [ObservableProperty]
        private ObservableCollection<ClienteModel> _clientesEncontrados = new();

        [ObservableProperty]
        private string _busquedaCliente = string.Empty;

        public ObservableCollection<string> TiposBusquedaCliente { get; } = new()
        {
            "Todos", "Nombre", "Documento", "Telefono"
        };

        [ObservableProperty]
        private string _tipoBusquedaSeleccionado = "Todos";

        [ObservableProperty]
        private bool _mostrarSugerenciasCliente = false;

        [ObservableProperty]
        private ObservableCollection<ClienteModel> _clientesFiltrados = new();

        // ============================================
        // NAVEGACION ENTRE VISTAS
        // ============================================

        [ObservableProperty]
        private bool _vistaPrincipal = true;

        [ObservableProperty]
        private bool _vistaBuscarCliente = false;

        [ObservableProperty]
        private bool _vistaNuevoCliente = false;

        [ObservableProperty]
        private bool _vistaConfirmacionCompra = false;

        // ============================================
        // CAMPOS NUEVO CLIENTE
        // ============================================

        [ObservableProperty]
        private string _nuevoNombre = string.Empty;

        [ObservableProperty]
        private string _nuevoSegundoNombre = string.Empty;

        [ObservableProperty]
        private string _nuevoApellido = string.Empty;

        [ObservableProperty]
        private string _nuevoSegundoApellido = string.Empty;

        [ObservableProperty]
        private string _nuevoTelefono = string.Empty;

        [ObservableProperty]
        private string _nuevaDireccion = string.Empty;

        [ObservableProperty]
        private string _nuevaNacionalidad = string.Empty;

        [ObservableProperty]
        private string _nuevoTipoDocumento = "DNI";

        [ObservableProperty]
        private string _nuevoNumeroDocumento = string.Empty;

        public ObservableCollection<string> TiposDocumento { get; } = new() { "DNI", "NIE", "Pasaporte" };

        // ============================================
        // ESTADO DE OPERACION
        // ============================================

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _puedeRealizarCompra = false;

        private int _idComercio = 0;
        private int _idLocal = 0;
        private int _idUsuario = 0;

        // ============================================
        // PROPIEDADES CALCULADAS
        // ============================================

        public IBrush MensajeBackground => MensajeEsError
            ? new SolidColorBrush(Color.Parse("#dc3545"))
            : new SolidColorBrush(Color.Parse("#28a745"));

        public bool HayPacks => PacksDisponibles.Count > 0;

        public bool NohayPacks => PacksDisponibles.Count == 0 && !EstaCargando;

        public string ClienteSeleccionadoNombre => ClienteSeleccionado?.NombreCompleto ?? "Sin cliente seleccionado";

        public string ClienteSeleccionadoDocumento => ClienteSeleccionado?.DocumentoCompleto ?? "";

        public bool TieneClienteSeleccionado => ClienteSeleccionado != null;

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

        public async Task InicializarAsync(int idComercio, int idLocal, int idUsuario = 0)
        {
            _idComercio = idComercio;
            _idLocal = idLocal;
            _idUsuario = idUsuario;
            await CargarPacksDisponiblesAsync();
        }

        // ============================================
        // COMANDOS - NAVEGACION
        // ============================================

        private void OcultarTodasLasVistas()
        {
            VistaPrincipal = false;
            VistaBuscarCliente = false;
            VistaNuevoCliente = false;
            VistaConfirmacionCompra = false;
        }

        [RelayCommand]
        private void VolverAPrincipal()
        {
            OcultarTodasLasVistas();
            VistaPrincipal = true;
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private void IrABuscarCliente()
        {
            OcultarTodasLasVistas();
            VistaBuscarCliente = true;
            BusquedaCliente = string.Empty;
            ClientesEncontrados.Clear();
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private void IrANuevoCliente()
        {
            LimpiarFormularioCliente();
            OcultarTodasLasVistas();
            VistaNuevoCliente = true;
            ErrorMessage = string.Empty;
        }

        // ============================================
        // COMANDOS - PACKS
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
        private void ComprarPack(PackAlimentoFront? pack)
        {
            if (pack == null) return;

            if (ClienteSeleccionado == null)
            {
                MostrarMensaje("Debe seleccionar un cliente antes de comprar", true);
                return;
            }

            PackSeleccionado = pack;
            CerrarDetallePack();
            // El modal de confirmacion es un overlay, no oculta las vistas
            VistaConfirmacionCompra = true;
        }

        [RelayCommand]
        private async Task ConfirmarCompraAsync()
        {
            if (PackSeleccionado == null || ClienteSeleccionado == null)
            {
                MostrarMensaje("Debe seleccionar un pack y un cliente", true);
                return;
            }

            EstaCargando = true;

            try
            {
                // TODO: Implementar logica de compra completa (registro en operaciones)
                await Task.Delay(500); // Simulacion

                var nombrePack = PackSeleccionado.NombrePack;
                var nombreCliente = ClienteSeleccionado.NombreCompleto;

                VistaConfirmacionCompra = false;
                PackSeleccionado = null;
                
                MostrarMensaje($"Pack '{nombrePack}' comprado correctamente para {nombreCliente}", false);
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al procesar compra: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        [RelayCommand]
        private void CancelarCompra()
        {
            VistaConfirmacionCompra = false;
            // PackSeleccionado se mantiene por si quiere seleccionar otro pack
        }

        // ============================================
        // COMANDOS - BUSQUEDA DE CLIENTES
        // ============================================

        partial void OnBusquedaClienteChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
            {
                MostrarSugerenciasCliente = false;
                ClientesFiltrados.Clear();
                return;
            }

            _ = BuscarClientesSugerenciasAsync(value);
        }

        private async Task BuscarClientesSugerenciasAsync(string termino)
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var whereClause = TipoBusquedaSeleccionado switch
                {
                    "Nombre" => "(nombre ILIKE @termino OR apellidos ILIKE @termino)",
                    "Documento" => "documento_numero ILIKE @termino",
                    "Telefono" => "telefono ILIKE @termino",
                    _ => "(nombre ILIKE @termino OR apellidos ILIKE @termino OR documento_numero ILIKE @termino OR telefono ILIKE @termino)"
                };

                var comercioFilter = _idComercio > 0 ? " AND id_comercio_registro = @idComercio" : "";

                var sql = $@"SELECT id_cliente, nombre, apellidos, telefono, 
                            COALESCE(documento_tipo, 'DNI') as documento_tipo, 
                            COALESCE(documento_numero, '') as documento_numero,
                            segundo_nombre, segundo_apellido
                        FROM clientes
                        WHERE activo = true AND {whereClause}{comercioFilter}
                        ORDER BY nombre, apellidos
                        LIMIT 8";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("termino", $"%{termino}%");
                if (_idComercio > 0)
                    cmd.Parameters.AddWithValue("idComercio", _idComercio);

                ClientesFiltrados.Clear();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var cliente = new ClienteModel
                    {
                        IdCliente = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        Apellido = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Telefono = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        TipoDocumento = reader.GetString(4),
                        NumeroDocumento = reader.GetString(5),
                        SegundoNombre = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        SegundoApellido = reader.IsDBNull(7) ? "" : reader.GetString(7)
                    };
                    ClientesFiltrados.Add(cliente);
                }

                MostrarSugerenciasCliente = ClientesFiltrados.Count > 0;
            }
            catch
            {
                MostrarSugerenciasCliente = false;
            }
        }

        [RelayCommand]
        private async Task BuscarClientesManualAsync()
        {
            if (string.IsNullOrWhiteSpace(BusquedaCliente))
            {
                ErrorMessage = "Ingrese un termino de busqueda";
                return;
            }

            ErrorMessage = string.Empty;
            MostrarSugerenciasCliente = false;
            EstaCargando = true;

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var termino = BusquedaCliente.Trim();
                var usarTermino = !string.IsNullOrWhiteSpace(termino);

                var whereClause = TipoBusquedaSeleccionado switch
                {
                    "Nombre" => usarTermino ? "(nombre ILIKE @termino OR apellidos ILIKE @termino)" : "1=1",
                    "Documento" => usarTermino ? "documento_numero ILIKE @termino" : "1=1",
                    "Telefono" => usarTermino ? "telefono ILIKE @termino" : "1=1",
                    _ => usarTermino ? "(nombre ILIKE @termino OR apellidos ILIKE @termino OR documento_numero ILIKE @termino OR telefono ILIKE @termino)" : "1=1"
                };

                var comercioFilter = _idComercio > 0 ? " AND id_comercio_registro = @idComercio" : "";

                var sql = $@"SELECT DISTINCT id_cliente, nombre, apellidos, telefono, direccion,
                            COALESCE(nacionalidad, '') as nacionalidad, 
                            COALESCE(documento_tipo, 'DNI') as documento_tipo, 
                            COALESCE(documento_numero, '') as documento_numero,
                            segundo_nombre, segundo_apellido
                        FROM clientes
                        WHERE activo = true AND {whereClause}{comercioFilter}
                        ORDER BY nombre, apellidos
                        LIMIT 50";

                await using var cmd = new NpgsqlCommand(sql, conn);
                if (usarTermino)
                    cmd.Parameters.AddWithValue("termino", $"%{termino}%");
                if (_idComercio > 0)
                    cmd.Parameters.AddWithValue("idComercio", _idComercio);

                ClientesEncontrados.Clear();
                var idsAgregados = new HashSet<int>();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var idCliente = reader.GetInt32(0);
                    if (idsAgregados.Contains(idCliente)) continue;
                    idsAgregados.Add(idCliente);

                    var cliente = new ClienteModel
                    {
                        IdCliente = idCliente,
                        Nombre = reader.GetString(1),
                        Apellido = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Telefono = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Direccion = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        Nacionalidad = reader.GetString(5),
                        TipoDocumento = reader.GetString(6),
                        NumeroDocumento = reader.GetString(7),
                        SegundoNombre = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        SegundoApellido = reader.IsDBNull(9) ? "" : reader.GetString(9)
                    };

                    ClientesEncontrados.Add(cliente);
                }

                if (!ClientesEncontrados.Any())
                    ErrorMessage = usarTermino ? "No se encontraron clientes" : "No hay clientes registrados";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error en busqueda: {ex.Message}";
            }
            finally
            {
                EstaCargando = false;
            }
        }

        [RelayCommand]
        private void SeleccionarClienteSugerido(ClienteModel? cliente)
        {
            if (cliente == null) return;

            ClienteSeleccionado = cliente;
            BusquedaCliente = cliente.NombreCompleto;
            MostrarSugerenciasCliente = false;
            ClientesFiltrados.Clear();
            ValidarOperacion();

            OnPropertyChanged(nameof(ClienteSeleccionadoNombre));
            OnPropertyChanged(nameof(ClienteSeleccionadoDocumento));
            OnPropertyChanged(nameof(TieneClienteSeleccionado));
        }

        [RelayCommand]
        private void SeleccionarClienteYContinuar(ClienteModel? cliente)
        {
            if (cliente == null) return;

            ClienteSeleccionado = cliente;
            ValidarOperacion();

            OnPropertyChanged(nameof(ClienteSeleccionadoNombre));
            OnPropertyChanged(nameof(ClienteSeleccionadoDocumento));
            OnPropertyChanged(nameof(TieneClienteSeleccionado));

            OcultarTodasLasVistas();
            VistaPrincipal = true;
            MostrarMensaje($"Cliente seleccionado: {cliente.NombreCompleto}", false);
        }

        [RelayCommand]
        private void LimpiarClienteSeleccionado()
        {
            ClienteSeleccionado = null;
            BusquedaCliente = string.Empty;
            ValidarOperacion();

            OnPropertyChanged(nameof(ClienteSeleccionadoNombre));
            OnPropertyChanged(nameof(ClienteSeleccionadoDocumento));
            OnPropertyChanged(nameof(TieneClienteSeleccionado));
        }

        // ============================================
        // COMANDOS - NUEVO CLIENTE
        // ============================================

        private void LimpiarFormularioCliente()
        {
            NuevoNombre = string.Empty;
            NuevoSegundoNombre = string.Empty;
            NuevoApellido = string.Empty;
            NuevoSegundoApellido = string.Empty;
            NuevoTelefono = string.Empty;
            NuevaDireccion = string.Empty;
            NuevaNacionalidad = string.Empty;
            NuevoTipoDocumento = "DNI";
            NuevoNumeroDocumento = string.Empty;
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private async Task GuardarNuevoClienteAsync()
        {
            EstaCargando = true;
            ErrorMessage = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(NuevoNombre) || string.IsNullOrWhiteSpace(NuevoApellido))
                {
                    ErrorMessage = "Nombre y Apellido son obligatorios";
                    return;
                }

                if (string.IsNullOrWhiteSpace(NuevoTelefono))
                {
                    ErrorMessage = "Telefono es obligatorio";
                    return;
                }

                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var sql = @"INSERT INTO clientes 
                    (nombre, segundo_nombre, apellidos, segundo_apellido, telefono, 
                     direccion, nacionalidad, documento_tipo, documento_numero,
                     id_comercio_registro, id_local_registro, id_usuario_registro, activo)
                    VALUES 
                    (@nombre, @segundoNombre, @apellidos, @segundoApellido, @telefono,
                     @direccion, @nacionalidad, @tipoDoc, @numDoc,
                     @idComercio, @idLocal, @idUsuario, true)
                    RETURNING id_cliente";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("nombre", NuevoNombre.Trim());
                cmd.Parameters.AddWithValue("segundoNombre", NuevoSegundoNombre?.Trim() ?? "");
                cmd.Parameters.AddWithValue("apellidos", NuevoApellido.Trim());
                cmd.Parameters.AddWithValue("segundoApellido", NuevoSegundoApellido?.Trim() ?? "");
                cmd.Parameters.AddWithValue("telefono", NuevoTelefono.Trim());
                cmd.Parameters.AddWithValue("direccion", NuevaDireccion?.Trim() ?? "");
                cmd.Parameters.AddWithValue("nacionalidad", NuevaNacionalidad?.Trim() ?? "");
                cmd.Parameters.AddWithValue("tipoDoc", NuevoTipoDocumento);
                cmd.Parameters.AddWithValue("numDoc", NuevoNumeroDocumento?.Trim() ?? "");
                cmd.Parameters.AddWithValue("idComercio", _idComercio > 0 ? _idComercio : DBNull.Value);
                cmd.Parameters.AddWithValue("idLocal", _idLocal > 0 ? _idLocal : DBNull.Value);
                cmd.Parameters.AddWithValue("idUsuario", _idUsuario > 0 ? _idUsuario : DBNull.Value);

                var idNuevoCliente = (int)(await cmd.ExecuteScalarAsync() ?? 0);

                if (idNuevoCliente > 0)
                {
                    var nuevoCliente = new ClienteModel
                    {
                        IdCliente = idNuevoCliente,
                        Nombre = NuevoNombre.Trim(),
                        SegundoNombre = NuevoSegundoNombre?.Trim() ?? "",
                        Apellido = NuevoApellido.Trim(),
                        SegundoApellido = NuevoSegundoApellido?.Trim() ?? "",
                        Telefono = NuevoTelefono.Trim(),
                        Direccion = NuevaDireccion?.Trim() ?? "",
                        Nacionalidad = NuevaNacionalidad?.Trim() ?? "",
                        TipoDocumento = NuevoTipoDocumento,
                        NumeroDocumento = NuevoNumeroDocumento?.Trim() ?? ""
                    };

                    ClienteSeleccionado = nuevoCliente;
                    ValidarOperacion();

                    OnPropertyChanged(nameof(ClienteSeleccionadoNombre));
                    OnPropertyChanged(nameof(ClienteSeleccionadoDocumento));
                    OnPropertyChanged(nameof(TieneClienteSeleccionado));

                    MostrarMensaje("Cliente registrado correctamente", false);
                    LimpiarFormularioCliente();
                    OcultarTodasLasVistas();
                    VistaPrincipal = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al guardar: {ex.Message}";
            }
            finally
            {
                EstaCargando = false;
            }
        }

        // ============================================
        // METODOS PRIVADOS
        // ============================================

        private void ValidarOperacion()
        {
            PuedeRealizarCompra = ClienteSeleccionado != null;
        }

        private async Task CargarPacksDisponiblesAsync()
        {
            EstaCargando = true;
            PacksDisponibles.Clear();

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

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

                var packs = new List<PackAlimentoFront>();

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