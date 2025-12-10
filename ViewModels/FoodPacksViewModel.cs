using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models;
using Allva.Desktop.Services;
using Npgsql;

namespace Allva.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel para el modulo de Pack de Alimentos en el Front-Office
    /// Flujo: Buscar Cliente -> Seleccionar Pack -> Datos Beneficiario -> Confirmacion -> Guardar
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
        // PROPIEDADES OBSERVABLES - BENEFICIARIO
        // ============================================

        [ObservableProperty]
        private string _beneficiarioNombreCompleto = string.Empty;

        [ObservableProperty]
        private string _beneficiarioTipoDocumento = "DNI";

        [ObservableProperty]
        private string _beneficiarioNumeroDocumento = string.Empty;

        [ObservableProperty]
        private string _beneficiarioDireccion = string.Empty;

        [ObservableProperty]
        private string _beneficiarioPaisDestino = string.Empty;

        [ObservableProperty]
        private string _beneficiarioCiudadDestino = string.Empty;

        [ObservableProperty]
        private string _beneficiarioTelefono = string.Empty;

        public ObservableCollection<string> TiposDocumentoBeneficiario { get; } = new() 
        { 
            "DNI", "NIE", "Pasaporte", "Cedula", "Otro" 
        };

        public ObservableCollection<string> PaisesDestino { get; } = new()
        {
            "Argentina", "Bolivia", "Brasil", "Chile", "Colombia", 
            "Ecuador", "Paraguay", "Peru", "Rep. Dominicana", 
            "Uruguay", "Venezuela", "Mexico", "Cuba", "Otro"
        };

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
        private bool _vistaSeleccionPack = false;

        [ObservableProperty]
        private bool _vistaDatosBeneficiario = false;

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

        [ObservableProperty]
        private string _numeroOperacion = string.Empty;

        [ObservableProperty]
        private decimal _totalCompra = 0;

        [ObservableProperty]
        private string _divisaCompra = "EUR";

        private int _idComercio = 0;
        private int _idLocal = 0;
        private int _idUsuario = 0;
        private string _codigoLocal = string.Empty;
        private string _nombreUsuario = string.Empty;
        private string _numeroUsuario = string.Empty;

        // ============================================
        // PROPIEDADES CALCULADAS
        // ============================================

        public IBrush MensajeBackground => MensajeEsError
            ? new SolidColorBrush(Color.Parse("#dc3545"))
            : new SolidColorBrush(Color.Parse("#28a745"));

        public bool HayPacks => PacksDisponibles.Count > 0;
        public bool NoHayPacks => PacksDisponibles.Count == 0 && !EstaCargando;

        public bool TieneClienteSeleccionado => ClienteSeleccionado != null;
        public string ClienteSeleccionadoNombre => ClienteSeleccionado?.NombreCompleto ?? "Ninguno";
        public string ClienteSeleccionadoDocumento => ClienteSeleccionado != null 
            ? $"{ClienteSeleccionado.TipoDocumento}: {ClienteSeleccionado.NumeroDocumento}" 
            : "";

        public string TotalCompraFormateado => $"{TotalCompra:N2} {DivisaCompra}";

        public string ResumenPack => PackSeleccionado != null
            ? $"{PackSeleccionado.NombrePack} - {PackSeleccionado.PrecioFormateado}"
            : "";

        public string ResumenBeneficiario => !string.IsNullOrEmpty(BeneficiarioNombreCompleto)
            ? $"{BeneficiarioNombreCompleto} ({BeneficiarioPaisDestino})"
            : "";

        // ============================================
        // CONSTRUCTORES
        // ============================================

        public FoodPacksViewModel()
        {
            _idComercio = 1;
            _idLocal = 1;
            _idUsuario = 1;
            _ = CargarPacksDisponiblesAsync();
        }

        public FoodPacksViewModel(int idComercio, int idLocal)
        {
            _idComercio = idComercio;
            _idLocal = idLocal;
            _idUsuario = 0;
            _ = CargarPacksDisponiblesAsync();
        }

        public FoodPacksViewModel(int idComercio, int idLocal, int idUsuario, 
                                   string nombreUsuario, string numeroUsuario, string codigoLocal)
        {
            _idComercio = idComercio;
            _idLocal = idLocal;
            _idUsuario = idUsuario;
            _nombreUsuario = nombreUsuario;
            _numeroUsuario = numeroUsuario;
            _codigoLocal = codigoLocal;
            _ = CargarPacksDisponiblesAsync();
        }

        public void SetSesionData(int idLocal, int idComercio, int idUsuario, 
                                   string nombreUsuario, string numeroUsuario, string codigoLocal)
        {
            _idLocal = idLocal;
            _idComercio = idComercio;
            _idUsuario = idUsuario;
            _nombreUsuario = nombreUsuario;
            _numeroUsuario = numeroUsuario;
            _codigoLocal = codigoLocal;
        }

        // ============================================
        // METODOS - CARGA DE PACKS
        // ============================================

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

                while (await reader.ReadAsync())
                {
                    var pack = new PackAlimentoFront
                    {
                        IdPack = reader.GetInt32(0),
                        NombrePack = reader.GetString(1),
                        Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2),
                        ImagenPoster = reader.IsDBNull(3) ? null : (byte[])reader[3],
                        ImagenPosterNombre = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Precio = reader.GetDecimal(5),
                        Divisa = reader.GetString(6)
                    };

                    PacksDisponibles.Add(pack);
                }

                OnPropertyChanged(nameof(HayPacks));
                OnPropertyChanged(nameof(NoHayPacks));

                if (!PacksDisponibles.Any())
                    MostrarMensaje("No hay packs disponibles para este comercio", true);
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

        private async Task CargarProductosPackAsync(PackAlimentoFront pack)
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var query = @"
                    SELECT id_producto, nombre_producto, descripcion, detalles,
                           cantidad, unidad_medida, imagen, imagen_nombre
                    FROM pack_alimentos_productos
                    WHERE id_pack = @idPack
                    ORDER BY orden, nombre_producto";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idPack", pack.IdPack);

                await using var reader = await cmd.ExecuteReaderAsync();
                pack.Productos.Clear();

                while (await reader.ReadAsync())
                {
                    pack.Productos.Add(new PackAlimentoProductoFront
                    {
                        IdProducto = reader.GetInt32(0),
                        NombreProducto = reader.GetString(1),
                        Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Detalles = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Cantidad = reader.GetInt32(4),
                        UnidadMedida = reader.GetString(5),
                        Imagen = reader.IsDBNull(6) ? null : (byte[])reader[6],
                        ImagenNombre = reader.IsDBNull(7) ? null : reader.GetString(7)
                    });
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar productos: {ex.Message}", true);
            }
        }

        // ============================================
        // COMANDOS - NAVEGACION
        // ============================================

        private void OcultarTodasLasVistas()
        {
            VistaPrincipal = false;
            VistaBuscarCliente = false;
            VistaNuevoCliente = false;
            VistaSeleccionPack = false;
            VistaDatosBeneficiario = false;
            VistaConfirmacionCompra = false;
        }

        [RelayCommand]
        private void IrABuscarCliente()
        {
            OcultarTodasLasVistas();
            VistaBuscarCliente = true;
            BuscarClientesAsync().ConfigureAwait(false);
        }

        [RelayCommand]
        private void IrANuevoCliente()
        {
            OcultarTodasLasVistas();
            VistaNuevoCliente = true;
            LimpiarFormularioCliente();
        }

        [RelayCommand]
        private void IrASeleccionPack()
        {
            if (ClienteSeleccionado == null)
            {
                MostrarMensaje("Debe seleccionar un cliente primero", true);
                return;
            }

            OcultarTodasLasVistas();
            VistaSeleccionPack = true;
        }

        [RelayCommand]
        private void IrADatosBeneficiario()
        {
            if (PackSeleccionado == null)
            {
                MostrarMensaje("Debe seleccionar un pack", true);
                return;
            }

            TotalCompra = PackSeleccionado.Precio;
            DivisaCompra = PackSeleccionado.Divisa;
            OnPropertyChanged(nameof(TotalCompraFormateado));

            OcultarTodasLasVistas();
            VistaDatosBeneficiario = true;
        }

        [RelayCommand]
        private void IrAConfirmacion()
        {
            if (!ValidarDatosBeneficiario())
                return;

            OnPropertyChanged(nameof(ResumenPack));
            OnPropertyChanged(nameof(ResumenBeneficiario));
            OnPropertyChanged(nameof(TotalCompraFormateado));

            OcultarTodasLasVistas();
            VistaConfirmacionCompra = true;
        }

        [RelayCommand]
        private void VolverAVistaPrincipal()
        {
            OcultarTodasLasVistas();
            VistaPrincipal = true;
        }

        // Alias para vistas separadas
        [RelayCommand]
        private void VolverAPrincipal()
        {
            OcultarTodasLasVistas();
            VistaPrincipal = true;
        }

        [RelayCommand]
        private void VolverABuscarCliente()
        {
            OcultarTodasLasVistas();
            VistaBuscarCliente = true;
        }

        [RelayCommand]
        private async Task BuscarClientesManualAsync()
        {
            await BuscarClientesAsync();
        }

        [RelayCommand]
        private void CancelarCompra()
        {
            VistaConfirmacionCompra = false;
            OcultarTodasLasVistas();
            VistaPrincipal = true;
        }

        [RelayCommand]
        private async Task ConfirmarCompraAsync()
        {
            await FinalizarCompraAsync();
        }

        [RelayCommand]
        private void VolverASeleccionPack()
        {
            OcultarTodasLasVistas();
            VistaSeleccionPack = true;
        }

        [RelayCommand]
        private void VolverADatosBeneficiario()
        {
            OcultarTodasLasVistas();
            VistaDatosBeneficiario = true;
        }

        // ============================================
        // COMANDOS - PACKS
        // ============================================

        [RelayCommand]
        private async Task VerDetallePack(PackAlimentoFront? pack)
        {
            if (pack == null) return;
            PackSeleccionado = pack;
            await CargarProductosPackAsync(pack);
            MostrarDetallePack = true;
        }

        [RelayCommand]
        private void CerrarDetallePack()
        {
            MostrarDetallePack = false;
        }

        [RelayCommand]
        private void SeleccionarPack(PackAlimentoFront? pack)
        {
            if (pack == null) return;

            if (ClienteSeleccionado == null)
            {
                MostrarMensaje("Debe seleccionar un cliente antes", true);
                return;
            }

            PackSeleccionado = pack;
            CerrarDetallePack();
            IrADatosBeneficiario();
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

            FiltrarClientesParaSugerencias(value);
        }

        private void FiltrarClientesParaSugerencias(string termino)
        {
            ClientesFiltrados.Clear();
            var terminoLower = termino.ToLower();

            var filtrados = ClientesEncontrados
                .Where(c => c.NombreCompleto.ToLower().Contains(terminoLower) ||
                           c.NumeroDocumento.ToLower().Contains(terminoLower) ||
                           c.Telefono.Contains(termino))
                .Take(8)
                .ToList();

            foreach (var cliente in filtrados)
                ClientesFiltrados.Add(cliente);

            MostrarSugerenciasCliente = ClientesFiltrados.Any();
        }

        [RelayCommand]
        private async Task BuscarClientesAsync()
        {
            EstaCargando = true;
            ClientesEncontrados.Clear();
            ErrorMessage = string.Empty;

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var termino = BusquedaCliente?.Trim() ?? "";
                var usarTermino = !string.IsNullOrEmpty(termino);

                // Filtrar por comercio del usuario actual y clientes activos
                var whereClause = "WHERE activo = true AND id_comercio_registro = @id_comercio";
                
                if (usarTermino)
                {
                    switch (TipoBusquedaSeleccionado)
                    {
                        case "Nombre":
                            whereClause += " AND (LOWER(nombre || ' ' || apellidos) LIKE LOWER(@termino))";
                            break;
                        case "Documento":
                            whereClause += " AND (LOWER(documento_numero) LIKE LOWER(@termino))";
                            break;
                        case "Telefono":
                            whereClause += " AND (telefono LIKE @termino)";
                            break;
                        default:
                            whereClause += @" AND (LOWER(nombre || ' ' || apellidos) LIKE LOWER(@termino) 
                                             OR LOWER(documento_numero) LIKE LOWER(@termino) 
                                             OR telefono LIKE @termino)";
                            break;
                    }
                }

                var query = $@"
                    SELECT id_cliente, nombre, apellidos, telefono, direccion, nacionalidad,
                           documento_tipo, documento_numero, segundo_nombre, segundo_apellido
                    FROM clientes
                    {whereClause}
                    ORDER BY nombre, apellidos
                    LIMIT 50";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id_comercio", _idComercio);
                
                if (usarTermino)
                    cmd.Parameters.AddWithValue("@termino", $"%{termino}%");

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var cliente = new ClienteModel
                    {
                        IdCliente = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        Apellido = reader.GetString(2),
                        Telefono = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Direccion = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        Nacionalidad = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        TipoDocumento = reader.IsDBNull(6) ? "DNI" : reader.GetString(6),
                        NumeroDocumento = reader.IsDBNull(7) ? "" : reader.GetString(7),
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

            OnPropertyChanged(nameof(ClienteSeleccionadoNombre));
            OnPropertyChanged(nameof(ClienteSeleccionadoDocumento));
            OnPropertyChanged(nameof(TieneClienteSeleccionado));
        }

        [RelayCommand]
        private void SeleccionarClienteYContinuar(ClienteModel? cliente)
        {
            if (cliente == null) return;

            ClienteSeleccionado = cliente;

            OnPropertyChanged(nameof(ClienteSeleccionadoNombre));
            OnPropertyChanged(nameof(ClienteSeleccionadoDocumento));
            OnPropertyChanged(nameof(TieneClienteSeleccionado));

            MostrarMensaje($"Cliente seleccionado: {cliente.NombreCompleto}", false);
            IrASeleccionPack();
        }

        [RelayCommand]
        private void LimpiarClienteSeleccionado()
        {
            ClienteSeleccionado = null;
            BusquedaCliente = string.Empty;

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
            if (string.IsNullOrWhiteSpace(NuevoNombre) || string.IsNullOrWhiteSpace(NuevoApellido))
            {
                ErrorMessage = "Nombre y Apellido son obligatorios";
                return;
            }

            EstaCargando = true;
            ErrorMessage = string.Empty;

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var query = @"
                    INSERT INTO clientes (nombre, segundo_nombre, apellidos, segundo_apellido,
                                         telefono, direccion, nacionalidad, documento_tipo, 
                                         documento_numero, id_comercio_registro, activo)
                    VALUES (@nombre, @segundo_nombre, @apellidos, @segundo_apellido,
                            @telefono, @direccion, @nacionalidad, @documento_tipo,
                            @documento_numero, @id_comercio, true)
                    RETURNING id_cliente";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@nombre", NuevoNombre.Trim());
                cmd.Parameters.AddWithValue("@segundo_nombre", string.IsNullOrWhiteSpace(NuevoSegundoNombre) ? DBNull.Value : NuevoSegundoNombre.Trim());
                cmd.Parameters.AddWithValue("@apellidos", NuevoApellido.Trim());
                cmd.Parameters.AddWithValue("@segundo_apellido", string.IsNullOrWhiteSpace(NuevoSegundoApellido) ? DBNull.Value : NuevoSegundoApellido.Trim());
                cmd.Parameters.AddWithValue("@telefono", NuevoTelefono?.Trim() ?? "");
                cmd.Parameters.AddWithValue("@direccion", NuevaDireccion?.Trim() ?? "");
                cmd.Parameters.AddWithValue("@nacionalidad", NuevaNacionalidad?.Trim() ?? "");
                cmd.Parameters.AddWithValue("@documento_tipo", NuevoTipoDocumento);
                cmd.Parameters.AddWithValue("@documento_numero", NuevoNumeroDocumento?.Trim() ?? "");
                cmd.Parameters.AddWithValue("@id_comercio", _idComercio > 0 ? _idComercio : DBNull.Value);

                var idCliente = (int)(await cmd.ExecuteScalarAsync() ?? 0);

                if (idCliente > 0)
                {
                    var nuevoCliente = new ClienteModel
                    {
                        IdCliente = idCliente,
                        Nombre = NuevoNombre.Trim(),
                        SegundoNombre = NuevoSegundoNombre?.Trim() ?? "",
                        Apellido = NuevoApellido.Trim(),
                        SegundoApellido = NuevoSegundoApellido?.Trim() ?? "",
                        Telefono = NuevoTelefono?.Trim() ?? "",
                        Direccion = NuevaDireccion?.Trim() ?? "",
                        Nacionalidad = NuevaNacionalidad?.Trim() ?? "",
                        TipoDocumento = NuevoTipoDocumento,
                        NumeroDocumento = NuevoNumeroDocumento?.Trim() ?? ""
                    };

                    ClienteSeleccionado = nuevoCliente;

                    OnPropertyChanged(nameof(ClienteSeleccionadoNombre));
                    OnPropertyChanged(nameof(ClienteSeleccionadoDocumento));
                    OnPropertyChanged(nameof(TieneClienteSeleccionado));

                    MostrarMensaje("Cliente registrado correctamente", false);
                    LimpiarFormularioCliente();
                    IrASeleccionPack();
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
        // VALIDACION - BENEFICIARIO
        // ============================================

        private bool ValidarDatosBeneficiario()
        {
            if (string.IsNullOrWhiteSpace(BeneficiarioNombreCompleto))
            {
                MostrarMensaje("El nombre del beneficiario es obligatorio", true);
                return false;
            }

            if (string.IsNullOrWhiteSpace(BeneficiarioNumeroDocumento))
            {
                MostrarMensaje("El documento del beneficiario es obligatorio", true);
                return false;
            }

            if (string.IsNullOrWhiteSpace(BeneficiarioDireccion))
            {
                MostrarMensaje("La direccion del beneficiario es obligatoria", true);
                return false;
            }

            if (string.IsNullOrWhiteSpace(BeneficiarioPaisDestino))
            {
                MostrarMensaje("El pais de destino es obligatorio", true);
                return false;
            }

            return true;
        }

        private void LimpiarDatosBeneficiario()
        {
            BeneficiarioNombreCompleto = string.Empty;
            BeneficiarioTipoDocumento = "DNI";
            BeneficiarioNumeroDocumento = string.Empty;
            BeneficiarioDireccion = string.Empty;
            BeneficiarioPaisDestino = string.Empty;
            BeneficiarioCiudadDestino = string.Empty;
            BeneficiarioTelefono = string.Empty;
        }

        // ============================================
        // COMANDOS - FINALIZAR COMPRA
        // ============================================

        [RelayCommand]
        private async Task FinalizarCompraAsync()
        {
            if (ClienteSeleccionado == null || PackSeleccionado == null)
            {
                MostrarMensaje("Datos incompletos para la compra", true);
                return;
            }

            if (!ValidarDatosBeneficiario())
                return;

            // Validar datos de sesion
            if (_idUsuario <= 0 || _idComercio <= 0 || _idLocal <= 0)
            {
                MostrarMensaje("Error de sesion. Por favor, cierre sesion y vuelva a ingresar.", true);
                return;
            }

            EstaCargando = true;

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                using var transaction = await conn.BeginTransactionAsync();

                try
                {
                    // 1. Generar numero de operacion
                    NumeroOperacion = await GenerarNumeroOperacionAsync(conn, transaction);

                    // 2. Insertar operacion principal
                    var queryOperacion = @"
                        INSERT INTO operaciones (
                            numero_operacion, id_comercio, id_local, codigo_local,
                            id_usuario, nombre_usuario, numero_usuario,
                            id_cliente, nombre_cliente, modulo, tipo_operacion,
                            estado, importe_total, importe_pagado, moneda,
                            metodo_pago, fecha_operacion, hora_operacion
                        )
                        VALUES (
                            @numero_operacion, @id_comercio, @id_local, @codigo_local,
                            @id_usuario, @nombre_usuario, @numero_usuario,
                            @id_cliente, @nombre_cliente, 'PACK_ALIMENTOS', 'VENTA',
                            'COMPLETADA', @importe_total, @importe_total, @moneda,
                            'EFECTIVO', CURRENT_TIMESTAMP, CURRENT_TIME
                        )
                        RETURNING id_operacion";

                    await using var cmdOp = new NpgsqlCommand(queryOperacion, conn, transaction);
                    cmdOp.Parameters.AddWithValue("@numero_operacion", NumeroOperacion);
                    cmdOp.Parameters.AddWithValue("@id_comercio", _idComercio);
                    cmdOp.Parameters.AddWithValue("@id_local", _idLocal);
                    cmdOp.Parameters.AddWithValue("@codigo_local", _codigoLocal);
                    cmdOp.Parameters.AddWithValue("@id_usuario", _idUsuario);
                    cmdOp.Parameters.AddWithValue("@nombre_usuario", _nombreUsuario);
                    cmdOp.Parameters.AddWithValue("@numero_usuario", _numeroUsuario);
                    cmdOp.Parameters.AddWithValue("@id_cliente", ClienteSeleccionado.IdCliente);
                    cmdOp.Parameters.AddWithValue("@nombre_cliente", ClienteSeleccionado.NombreCompleto);
                    cmdOp.Parameters.AddWithValue("@importe_total", TotalCompra);
                    cmdOp.Parameters.AddWithValue("@moneda", DivisaCompra);

                    var idOperacion = (long)(await cmdOp.ExecuteScalarAsync() ?? 0);

                    // 3. Insertar detalle de pack alimentos
                    var queryDetalle = @"
                        INSERT INTO operaciones_pack_alimentos (
                            id_operacion, nombre_pack, descripcion_pack,
                            pais_destino, ciudad_destino, precio_pack,
                            estado_envio, observaciones
                        )
                        VALUES (
                            @id_operacion, @nombre_pack, @descripcion_pack,
                            @pais_destino, @ciudad_destino, @precio_pack,
                            'PENDIENTE', @observaciones
                        )";

                    var observaciones = $"Beneficiario: {BeneficiarioNombreCompleto} | " +
                                       $"Doc: {BeneficiarioTipoDocumento} {BeneficiarioNumeroDocumento} | " +
                                       $"Dir: {BeneficiarioDireccion} | " +
                                       $"Tel: {BeneficiarioTelefono}";

                    await using var cmdDetalle = new NpgsqlCommand(queryDetalle, conn, transaction);
                    cmdDetalle.Parameters.AddWithValue("@id_operacion", idOperacion);
                    cmdDetalle.Parameters.AddWithValue("@nombre_pack", PackSeleccionado.NombrePack);
                    cmdDetalle.Parameters.AddWithValue("@descripcion_pack", PackSeleccionado.Descripcion ?? "");
                    cmdDetalle.Parameters.AddWithValue("@pais_destino", BeneficiarioPaisDestino);
                    cmdDetalle.Parameters.AddWithValue("@ciudad_destino", BeneficiarioCiudadDestino ?? "");
                    cmdDetalle.Parameters.AddWithValue("@precio_pack", TotalCompra);
                    cmdDetalle.Parameters.AddWithValue("@observaciones", observaciones);

                    await cmdDetalle.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();

                    MostrarMensaje($"Compra finalizada correctamente. Operacion: {NumeroOperacion}", false);
                    
                    // Esperar un momento para mostrar el mensaje y volver al panel principal
                    await Task.Delay(2000);
                    
                    // Limpiar datos y volver al panel principal
                    NuevaOperacion();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al finalizar compra: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private async Task<string> GenerarNumeroOperacionAsync(NpgsqlConnection conn, NpgsqlTransaction transaction)
        {
            var query = @"SELECT generar_numero_operacion('PACK_ALIMENTOS', @id_local)";
            await using var cmd = new NpgsqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@id_local", _idLocal);
            var resultado = await cmd.ExecuteScalarAsync();
            return resultado?.ToString() ?? $"PA{DateTime.Now:yyyyMMddHHmmss}";
        }

        // ============================================
        // COMANDOS - IMPRIMIR RECIBO
        // ============================================

        [RelayCommand]
        private async Task ImprimirReciboAsync(Avalonia.Controls.Window? window)
        {
            if (ClienteSeleccionado == null || PackSeleccionado == null)
            {
                MostrarMensaje("Datos incompletos para generar recibo", true);
                return;
            }

            if (!ValidarDatosBeneficiario())
                return;

            if (window == null)
            {
                MostrarMensaje("No se puede abrir el dialogo de guardar", true);
                return;
            }

            try
            {
                var nombreSugerido = $"Recibo_FoodPack_{(string.IsNullOrEmpty(NumeroOperacion) ? DateTime.Now.ToString("yyyyMMdd_HHmmss") : NumeroOperacion)}.pdf";
                
                var archivo = await window.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "Guardar Recibo PDF",
                    SuggestedFileName = nombreSugerido,
                    DefaultExtension = "pdf",
                    FileTypeChoices = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Documento PDF") { Patterns = new[] { "*.pdf" } }
                    }
                });

                if (archivo == null)
                    return;

                EstaCargando = true;

                // Preparar datos para el recibo
                var datosRecibo = new ReciboFoodPackService.DatosReciboFoodPack
                {
                    // Operacion
                    NumeroOperacion = string.IsNullOrEmpty(NumeroOperacion) ? "PENDIENTE" : NumeroOperacion,
                    FechaOperacion = DateTime.Now,
                    CodigoLocal = _codigoLocal,
                    NombreUsuario = _nombreUsuario,
                    NumeroUsuario = _numeroUsuario,

                    // Cliente
                    ClienteNombre = ClienteSeleccionado.NombreCompleto,
                    ClienteTipoDocumento = ClienteSeleccionado.TipoDocumento,
                    ClienteNumeroDocumento = ClienteSeleccionado.NumeroDocumento,
                    ClienteTelefono = ClienteSeleccionado.Telefono ?? "N/A",
                    ClienteDireccion = ClienteSeleccionado.Direccion ?? "",
                    ClienteNacionalidad = ClienteSeleccionado.Nacionalidad ?? "N/A",

                    // Beneficiario
                    BeneficiarioNombre = BeneficiarioNombreCompleto,
                    BeneficiarioTipoDocumento = BeneficiarioTipoDocumento,
                    BeneficiarioNumeroDocumento = BeneficiarioNumeroDocumento,
                    BeneficiarioDireccion = BeneficiarioDireccion,
                    BeneficiarioTelefono = BeneficiarioTelefono ?? "",
                    BeneficiarioPaisDestino = BeneficiarioPaisDestino,
                    BeneficiarioCiudadDestino = BeneficiarioCiudadDestino ?? "",

                    // Pack
                    PackNombre = PackSeleccionado.NombrePack,
                    PackDescripcion = PackSeleccionado.Descripcion ?? "",
                    PackProductos = PackSeleccionado.Productos?
                        .Select(p => $"{p.NombreProducto} ({p.CantidadConUnidad})")
                        .ToArray() ?? Array.Empty<string>(),

                    // Totales
                    PrecioPack = TotalCompra,
                    Total = TotalCompra,
                    Moneda = DivisaCompra,
                    MetodoPago = "EFECTIVO"
                };

                // Generar PDF
                var pdfService = new ReciboFoodPackService();
                var pdfBytes = pdfService.GenerarReciboPdf(datosRecibo);

                // Guardar archivo
                await using var stream = await archivo.OpenWriteAsync();
                await stream.WriteAsync(pdfBytes);

                MostrarMensaje("Recibo PDF generado correctamente", false);
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al generar recibo: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        // ============================================
        // COMANDOS - NUEVA OPERACION
        // ============================================

        [RelayCommand]
        private void NuevaOperacion()
        {
            // Limpiar todo para nueva operacion
            ClienteSeleccionado = null;
            PackSeleccionado = null;
            NumeroOperacion = string.Empty;
            TotalCompra = 0;
            
            LimpiarDatosBeneficiario();
            LimpiarClienteSeleccionado();
            
            OnPropertyChanged(nameof(TieneClienteSeleccionado));
            OnPropertyChanged(nameof(ClienteSeleccionadoNombre));
            OnPropertyChanged(nameof(ClienteSeleccionadoDocumento));
            
            OcultarTodasLasVistas();
            VistaPrincipal = true;
            
            MostrarMensaje("Listo para nueva operacion", false);
        }

        // ============================================
        // METODOS AUXILIARES
        // ============================================

        private void MostrarMensaje(string mensaje, bool esError)
        {
            MensajeEstado = mensaje;
            MensajeEsError = esError;
            HayMensaje = true;
            OnPropertyChanged(nameof(MensajeBackground));

            Task.Delay(4000).ContinueWith(_ =>
            {
                HayMensaje = false;
            });
        }
    }

    // ============================================
    // MODELOS AUXILIARES
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