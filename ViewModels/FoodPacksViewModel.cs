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
    /// Flujo: Seleccionar Pais -> Buscar/Crear Cliente -> Seleccionar/Crear Beneficiario -> Seleccionar Pack -> Confirmacion -> Guardar
    /// IMPORTANTE: Los datos se guardan con hora Espana (Europe/Madrid) y NO se comparten entre comercios
    /// </summary>
    public partial class FoodPacksViewModel : ObservableObject
    {
        private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

        // ============================================
        // PROPIEDADES OBSERVABLES - PAISES
        // ============================================

        [ObservableProperty]
        private ObservableCollection<PaisDesignadoFront> _paisesDisponibles = new();

        [ObservableProperty]
        private PaisDesignadoFront? _paisSeleccionado;

        [ObservableProperty]
        private bool _vistaSeleccionPais = true;

        public bool HayPaises => PaisesDisponibles.Count > 0;
        public bool NoHayPaises => PaisesDisponibles.Count == 0 && !EstaCargando;
        public string PaisSeleccionadoNombre => PaisSeleccionado?.NombrePais ?? "";

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

        [ObservableProperty]
        private bool _vistaEditarCliente = false;

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
        // PROPIEDADES OBSERVABLES - BENEFICIARIOS
        // ============================================

        [ObservableProperty]
        private ObservableCollection<BeneficiarioModel> _beneficiariosCliente = new();

        [ObservableProperty]
        private BeneficiarioModel? _beneficiarioSeleccionado;

        [ObservableProperty]
        private BeneficiarioModel? _beneficiarioEditando;

        [ObservableProperty]
        private bool _esEdicionBeneficiario = false;

        // Campos para nuevo/editar beneficiario
        [ObservableProperty]
        private string _beneficiarioNombre = string.Empty;

        [ObservableProperty]
        private string _beneficiarioSegundoNombre = string.Empty;

        [ObservableProperty]
        private string _beneficiarioApellido = string.Empty;

        [ObservableProperty]
        private string _beneficiarioSegundoApellido = string.Empty;

        [ObservableProperty]
        private string _beneficiarioTipoDocumento = "DNI";

        [ObservableProperty]
        private string _beneficiarioNumeroDocumento = string.Empty;

        [ObservableProperty]
        private string _beneficiarioTelefono = string.Empty;

        [ObservableProperty]
        private string _beneficiarioPais = string.Empty;

        [ObservableProperty]
        private string _beneficiarioCiudad = string.Empty;

        [ObservableProperty]
        private string _beneficiarioCalle = string.Empty;

        [ObservableProperty]
        private string _beneficiarioNumero = string.Empty;

        [ObservableProperty]
        private string _beneficiarioPiso = string.Empty;

        [ObservableProperty]
        private string _beneficiarioNumeroDepartamento = string.Empty;

        [ObservableProperty]
        private string _beneficiarioCodigoPostal = string.Empty;

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
        private bool _vistaPrincipal = false;

        [ObservableProperty]
        private bool _vistaBuscarCliente = false;

        [ObservableProperty]
        private bool _vistaNuevoCliente = false;

        [ObservableProperty]
        private bool _vistaSeleccionBeneficiario = false;

        [ObservableProperty]
        private bool _vistaNuevoBeneficiario = false;

        [ObservableProperty]
        private bool _vistaSeleccionPack = false;

        [ObservableProperty]
        private bool _vistaConfirmacionCompra = false;

        // ============================================
        // CAMPOS NUEVO/EDITAR CLIENTE
        // ============================================

        [ObservableProperty]
        private string _nuevoNombre = string.Empty;

        [ObservableProperty]
        private string _nuevoApellido = string.Empty;

        [ObservableProperty]
        private string _nuevoTelefono = string.Empty;

        [ObservableProperty]
        private string _nuevoTipoDocumento = "DNI";

        [ObservableProperty]
        private string _nuevoNumeroDocumento = string.Empty;

        [ObservableProperty]
        private bool _esEdicionCliente = false;

        [ObservableProperty]
        private string _nuevoSegundoNombre = string.Empty;

        [ObservableProperty]
        private string _nuevoSegundoApellido = string.Empty;

        [ObservableProperty]
        private string _nuevaNacionalidad = string.Empty;

        [ObservableProperty]
        private string _nuevaDireccion = string.Empty;

        public ObservableCollection<string> TiposDocumento { get; } = new() { "DNI", "NIE", "Pasaporte", "Cedula" };

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
        public string ClienteSeleccionadoTelefono => ClienteSeleccionado?.Telefono ?? "";

        public bool TieneBeneficiarioSeleccionado => BeneficiarioSeleccionado != null;
        public string BeneficiarioSeleccionadoNombre => BeneficiarioSeleccionado?.NombreCompleto ?? "Ninguno";
        public string BeneficiarioSeleccionadoDocumento => BeneficiarioSeleccionado != null 
            ? $"{BeneficiarioSeleccionado.TipoDocumento}: {BeneficiarioSeleccionado.NumeroDocumento}" 
            : "";
        public string BeneficiarioSeleccionadoDireccion => BeneficiarioSeleccionado?.DireccionCompleta ?? "";
        public string BeneficiarioSeleccionadoTelefono => BeneficiarioSeleccionado?.Telefono ?? "";
        public string BeneficiarioSeleccionadoPais => BeneficiarioSeleccionado?.Pais ?? "";
        public string BeneficiarioSeleccionadoCiudad => BeneficiarioSeleccionado?.Ciudad ?? "";

        public bool HayBeneficiarios => BeneficiariosCliente.Count > 0;
        public bool NoHayBeneficiarios => BeneficiariosCliente.Count == 0;

        public string TotalCompraFormateado => $"{TotalCompra:N2} {DivisaCompra}";

        public string ResumenPack => PackSeleccionado != null
            ? $"{PackSeleccionado.NombrePack} - {PackSeleccionado.PrecioFormateado}"
            : "";

        public string ResumenBeneficiario => BeneficiarioSeleccionado != null
            ? $"{BeneficiarioSeleccionado.NombreCompleto} ({BeneficiarioSeleccionado.Pais})"
            : "";

        public string TituloFormularioCliente => EsEdicionCliente ? "Editar Cliente" : "Nuevo Cliente";
        public string TituloFormularioBeneficiario => EsEdicionBeneficiario ? "Editar Beneficiario" : "Nuevo Beneficiario";

        // ============================================
        // CONSTRUCTORES
        // ============================================

        public FoodPacksViewModel()
        {
            _idComercio = 1;
            _idLocal = 1;
            _idUsuario = 1;
            _ = CargarPaisesDisponiblesAsync();
        }

        public FoodPacksViewModel(int idComercio, int idLocal)
        {
            _idComercio = idComercio;
            _idLocal = idLocal;
            _idUsuario = 0;
            _ = CargarPaisesDisponiblesAsync();
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
            _ = CargarPaisesDisponiblesAsync();
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
        // METODOS - CARGA DE PAISES
        // ============================================

        private async Task CargarPaisesDisponiblesAsync()
        {
            EstaCargando = true;
            PaisesDisponibles.Clear();

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                // Cargar solo paises que tienen packs asignados
                var query = @"
                    SELECT DISTINCT pd.id_pais, pd.nombre_pais, pd.codigo_iso, pd.bandera_imagen
                    FROM paises_designados pd
                    INNER JOIN packs_alimentos pa ON pa.id_pais = pd.id_pais AND pa.activo = true
                    LEFT JOIN pack_alimentos_asignacion_comercios paac 
                        ON pa.id_pack = paac.id_pack AND paac.id_comercio = @idComercio AND paac.activo = true
                    LEFT JOIN pack_alimentos_asignacion_global paag 
                        ON pa.id_pack = paag.id_pack AND paag.activo = true
                    WHERE pd.activo = true
                      AND (paac.id_asignacion IS NOT NULL OR paag.id_asignacion IS NOT NULL)
                    ORDER BY pd.nombre_pais";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idComercio", _idComercio > 0 ? _idComercio : 1);

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var pais = new PaisDesignadoFront
                    {
                        IdPais = reader.GetInt32(0),
                        NombrePais = reader.GetString(1),
                        CodigoIso = reader.IsDBNull(2) ? null : reader.GetString(2),
                        BanderaImagen = reader.IsDBNull(3) ? null : (byte[])reader[3]
                    };

                    PaisesDisponibles.Add(pais);
                }

                OnPropertyChanged(nameof(HayPaises));
                OnPropertyChanged(nameof(NoHayPaises));

                if (!PaisesDisponibles.Any())
                    MostrarMensaje("No hay paises con packs disponibles", true);
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar paises: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        [RelayCommand]
        private async Task SeleccionarPaisAsync(PaisDesignadoFront? pais)
        {
            if (pais == null) return;

            PaisSeleccionado = pais;
            OnPropertyChanged(nameof(PaisSeleccionadoNombre));

            await CargarPacksPorPaisAsync(pais.IdPais);

            OcultarTodasLasVistas();
            VistaPrincipal = true;
        }

        [RelayCommand]
        private void VolverASeleccionPais()
        {
            PaisSeleccionado = null;
            PacksDisponibles.Clear();
            OnPropertyChanged(nameof(PaisSeleccionadoNombre));

            OcultarTodasLasVistas();
            VistaSeleccionPais = true;
        }

        // ============================================
        // METODOS - CARGA DE PACKS
        // ============================================

        private async Task CargarPacksPorPaisAsync(int idPais)
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
                      AND pa.id_pais = @idPais
                      AND (paac.id_asignacion IS NOT NULL OR paag.id_asignacion IS NOT NULL)
                    ORDER BY pa.nombre_pack";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idComercio", _idComercio > 0 ? _idComercio : 1);
                cmd.Parameters.AddWithValue("@idPais", idPais);

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
                    MostrarMensaje("No hay packs disponibles para este pais", true);
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
        // METODOS - CARGA DE BENEFICIARIOS
        // ============================================

        private async Task CargarBeneficiariosClienteAsync()
        {
            if (ClienteSeleccionado == null) return;

            BeneficiariosCliente.Clear();

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                // Solo cargar beneficiarios del cliente Y del comercio actual
                var query = @"
                    SELECT id_beneficiario, id_cliente, id_comercio, id_local_registro,
                           nombre, segundo_nombre, apellido, segundo_apellido,
                           tipo_documento, numero_documento, telefono,
                           pais, ciudad, calle, numero, piso, numero_departamento, codigo_postal,
                           activo, fecha_creacion
                    FROM clientes_beneficiarios
                    WHERE id_cliente = @idCliente 
                      AND id_comercio = @idComercio
                      AND activo = true
                    ORDER BY nombre, apellido";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idCliente", ClienteSeleccionado.IdCliente);
                cmd.Parameters.AddWithValue("@idComercio", _idComercio);

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var beneficiario = new BeneficiarioModel
                    {
                        IdBeneficiario = reader.GetInt32(0),
                        IdCliente = reader.GetInt32(1),
                        IdComercio = reader.GetInt32(2),
                        IdLocalRegistro = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                        Nombre = reader.GetString(4),
                        SegundoNombre = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        Apellido = reader.GetString(6),
                        SegundoApellido = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        TipoDocumento = reader.GetString(8),
                        NumeroDocumento = reader.GetString(9),
                        Telefono = reader.GetString(10),
                        Pais = reader.GetString(11),
                        Ciudad = reader.GetString(12),
                        Calle = reader.GetString(13),
                        Numero = reader.GetString(14),
                        Piso = reader.IsDBNull(15) ? "" : reader.GetString(15),
                        NumeroDepartamento = reader.IsDBNull(16) ? "" : reader.GetString(16),
                        CodigoPostal = reader.IsDBNull(17) ? "" : reader.GetString(17),
                        Activo = reader.GetBoolean(18),
                        FechaCreacion = reader.GetDateTime(19)
                    };

                    BeneficiariosCliente.Add(beneficiario);
                }

                OnPropertyChanged(nameof(HayBeneficiarios));
                OnPropertyChanged(nameof(NoHayBeneficiarios));
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar beneficiarios: {ex.Message}", true);
            }
        }

        // ============================================
        // COMANDOS - NAVEGACION
        // ============================================

        private void OcultarTodasLasVistas()
        {
            VistaSeleccionPais = false;
            VistaPrincipal = false;
            VistaBuscarCliente = false;
            VistaNuevoCliente = false;
            VistaEditarCliente = false;
            VistaSeleccionBeneficiario = false;
            VistaNuevoBeneficiario = false;
            VistaSeleccionPack = false;
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
            EsEdicionCliente = false;
            LimpiarFormularioCliente();
            OcultarTodasLasVistas();
            VistaNuevoCliente = true;
            OnPropertyChanged(nameof(TituloFormularioCliente));
        }

        [RelayCommand]
        private void IrAEditarCliente()
        {
            if (ClienteSeleccionado == null) return;

            EsEdicionCliente = true;
            NuevoNombre = ClienteSeleccionado.Nombre;
            NuevoApellido = ClienteSeleccionado.Apellido;
            NuevoTelefono = ClienteSeleccionado.Telefono;
            NuevoTipoDocumento = ClienteSeleccionado.TipoDocumento;
            NuevoNumeroDocumento = ClienteSeleccionado.NumeroDocumento;

            OcultarTodasLasVistas();
            VistaEditarCliente = true;
            OnPropertyChanged(nameof(TituloFormularioCliente));
        }

        [RelayCommand]
        private async Task IrASeleccionBeneficiarioAsync()
        {
            if (ClienteSeleccionado == null)
            {
                MostrarMensaje("Debe seleccionar un cliente primero", true);
                return;
            }

            await CargarBeneficiariosClienteAsync();

            // Si no tiene beneficiarios, ir directo a crear uno
            if (!HayBeneficiarios)
            {
                IrANuevoBeneficiario();
                return;
            }

            OcultarTodasLasVistas();
            VistaSeleccionBeneficiario = true;
        }

        [RelayCommand]
        private void IrANuevoBeneficiario()
        {
            EsEdicionBeneficiario = false;
            BeneficiarioEditando = null;
            LimpiarFormularioBeneficiario();
            
            // Pre-llenar el pais con el pais seleccionado
            if (PaisSeleccionado != null)
            {
                BeneficiarioPais = PaisSeleccionado.NombrePais;
            }
            
            OcultarTodasLasVistas();
            VistaNuevoBeneficiario = true;
            OnPropertyChanged(nameof(TituloFormularioBeneficiario));
        }

        [RelayCommand]
        private void IrAEditarBeneficiario(BeneficiarioModel? beneficiario)
        {
            if (beneficiario == null) return;

            EsEdicionBeneficiario = true;
            BeneficiarioEditando = beneficiario;

            // Cargar datos en el formulario
            BeneficiarioNombre = beneficiario.Nombre;
            BeneficiarioSegundoNombre = beneficiario.SegundoNombre;
            BeneficiarioApellido = beneficiario.Apellido;
            BeneficiarioSegundoApellido = beneficiario.SegundoApellido;
            BeneficiarioTipoDocumento = beneficiario.TipoDocumento;
            BeneficiarioNumeroDocumento = beneficiario.NumeroDocumento;
            BeneficiarioTelefono = beneficiario.Telefono;
            BeneficiarioPais = beneficiario.Pais;
            BeneficiarioCiudad = beneficiario.Ciudad;
            BeneficiarioCalle = beneficiario.Calle;
            BeneficiarioNumero = beneficiario.Numero;
            BeneficiarioPiso = beneficiario.Piso;
            BeneficiarioNumeroDepartamento = beneficiario.NumeroDepartamento;
            BeneficiarioCodigoPostal = beneficiario.CodigoPostal;

            OcultarTodasLasVistas();
            VistaNuevoBeneficiario = true;
            OnPropertyChanged(nameof(TituloFormularioBeneficiario));
        }

        [RelayCommand]
        private void IrASeleccionPack()
        {
            if (ClienteSeleccionado == null)
            {
                MostrarMensaje("Debe seleccionar un cliente primero", true);
                return;
            }

            if (BeneficiarioSeleccionado == null)
            {
                MostrarMensaje("Debe seleccionar un beneficiario", true);
                return;
            }

            OcultarTodasLasVistas();
            VistaSeleccionPack = true;
        }

        [RelayCommand]
        private void IrAConfirmacion()
        {
            if (PackSeleccionado == null)
            {
                MostrarMensaje("Debe seleccionar un pack", true);
                return;
            }

            TotalCompra = PackSeleccionado.Precio;
            DivisaCompra = PackSeleccionado.Divisa;

            OnPropertyChanged(nameof(TotalCompraFormateado));
            OnPropertyChanged(nameof(ResumenPack));
            OnPropertyChanged(nameof(ResumenBeneficiario));

            OcultarTodasLasVistas();
            VistaConfirmacionCompra = true;
        }

        [RelayCommand]
        private void VolverAVistaPrincipal()
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
        private void VolverASeleccionBeneficiario()
        {
            OcultarTodasLasVistas();
            VistaSeleccionBeneficiario = true;
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
            VistaSeleccionBeneficiario = true;
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

            if (BeneficiarioSeleccionado == null)
            {
                MostrarMensaje("Debe seleccionar un beneficiario antes", true);
                return;
            }

            PackSeleccionado = pack;
            CerrarDetallePack();
            IrAConfirmacion();
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
                    SELECT id_cliente, nombre, apellidos, telefono, documento_tipo, documento_numero
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
                        TipoDocumento = reader.IsDBNull(4) ? "DNI" : reader.GetString(4),
                        NumeroDocumento = reader.IsDBNull(5) ? "" : reader.GetString(5)
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

            ActualizarPropiedadesCliente();
        }

        [RelayCommand]
        private async Task SeleccionarClienteYContinuarAsync(ClienteModel? cliente)
        {
            if (cliente == null) return;

            ClienteSeleccionado = cliente;
            ActualizarPropiedadesCliente();

            MostrarMensaje($"Cliente seleccionado: {cliente.NombreCompleto}", false);
            await IrASeleccionBeneficiarioAsync();
        }

        [RelayCommand]
        private void LimpiarClienteSeleccionado()
        {
            ClienteSeleccionado = null;
            BeneficiarioSeleccionado = null;
            BeneficiariosCliente.Clear();
            BusquedaCliente = string.Empty;

            ActualizarPropiedadesCliente();
            ActualizarPropiedadesBeneficiario();
        }

        private void ActualizarPropiedadesCliente()
        {
            OnPropertyChanged(nameof(ClienteSeleccionadoNombre));
            OnPropertyChanged(nameof(ClienteSeleccionadoDocumento));
            OnPropertyChanged(nameof(ClienteSeleccionadoTelefono));
            OnPropertyChanged(nameof(TieneClienteSeleccionado));
        }

        private void ActualizarPropiedadesBeneficiario()
        {
            OnPropertyChanged(nameof(BeneficiarioSeleccionadoNombre));
            OnPropertyChanged(nameof(BeneficiarioSeleccionadoDocumento));
            OnPropertyChanged(nameof(BeneficiarioSeleccionadoDireccion));
            OnPropertyChanged(nameof(BeneficiarioSeleccionadoTelefono));
            OnPropertyChanged(nameof(BeneficiarioSeleccionadoPais));
            OnPropertyChanged(nameof(BeneficiarioSeleccionadoCiudad));
            OnPropertyChanged(nameof(TieneBeneficiarioSeleccionado));
            OnPropertyChanged(nameof(HayBeneficiarios));
            OnPropertyChanged(nameof(NoHayBeneficiarios));
        }

        // ============================================
        // COMANDOS - CLIENTE (CREAR/EDITAR)
        // ============================================

        private void LimpiarFormularioCliente()
        {
            NuevoNombre = string.Empty;
            NuevoSegundoNombre = string.Empty;
            NuevoApellido = string.Empty;
            NuevoSegundoApellido = string.Empty;
            NuevoTelefono = string.Empty;
            NuevoTipoDocumento = "DNI";
            NuevoNumeroDocumento = string.Empty;
            NuevaNacionalidad = string.Empty;
            NuevaDireccion = string.Empty;
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private async Task GuardarClienteAsync()
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(NuevoNombre))
            {
                ErrorMessage = "El nombre es obligatorio";
                return;
            }
            if (string.IsNullOrWhiteSpace(NuevoApellido))
            {
                ErrorMessage = "El apellido es obligatorio";
                return;
            }
            if (string.IsNullOrWhiteSpace(NuevoTipoDocumento))
            {
                ErrorMessage = "El tipo de documento es obligatorio";
                return;
            }
            if (string.IsNullOrWhiteSpace(NuevoNumeroDocumento))
            {
                ErrorMessage = "El numero de documento es obligatorio";
                return;
            }
            if (string.IsNullOrWhiteSpace(NuevoTelefono))
            {
                ErrorMessage = "El telefono es obligatorio";
                return;
            }

            EstaCargando = true;
            ErrorMessage = string.Empty;

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                if (EsEdicionCliente && ClienteSeleccionado != null)
                {
                    // Actualizar cliente existente
                    var queryUpdate = @"
                        UPDATE clientes SET
                            nombre = @nombre,
                            apellidos = @apellidos,
                            telefono = @telefono,
                            documento_tipo = @documento_tipo,
                            documento_numero = @documento_numero
                        WHERE id_cliente = @id_cliente AND id_comercio_registro = @id_comercio";

                    await using var cmdUpdate = new NpgsqlCommand(queryUpdate, conn);
                    cmdUpdate.Parameters.AddWithValue("@nombre", NuevoNombre.Trim());
                    cmdUpdate.Parameters.AddWithValue("@apellidos", NuevoApellido.Trim());
                    cmdUpdate.Parameters.AddWithValue("@telefono", NuevoTelefono.Trim());
                    cmdUpdate.Parameters.AddWithValue("@documento_tipo", NuevoTipoDocumento);
                    cmdUpdate.Parameters.AddWithValue("@documento_numero", NuevoNumeroDocumento.Trim());
                    cmdUpdate.Parameters.AddWithValue("@id_cliente", ClienteSeleccionado.IdCliente);
                    cmdUpdate.Parameters.AddWithValue("@id_comercio", _idComercio);

                    await cmdUpdate.ExecuteNonQueryAsync();

                    // Actualizar modelo local
                    ClienteSeleccionado.Nombre = NuevoNombre.Trim();
                    ClienteSeleccionado.Apellido = NuevoApellido.Trim();
                    ClienteSeleccionado.Telefono = NuevoTelefono.Trim();
                    ClienteSeleccionado.TipoDocumento = NuevoTipoDocumento;
                    ClienteSeleccionado.NumeroDocumento = NuevoNumeroDocumento.Trim();

                    MostrarMensaje("Cliente actualizado correctamente", false);
                }
                else
                {
                    // Crear nuevo cliente (hora Espana)
                    var queryInsert = @"
                        INSERT INTO clientes (nombre, apellidos, telefono, documento_tipo, 
                                             documento_numero, id_comercio_registro, id_local_registro,
                                             id_usuario_registro, activo, fecha_registro)
                        VALUES (@nombre, @apellidos, @telefono, @documento_tipo,
                                @documento_numero, @id_comercio, @id_local, @id_usuario, true,
                                CURRENT_TIMESTAMP AT TIME ZONE 'Europe/Madrid')
                        RETURNING id_cliente";

                    await using var cmdInsert = new NpgsqlCommand(queryInsert, conn);
                    cmdInsert.Parameters.AddWithValue("@nombre", NuevoNombre.Trim());
                    cmdInsert.Parameters.AddWithValue("@apellidos", NuevoApellido.Trim());
                    cmdInsert.Parameters.AddWithValue("@telefono", NuevoTelefono.Trim());
                    cmdInsert.Parameters.AddWithValue("@documento_tipo", NuevoTipoDocumento);
                    cmdInsert.Parameters.AddWithValue("@documento_numero", NuevoNumeroDocumento.Trim());
                    cmdInsert.Parameters.AddWithValue("@id_comercio", _idComercio);
                    cmdInsert.Parameters.AddWithValue("@id_local", _idLocal);
                    cmdInsert.Parameters.AddWithValue("@id_usuario", _idUsuario);

                    var idCliente = (int)(await cmdInsert.ExecuteScalarAsync() ?? 0);

                    if (idCliente > 0)
                    {
                        ClienteSeleccionado = new ClienteModel
                        {
                            IdCliente = idCliente,
                            Nombre = NuevoNombre.Trim(),
                            Apellido = NuevoApellido.Trim(),
                            Telefono = NuevoTelefono.Trim(),
                            TipoDocumento = NuevoTipoDocumento,
                            NumeroDocumento = NuevoNumeroDocumento.Trim()
                        };

                        MostrarMensaje("Cliente creado correctamente", false);
                    }
                }

                ActualizarPropiedadesCliente();
                LimpiarFormularioCliente();

                // Ir a seleccionar/crear beneficiario
                await IrASeleccionBeneficiarioAsync();
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
        // COMANDOS - BENEFICIARIO
        // ============================================

        private void LimpiarFormularioBeneficiario()
        {
            BeneficiarioNombre = string.Empty;
            BeneficiarioSegundoNombre = string.Empty;
            BeneficiarioApellido = string.Empty;
            BeneficiarioSegundoApellido = string.Empty;
            BeneficiarioTipoDocumento = "DNI";
            BeneficiarioNumeroDocumento = string.Empty;
            BeneficiarioTelefono = string.Empty;
            BeneficiarioPais = string.Empty;
            BeneficiarioCiudad = string.Empty;
            BeneficiarioCalle = string.Empty;
            BeneficiarioNumero = string.Empty;
            BeneficiarioPiso = string.Empty;
            BeneficiarioNumeroDepartamento = string.Empty;
            BeneficiarioCodigoPostal = string.Empty;
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private void SeleccionarBeneficiario(BeneficiarioModel? beneficiario)
        {
            if (beneficiario == null) return;

            BeneficiarioSeleccionado = beneficiario;
            ActualizarPropiedadesBeneficiario();

            MostrarMensaje($"Beneficiario: {beneficiario.NombreCompleto}", false);
            IrASeleccionPack();
        }

        [RelayCommand]
        private async Task GuardarBeneficiarioAsync()
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(BeneficiarioNombre))
            {
                ErrorMessage = "El nombre es obligatorio";
                return;
            }
            if (string.IsNullOrWhiteSpace(BeneficiarioApellido))
            {
                ErrorMessage = "El apellido es obligatorio";
                return;
            }
            if (string.IsNullOrWhiteSpace(BeneficiarioTipoDocumento))
            {
                ErrorMessage = "El tipo de documento es obligatorio";
                return;
            }
            if (string.IsNullOrWhiteSpace(BeneficiarioNumeroDocumento))
            {
                ErrorMessage = "El numero de documento es obligatorio";
                return;
            }
            if (string.IsNullOrWhiteSpace(BeneficiarioTelefono))
            {
                ErrorMessage = "El telefono es obligatorio";
                return;
            }
            if (string.IsNullOrWhiteSpace(BeneficiarioPais))
            {
                ErrorMessage = "El pais es obligatorio";
                return;
            }
            if (string.IsNullOrWhiteSpace(BeneficiarioCiudad))
            {
                ErrorMessage = "La ciudad es obligatoria";
                return;
            }
            if (string.IsNullOrWhiteSpace(BeneficiarioCalle))
            {
                ErrorMessage = "La calle es obligatoria";
                return;
            }
            if (string.IsNullOrWhiteSpace(BeneficiarioNumero))
            {
                ErrorMessage = "El numero es obligatorio";
                return;
            }

            if (ClienteSeleccionado == null)
            {
                ErrorMessage = "Debe seleccionar un cliente primero";
                return;
            }

            EstaCargando = true;
            ErrorMessage = string.Empty;

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                if (EsEdicionBeneficiario && BeneficiarioEditando != null)
                {
                    // Actualizar beneficiario existente
                    var queryUpdate = @"
                        UPDATE clientes_beneficiarios SET
                            nombre = @nombre,
                            segundo_nombre = @segundo_nombre,
                            apellido = @apellido,
                            segundo_apellido = @segundo_apellido,
                            tipo_documento = @tipo_documento,
                            numero_documento = @numero_documento,
                            telefono = @telefono,
                            pais = @pais,
                            ciudad = @ciudad,
                            calle = @calle,
                            numero = @numero,
                            piso = @piso,
                            numero_departamento = @numero_departamento,
                            codigo_postal = @codigo_postal,
                            fecha_modificacion = CURRENT_TIMESTAMP AT TIME ZONE 'Europe/Madrid'
                        WHERE id_beneficiario = @id_beneficiario AND id_comercio = @id_comercio";

                    await using var cmdUpdate = new NpgsqlCommand(queryUpdate, conn);
                    cmdUpdate.Parameters.AddWithValue("@nombre", BeneficiarioNombre.Trim());
                    cmdUpdate.Parameters.AddWithValue("@segundo_nombre", BeneficiarioSegundoNombre?.Trim() ?? "");
                    cmdUpdate.Parameters.AddWithValue("@apellido", BeneficiarioApellido.Trim());
                    cmdUpdate.Parameters.AddWithValue("@segundo_apellido", BeneficiarioSegundoApellido?.Trim() ?? "");
                    cmdUpdate.Parameters.AddWithValue("@tipo_documento", BeneficiarioTipoDocumento);
                    cmdUpdate.Parameters.AddWithValue("@numero_documento", BeneficiarioNumeroDocumento.Trim());
                    cmdUpdate.Parameters.AddWithValue("@telefono", BeneficiarioTelefono.Trim());
                    cmdUpdate.Parameters.AddWithValue("@pais", BeneficiarioPais);
                    cmdUpdate.Parameters.AddWithValue("@ciudad", BeneficiarioCiudad.Trim());
                    cmdUpdate.Parameters.AddWithValue("@calle", BeneficiarioCalle.Trim());
                    cmdUpdate.Parameters.AddWithValue("@numero", BeneficiarioNumero.Trim());
                    cmdUpdate.Parameters.AddWithValue("@piso", BeneficiarioPiso?.Trim() ?? "");
                    cmdUpdate.Parameters.AddWithValue("@numero_departamento", BeneficiarioNumeroDepartamento?.Trim() ?? "");
                    cmdUpdate.Parameters.AddWithValue("@codigo_postal", BeneficiarioCodigoPostal?.Trim() ?? "");
                    cmdUpdate.Parameters.AddWithValue("@id_beneficiario", BeneficiarioEditando.IdBeneficiario);
                    cmdUpdate.Parameters.AddWithValue("@id_comercio", _idComercio);

                    await cmdUpdate.ExecuteNonQueryAsync();

                    // Actualizar modelo local
                    BeneficiarioEditando.Nombre = BeneficiarioNombre.Trim();
                    BeneficiarioEditando.SegundoNombre = BeneficiarioSegundoNombre?.Trim() ?? "";
                    BeneficiarioEditando.Apellido = BeneficiarioApellido.Trim();
                    BeneficiarioEditando.SegundoApellido = BeneficiarioSegundoApellido?.Trim() ?? "";
                    BeneficiarioEditando.TipoDocumento = BeneficiarioTipoDocumento;
                    BeneficiarioEditando.NumeroDocumento = BeneficiarioNumeroDocumento.Trim();
                    BeneficiarioEditando.Telefono = BeneficiarioTelefono.Trim();
                    BeneficiarioEditando.Pais = BeneficiarioPais;
                    BeneficiarioEditando.Ciudad = BeneficiarioCiudad.Trim();
                    BeneficiarioEditando.Calle = BeneficiarioCalle.Trim();
                    BeneficiarioEditando.Numero = BeneficiarioNumero.Trim();
                    BeneficiarioEditando.Piso = BeneficiarioPiso?.Trim() ?? "";
                    BeneficiarioEditando.NumeroDepartamento = BeneficiarioNumeroDepartamento?.Trim() ?? "";
                    BeneficiarioEditando.CodigoPostal = BeneficiarioCodigoPostal?.Trim() ?? "";

                    BeneficiarioSeleccionado = BeneficiarioEditando;
                    MostrarMensaje("Beneficiario actualizado correctamente", false);
                }
                else
                {
                    // Insertar beneficiario (hora Espana)
                    var query = @"
                        INSERT INTO clientes_beneficiarios (
                            id_cliente, id_comercio, id_local_registro,
                            nombre, segundo_nombre, apellido, segundo_apellido,
                            tipo_documento, numero_documento, telefono,
                            pais, ciudad, calle, numero, piso, numero_departamento, codigo_postal,
                            activo, fecha_creacion
                        )
                        VALUES (
                            @id_cliente, @id_comercio, @id_local,
                            @nombre, @segundo_nombre, @apellido, @segundo_apellido,
                            @tipo_documento, @numero_documento, @telefono,
                            @pais, @ciudad, @calle, @numero, @piso, @numero_departamento, @codigo_postal,
                            true, CURRENT_TIMESTAMP AT TIME ZONE 'Europe/Madrid'
                        )
                        RETURNING id_beneficiario";

                    await using var cmd = new NpgsqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id_cliente", ClienteSeleccionado.IdCliente);
                    cmd.Parameters.AddWithValue("@id_comercio", _idComercio);
                    cmd.Parameters.AddWithValue("@id_local", _idLocal);
                    cmd.Parameters.AddWithValue("@nombre", BeneficiarioNombre.Trim());
                    cmd.Parameters.AddWithValue("@segundo_nombre", BeneficiarioSegundoNombre?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@apellido", BeneficiarioApellido.Trim());
                    cmd.Parameters.AddWithValue("@segundo_apellido", BeneficiarioSegundoApellido?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@tipo_documento", BeneficiarioTipoDocumento);
                    cmd.Parameters.AddWithValue("@numero_documento", BeneficiarioNumeroDocumento.Trim());
                    cmd.Parameters.AddWithValue("@telefono", BeneficiarioTelefono.Trim());
                    cmd.Parameters.AddWithValue("@pais", BeneficiarioPais);
                    cmd.Parameters.AddWithValue("@ciudad", BeneficiarioCiudad.Trim());
                    cmd.Parameters.AddWithValue("@calle", BeneficiarioCalle.Trim());
                    cmd.Parameters.AddWithValue("@numero", BeneficiarioNumero.Trim());
                    cmd.Parameters.AddWithValue("@piso", BeneficiarioPiso?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@numero_departamento", BeneficiarioNumeroDepartamento?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@codigo_postal", BeneficiarioCodigoPostal?.Trim() ?? "");

                    var idBeneficiario = (int)(await cmd.ExecuteScalarAsync() ?? 0);

                    if (idBeneficiario > 0)
                    {
                        var nuevoBeneficiario = new BeneficiarioModel
                        {
                            IdBeneficiario = idBeneficiario,
                            IdCliente = ClienteSeleccionado.IdCliente,
                            IdComercio = _idComercio,
                            IdLocalRegistro = _idLocal,
                            Nombre = BeneficiarioNombre.Trim(),
                            SegundoNombre = BeneficiarioSegundoNombre?.Trim() ?? "",
                            Apellido = BeneficiarioApellido.Trim(),
                            SegundoApellido = BeneficiarioSegundoApellido?.Trim() ?? "",
                            TipoDocumento = BeneficiarioTipoDocumento,
                            NumeroDocumento = BeneficiarioNumeroDocumento.Trim(),
                            Telefono = BeneficiarioTelefono.Trim(),
                            Pais = BeneficiarioPais,
                            Ciudad = BeneficiarioCiudad.Trim(),
                            Calle = BeneficiarioCalle.Trim(),
                            Numero = BeneficiarioNumero.Trim(),
                            Piso = BeneficiarioPiso?.Trim() ?? "",
                            NumeroDepartamento = BeneficiarioNumeroDepartamento?.Trim() ?? "",
                            CodigoPostal = BeneficiarioCodigoPostal?.Trim() ?? ""
                        };

                        BeneficiariosCliente.Add(nuevoBeneficiario);
                        BeneficiarioSeleccionado = nuevoBeneficiario;

                        MostrarMensaje("Beneficiario creado correctamente", false);
                    }
                }

                ActualizarPropiedadesBeneficiario();
                LimpiarFormularioBeneficiario();

                // Ir a seleccion de pack
                IrASeleccionPack();
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

        [RelayCommand]
        private async Task EliminarBeneficiarioAsync(BeneficiarioModel? beneficiario)
        {
            if (beneficiario == null) return;

            EstaCargando = true;

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                // Soft delete
                var query = @"
                    UPDATE clientes_beneficiarios 
                    SET activo = false, fecha_modificacion = CURRENT_TIMESTAMP AT TIME ZONE 'Europe/Madrid'
                    WHERE id_beneficiario = @id_beneficiario AND id_comercio = @id_comercio";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id_beneficiario", beneficiario.IdBeneficiario);
                cmd.Parameters.AddWithValue("@id_comercio", _idComercio);

                await cmd.ExecuteNonQueryAsync();

                BeneficiariosCliente.Remove(beneficiario);

                if (BeneficiarioSeleccionado?.IdBeneficiario == beneficiario.IdBeneficiario)
                {
                    BeneficiarioSeleccionado = null;
                }

                ActualizarPropiedadesBeneficiario();
                MostrarMensaje("Beneficiario eliminado", false);
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
        // COMANDOS - FINALIZAR COMPRA
        // ============================================

        [RelayCommand]
        private async Task FinalizarCompraAsync()
        {
            if (ClienteSeleccionado == null || PackSeleccionado == null || BeneficiarioSeleccionado == null)
            {
                MostrarMensaje("Datos incompletos para la compra", true);
                return;
            }

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

                    // 2. Insertar operacion principal (hora Espana)
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
                            'EFECTIVO', 
                            (CURRENT_TIMESTAMP AT TIME ZONE 'Europe/Madrid')::DATE,
                            (CURRENT_TIMESTAMP AT TIME ZONE 'Europe/Madrid')::TIME
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

                    // 3. Verificar si hay beneficio acumulado para este local
                    decimal beneficioDisponible = 0;
                    var sqlBeneficio = "SELECT COALESCE(beneficio_acumulado, 0) FROM locales WHERE id_local = @idLocal";
                    await using var cmdBeneficio = new NpgsqlCommand(sqlBeneficio, conn, transaction);
                    cmdBeneficio.Parameters.AddWithValue("@idLocal", _idLocal);
                    var resultBeneficio = await cmdBeneficio.ExecuteScalarAsync();
                    if (resultBeneficio != null && resultBeneficio != DBNull.Value)
                        beneficioDisponible = Convert.ToDecimal(resultBeneficio);

                    // Determinar estado inicial basado en beneficio
                    string estadoEnvio = "PENDIENTE";
                    decimal beneficioUsado = 0;

                    if (beneficioDisponible >= TotalCompra)
                    {
                        // El beneficio cubre todo el costo - marcar como PAGADO
                        estadoEnvio = "PAGADO";
                        beneficioUsado = TotalCompra;
                        
                        // Descontar beneficio usado
                        var sqlDescontar = @"UPDATE locales 
                                            SET beneficio_acumulado = beneficio_acumulado - @beneficioUsado 
                                            WHERE id_local = @idLocal";
                        await using var cmdDescontar = new NpgsqlCommand(sqlDescontar, conn, transaction);
                        cmdDescontar.Parameters.AddWithValue("@beneficioUsado", beneficioUsado);
                        cmdDescontar.Parameters.AddWithValue("@idLocal", _idLocal);
                        await cmdDescontar.ExecuteNonQueryAsync();
                    }

                    // 4. Insertar detalle de pack alimentos
                    var queryDetalle = @"
                        INSERT INTO operaciones_pack_alimentos (
                            id_operacion, id_beneficiario, nombre_pack, descripcion_pack,
                            pais_destino, ciudad_destino, precio_pack,
                            estado_envio, observaciones
                        )
                        VALUES (
                            @id_operacion, @id_beneficiario, @nombre_pack, @descripcion_pack,
                            @pais_destino, @ciudad_destino, @precio_pack,
                            @estado_envio, @observaciones
                        )";

                    var observaciones = $"Beneficiario: {BeneficiarioSeleccionado.NombreCompleto} | " +
                                    $"Doc: {BeneficiarioSeleccionado.DocumentoCompleto} | " +
                                    $"Dir: {BeneficiarioSeleccionado.DireccionCompleta} | " +
                                    $"Tel: {BeneficiarioSeleccionado.Telefono}";

                    if (beneficioUsado > 0)
                        observaciones += $" | PAGADO CON BENEFICIO: {beneficioUsado:N2} EUR";

                    await using var cmdDetalle = new NpgsqlCommand(queryDetalle, conn, transaction);
                    cmdDetalle.Parameters.AddWithValue("@id_operacion", idOperacion);
                    cmdDetalle.Parameters.AddWithValue("@id_beneficiario", BeneficiarioSeleccionado.IdBeneficiario);
                    cmdDetalle.Parameters.AddWithValue("@nombre_pack", PackSeleccionado.NombrePack);
                    cmdDetalle.Parameters.AddWithValue("@descripcion_pack", PackSeleccionado.Descripcion ?? "");
                    cmdDetalle.Parameters.AddWithValue("@pais_destino", BeneficiarioSeleccionado.Pais);
                    cmdDetalle.Parameters.AddWithValue("@ciudad_destino", BeneficiarioSeleccionado.Ciudad);
                    cmdDetalle.Parameters.AddWithValue("@precio_pack", TotalCompra);
                    cmdDetalle.Parameters.AddWithValue("@estado_envio", estadoEnvio);
                    cmdDetalle.Parameters.AddWithValue("@observaciones", observaciones);

                    await cmdDetalle.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();

                    // Mensaje según el estado
                    var mensajeFinal = beneficioUsado > 0
                        ? $"Compra finalizada y PAGADA con beneficio. Operacion: {NumeroOperacion}"
                        : $"Compra finalizada correctamente. Operacion: {NumeroOperacion}";

                    MostrarMensaje(mensajeFinal, false);

                    
                    // Esperar un momento y luego ir a la pantalla de selección de países
                    await Task.Delay(2000);
                    
                    // Limpiar datos y volver a selección de países
                    ClienteSeleccionado = null;
                    BeneficiarioSeleccionado = null;
                    BeneficiarioEditando = null;
                    PackSeleccionado = null;
                    PaisSeleccionado = null;
                    NumeroOperacion = string.Empty;
                    TotalCompra = 0;
                    BeneficiariosCliente.Clear();
                    PacksDisponibles.Clear();
                    
                    LimpiarFormularioBeneficiario();
                    LimpiarFormularioCliente();
                    BusquedaCliente = string.Empty;
                    
                    ActualizarPropiedadesCliente();
                    ActualizarPropiedadesBeneficiario();
                    OnPropertyChanged(nameof(PaisSeleccionadoNombre));
                    
                    OcultarTodasLasVistas();
                    VistaSeleccionPais = true;
                    
                    // Recargar países
                    await CargarPaisesDisponiblesAsync();
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
            try
            {
                var query = @"SELECT generar_numero_operacion('PACK_ALIMENTOS', @id_local)";
                await using var cmd = new NpgsqlCommand(query, conn, transaction);
                cmd.Parameters.AddWithValue("@id_local", _idLocal);
                var resultado = await cmd.ExecuteScalarAsync();
                return resultado?.ToString() ?? $"PA{DateTime.Now:yyyyMMddHHmmss}";
            }
            catch
            {
                return $"PA{DateTime.Now:yyyyMMddHHmmss}";
            }
        }

        // ============================================
        // COMANDOS - IMPRIMIR RECIBO
        // ============================================

        [RelayCommand]
        private async Task ImprimirReciboAsync(Avalonia.Controls.Window? window)
        {
            if (ClienteSeleccionado == null || PackSeleccionado == null || BeneficiarioSeleccionado == null)
            {
                MostrarMensaje("Datos incompletos para generar recibo", true);
                return;
            }

            if (window == null)
            {
                MostrarMensaje("No se puede abrir el dialogo de guardar", true);
                return;
            }

            try
            {
                var nombreSugerido = $"Recibo_FoodPack_{(string.IsNullOrEmpty(NumeroOperacion) ? DateTime.Now.ToString("yyyyMMdd_HHmmss") : NumeroOperacion)}.pdf";
                
                var archivo = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Guardar Recibo PDF",
                    SuggestedFileName = nombreSugerido,
                    DefaultExtension = "pdf",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("Documento PDF") { Patterns = new[] { "*.pdf" } }
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
                    ClienteDireccion = "",
                    ClienteNacionalidad = "",

                    // Beneficiario
                    BeneficiarioNombre = BeneficiarioSeleccionado.NombreCompleto,
                    BeneficiarioTipoDocumento = BeneficiarioSeleccionado.TipoDocumento,
                    BeneficiarioNumeroDocumento = BeneficiarioSeleccionado.NumeroDocumento,
                    BeneficiarioDireccion = BeneficiarioSeleccionado.DireccionCompleta,
                    BeneficiarioTelefono = BeneficiarioSeleccionado.Telefono,
                    BeneficiarioPaisDestino = BeneficiarioSeleccionado.Pais,
                    BeneficiarioCiudadDestino = BeneficiarioSeleccionado.Ciudad,

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
            BeneficiarioSeleccionado = null;
            BeneficiarioEditando = null;
            PackSeleccionado = null;
            PaisSeleccionado = null;
            NumeroOperacion = string.Empty;
            TotalCompra = 0;
            BeneficiariosCliente.Clear();
            PacksDisponibles.Clear();
            
            LimpiarFormularioBeneficiario();
            LimpiarFormularioCliente();
            BusquedaCliente = string.Empty;
            
            ActualizarPropiedadesCliente();
            ActualizarPropiedadesBeneficiario();
            OnPropertyChanged(nameof(PaisSeleccionadoNombre));
            
            OcultarTodasLasVistas();
            VistaSeleccionPais = true;
            
            // Recargar paises
            _ = CargarPaisesDisponiblesAsync();
            
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

    public class PaisDesignadoFront
    {
        public int IdPais { get; set; }
        public string NombrePais { get; set; } = string.Empty;
        public string? CodigoIso { get; set; }
        public byte[]? BanderaImagen { get; set; }

        public bool TieneBandera => BanderaImagen != null && BanderaImagen.Length > 0;
        public string NombreConCodigo => !string.IsNullOrEmpty(CodigoIso) 
            ? $"{NombrePais} ({CodigoIso})" 
            : NombrePais;
    }

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