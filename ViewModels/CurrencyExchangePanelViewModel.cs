using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models;
using Allva.Desktop.Services;
using Npgsql;

namespace Allva.Desktop.ViewModels;

public partial class CurrencyExchangePanelViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";
    
    private readonly CurrencyExchangeService _currencyService;
    
    // Zona horaria de España (Europe/Madrid)
    private static readonly TimeZoneInfo _zonaHorariaEspana = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Romance Standard Time" : "Europe/Madrid");
    
    // Margen de ganancia (INTERNO - NUNCA MOSTRAR AL USUARIO)
    private decimal _margenGanancia = 10.00m;
    private int _idLocalActual = 0;
    private int _idComercioActual = 0;
    
    // Datos del usuario actual (se deben establecer desde la sesión)
    private int _idUsuarioActual = 0;
    private string? _nombreUsuarioActual;
    private string? _numeroUsuarioActual;

    // ═══════════════════════════════════════════════════════════
    // NAVEGACIÓN ENTRE VISTAS
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private bool _vistaPrincipal = true;
    [ObservableProperty] private bool _vistaBuscarCliente = false;
    [ObservableProperty] private bool _vistaNuevoCliente = false;
    [ObservableProperty] private bool _vistaConfirmacionCliente = false;
    [ObservableProperty] private bool _vistaTransaccion = false;
    [ObservableProperty] private bool _vistaResumen = false;
    [ObservableProperty] private bool _vistaEditarCliente = false;

    // ═══════════════════════════════════════════════════════════
    // CLIENTE
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private ClienteModel? _clienteSeleccionado;
    [ObservableProperty] private ObservableCollection<ClienteModel> _clientesEncontrados = new();
    [ObservableProperty] private string _busquedaCliente = string.Empty;
    
    public ObservableCollection<string> TiposBusquedaCliente { get; } = new()
    {
        "Todos", "Nombre", "Documento", "Telefono"
    };
    
    [ObservableProperty] private string _tipoBusquedaSeleccionado = "Todos";
    
    // ═══════════════════════════════════════════════════════════
    // ACUMULADOS DEL CLIENTE (MES Y TRIMESTRE)
    // ═══════════════════════════════════════════════════════════
    
    private const decimal LIMITE_TRIMESTRE = 3000.00m;
    
    [ObservableProperty] private decimal _acumuladoMes = 0;
    [ObservableProperty] private decimal _acumuladoTrimestre = 0;
    [ObservableProperty] private string _mesActualNombre = string.Empty;
    [ObservableProperty] private string _trimestreActualNombre = string.Empty;
    [ObservableProperty] private decimal _disponibleTrimestre = LIMITE_TRIMESTRE;
    [ObservableProperty] private bool _limiteTrimestreExcedido = false;
    [ObservableProperty] private bool _mostrarDisponibleTrimestre = true;
    
    public string ColorAcumuladoTrimestre => LimiteTrimestreExcedido ? "#FFEBEE" : (AcumuladoTrimestre > LIMITE_TRIMESTRE * 0.8m ? "#FFF3E0" : "#E8F5E9");
    public string ColorTextoAcumuladoTrimestre => LimiteTrimestreExcedido ? "#C62828" : (AcumuladoTrimestre > LIMITE_TRIMESTRE * 0.8m ? "#E65100" : "#2E7D32");

    // ═══════════════════════════════════════════════════════════
    // CAMPOS NUEVO/EDITAR CLIENTE (con segundo nombre/apellido)
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private string _nuevoNombre = string.Empty;
    [ObservableProperty] private string _nuevoSegundoNombre = string.Empty;
    [ObservableProperty] private string _nuevoApellido = string.Empty;
    [ObservableProperty] private string _nuevoSegundoApellido = string.Empty;
    [ObservableProperty] private string _nuevoTelefono = string.Empty;
    [ObservableProperty] private string _nuevaDireccion = string.Empty;
    [ObservableProperty] private string _nuevaNacionalidad = string.Empty;
    [ObservableProperty] private string _nuevoTipoDocumento = "DNI";
    [ObservableProperty] private string _nuevoNumeroDocumento = string.Empty;
    
    // Para edición - guardamos el ID del cliente que estamos editando
    private int _idClienteEditando = 0;
    [ObservableProperty] private bool _esModoEdicion = false;
    [ObservableProperty] private string _tituloFormularioCliente = "Registrar Nuevo Cliente";
    
    // Imágenes documento
    [ObservableProperty] private byte[]? _nuevaImagenDocumentoFrontal;
    [ObservableProperty] private byte[]? _nuevaImagenDocumentoTrasera;
    [ObservableProperty] private bool _tieneImagenDocumentoFrontal = false;
    [ObservableProperty] private bool _tieneImagenDocumentoTrasera = false;
    public bool TieneAlgunaImagenDocumento => TieneImagenDocumentoFrontal || TieneImagenDocumentoTrasera;
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? _imagenDocumentoFrontalPreview;
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? _imagenDocumentoTraseraPreview;
    [ObservableProperty] private bool _mostrarPreviewImagen = false;
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? _imagenPreviewActual;
    [ObservableProperty] private string _tituloPreviewImagen = string.Empty;
    
    // Propiedades calculadas para confirmación
    public string NuevoNombreCompletoPreview
    {
        get
        {
            var partes = new[] { NuevoNombre, NuevoSegundoNombre, NuevoApellido, NuevoSegundoApellido };
            return string.Join(" ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }
    
    [ObservableProperty] 
    private string _nuevaFechaNacimientoTexto = string.Empty;

    [ObservableProperty] 
    private string _nuevaCaducidadDocumentoTexto = string.Empty;

    public bool TieneImagenesDocumento => TieneImagenDocumentoFrontal || TieneImagenDocumentoTrasera;

    public ObservableCollection<string> TiposDocumento { get; } = new() { "DNI", "NIE", "Pasaporte" };

    // ═══════════════════════════════════════════════════════════
    // DIVISAS
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private ObservableCollection<FavoriteCurrencyModel> _favoriteCurrencies = new();
    [ObservableProperty] private ObservableCollection<CurrencyModel> _availableCurrencies = new();
    [ObservableProperty] private ObservableCollection<CurrencyModel> _divisasBusqueda = new();
    [ObservableProperty] private string _busquedaDivisa = string.Empty;
    [ObservableProperty] private string _busquedaDivisaCalculadora = string.Empty;
    [ObservableProperty] private CurrencyModel? _selectedCurrency;
    [ObservableProperty] private bool _mostrarSelectorDivisas = false;

    // ═══════════════════════════════════════════════════════════
    // CALCULADORA - BIDIRECCIONAL
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private string _amountReceived = string.Empty;
    [ObservableProperty] private string _totalInEurosTexto = string.Empty;
    [ObservableProperty] private decimal _totalInEuros;
    [ObservableProperty] private decimal _currentRate;
    [ObservableProperty] private string _rateDisplayText = string.Empty;
    
    // Flag para evitar loop infinito en cálculo bidireccional
    private bool _calculandoDesdeDivisa = false;
    private bool _calculandoDesdeEuros = false;

    // ═══════════════════════════════════════════════════════════
    // OPERACIÓN / TRANSACCIÓN
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private long _numeroOperacion = 0;
    [ObservableProperty] private string _numeroOperacionDisplay = string.Empty;
    [ObservableProperty] private DateTime _fechaOperacion;
    
    // Flag para saber si la operación ya fue guardada
    private bool _operacionGuardada = false;

    // ═══════════════════════════════════════════════════════════
    // ESTADOS
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _lastUpdateText = string.Empty;
    [ObservableProperty] private bool _puedeRealizarOperacion = false;

    private List<string> _favoriteCodes = new() { "USD", "CAD", "CHF", "GBP", "CNY" };

    // ═══════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════

    public CurrencyExchangePanelViewModel()
    {
        _currencyService = new CurrencyExchangeService();
        InitializeAsync();
    }
    
    public CurrencyExchangePanelViewModel(int idLocal, int idComercio)
    {
        _idLocalActual = idLocal;
        _idComercioActual = idComercio;
        _currencyService = new CurrencyExchangeService();
        InitializeAsync();
    }
    
    public CurrencyExchangePanelViewModel(int idLocal, int idComercio, int idUsuario, string nombreUsuario, string numeroUsuario)
    {
        _idLocalActual = idLocal;
        _idComercioActual = idComercio;
        _idUsuarioActual = idUsuario;
        _nombreUsuarioActual = nombreUsuario;
        _numeroUsuarioActual = numeroUsuario;
        _currencyService = new CurrencyExchangeService();
        InitializeAsync();
    }
    
    /// <summary>
    /// Establece los datos de la sesión del usuario
    /// </summary>
    public void SetSesionData(int idLocal, int idComercio, int idUsuario, string nombreUsuario, string numeroUsuario)
    {
        _idLocalActual = idLocal;
        _idComercioActual = idComercio;
        _idUsuarioActual = idUsuario;
        _nombreUsuarioActual = nombreUsuario;
        _numeroUsuarioActual = numeroUsuario;
    }

    // ═══════════════════════════════════════════════════════════
    // ZONA HORARIA - ESPAÑA
    // ═══════════════════════════════════════════════════════════
    
    private DateTime ObtenerHoraEspana()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zonaHorariaEspana);
    }

    private async void InitializeAsync()
    {
        ActualizarNombresMesYTrimestre();
        await CargarMargenGananciaAsync();
        await LoadDataAsync();
    }
    
    private void ActualizarNombresMesYTrimestre()
    {
        var ahora = ObtenerHoraEspana();
        var cultura = new CultureInfo("es-ES");
        
        // Nombre del mes
        MesActualNombre = cultura.DateTimeFormat.GetMonthName(ahora.Month).ToUpper() + " " + ahora.Year;
        
        // Nombre del trimestre
        int trimestre = (ahora.Month - 1) / 3 + 1;
        TrimestreActualNombre = $"T{trimestre} {ahora.Year}";
    }

    // ═══════════════════════════════════════════════════════════
    // NAVEGACIÓN
    // ═══════════════════════════════════════════════════════════
    
    private void OcultarTodasLasVistas()
    {
        VistaPrincipal = false;
        VistaBuscarCliente = false;
        VistaNuevoCliente = false;
        VistaConfirmacionCliente = false;
        VistaTransaccion = false;
        VistaResumen = false;
        VistaEditarCliente = false;
    }
    
    [RelayCommand]
    private async Task IrABuscarClienteAsync()
    {
        OcultarTodasLasVistas();
        ClientesEncontrados.Clear();
        ErrorMessage = string.Empty;
        VistaBuscarCliente = true;
        
        // Si ya hay texto de busqueda, ejecutar busqueda automaticamente
        if (!string.IsNullOrWhiteSpace(BusquedaCliente))
        {
            await BuscarClientesAsync(BusquedaCliente);
        }
    }
    
    [RelayCommand]
    private void IrANuevoCliente()
    {
        OcultarTodasLasVistas();
        LimpiarFormularioCliente();
        EsModoEdicion = false;
        TituloFormularioCliente = "Registrar Nuevo Cliente";
        ErrorMessage = string.Empty;
        VistaNuevoCliente = true;
    }
    
    [RelayCommand]
    private void IrAEditarCliente()
    {
        if (ClienteSeleccionado == null) return;
        
        OcultarTodasLasVistas();
        CargarDatosClienteParaEdicion();
        EsModoEdicion = true;
        TituloFormularioCliente = "Editar Cliente";
        ErrorMessage = string.Empty;
        VistaEditarCliente = true;
    }
    
    private void CargarDatosClienteParaEdicion()
    {
        if (ClienteSeleccionado == null) return;
        
        _idClienteEditando = ClienteSeleccionado.IdCliente;
        NuevoNombre = ClienteSeleccionado.Nombre ?? "";
        NuevoSegundoNombre = ClienteSeleccionado.SegundoNombre ?? "";
        NuevoApellido = ClienteSeleccionado.Apellido ?? "";
        NuevoSegundoApellido = ClienteSeleccionado.SegundoApellido ?? "";
        NuevoTelefono = ClienteSeleccionado.Telefono ?? "";
        NuevaDireccion = ClienteSeleccionado.Direccion ?? "";
        NuevaNacionalidad = ClienteSeleccionado.Nacionalidad ?? "";
        NuevoTipoDocumento = ClienteSeleccionado.TipoDocumento ?? "DNI";
        NuevoNumeroDocumento = ClienteSeleccionado.NumeroDocumento ?? "";
        
        // Fechas
        NuevaFechaNacimientoTexto = ClienteSeleccionado.FechaNacimiento?.ToString("dd/MM/yyyy") ?? "";
        NuevaCaducidadDocumentoTexto = ClienteSeleccionado.CaducidadDocumento?.ToString("dd/MM/yyyy") ?? "";
        
        // Limpiar imágenes previas primero
        NuevaImagenDocumentoFrontal = null;
        NuevaImagenDocumentoTrasera = null;
        TieneImagenDocumentoFrontal = false;
        TieneImagenDocumentoTrasera = false;
        ImagenDocumentoFrontalPreview = null;
        ImagenDocumentoTraseraPreview = null;
        
        // Cargar imagen frontal si existe
        if (ClienteSeleccionado.ImagenDocumentoFrontal != null && ClienteSeleccionado.ImagenDocumentoFrontal.Length > 0)
        {
            NuevaImagenDocumentoFrontal = ClienteSeleccionado.ImagenDocumentoFrontal;
            TieneImagenDocumentoFrontal = true;
            try
            {
                using var stream = new MemoryStream(ClienteSeleccionado.ImagenDocumentoFrontal);
                ImagenDocumentoFrontalPreview = new Avalonia.Media.Imaging.Bitmap(stream);
            }
            catch { ImagenDocumentoFrontalPreview = null; }
        }
        
        // Cargar imagen trasera si existe
        if (ClienteSeleccionado.ImagenDocumentoTrasera != null && ClienteSeleccionado.ImagenDocumentoTrasera.Length > 0)
        {
            NuevaImagenDocumentoTrasera = ClienteSeleccionado.ImagenDocumentoTrasera;
            TieneImagenDocumentoTrasera = true;
            try
            {
                using var stream = new MemoryStream(ClienteSeleccionado.ImagenDocumentoTrasera);
                ImagenDocumentoTraseraPreview = new Avalonia.Media.Imaging.Bitmap(stream);
            }
            catch { ImagenDocumentoTraseraPreview = null; }
        }
        
        OnPropertyChanged(nameof(TieneAlgunaImagenDocumento));
    }
    
    [RelayCommand]
    private void VolverAPrincipal()
    {
        OcultarTodasLasVistas();
        ErrorMessage = string.Empty;
        VistaPrincipal = true;
    }
    
    [RelayCommand]
    private void VolverABuscarCliente()
    {
        OcultarTodasLasVistas();
        ErrorMessage = string.Empty;
        VistaBuscarCliente = true;
    }
    
    [RelayCommand]
    private void VolverANuevoCliente()
    {
        OcultarTodasLasVistas();
        ErrorMessage = string.Empty;
        VistaNuevoCliente = true;
    }
    
    [RelayCommand]
    private void VolverATransaccion()
    {
        // Si vuelve desde el resumen sin finalizar, no se guarda nada
        OcultarTodasLasVistas();
        ErrorMessage = string.Empty;
        VistaTransaccion = true;
    }
    
    [RelayCommand]
    private void VolverDeEdicionATransaccion()
    {
        OcultarTodasLasVistas();
        ErrorMessage = string.Empty;
        VistaTransaccion = true;
    }
    
    [RelayCommand]
    private void MostrarConfirmacionCliente()
    {
        if (string.IsNullOrWhiteSpace(NuevoNombre))
        {
            ErrorMessage = "El primer nombre es obligatorio";
            return;
        }
        if (string.IsNullOrWhiteSpace(NuevoApellido))
        {
            ErrorMessage = "El primer apellido es obligatorio";
            return;
        }
        if (string.IsNullOrWhiteSpace(NuevoNumeroDocumento))
        {
            ErrorMessage = "El numero de documento es obligatorio";
            return;
        }
        
        // Validar fecha de nacimiento (mayor de edad)
        if (!string.IsNullOrWhiteSpace(NuevaFechaNacimientoTexto))
        {
            if (DateTime.TryParseExact(NuevaFechaNacimientoTexto, "dd/MM/yyyy", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaNac))
            {
                var hoy = ObtenerHoraEspana().Date;
                var edad = hoy.Year - fechaNac.Year;
                if (fechaNac.Date > hoy.AddYears(-edad)) edad--;
                
                if (edad < 18)
                {
                    ErrorMessage = "El cliente es menor de edad. No puede realizar operaciones.";
                    return;
                }
            }
            else
            {
                ErrorMessage = "Formato de fecha de nacimiento invalido. Use dd/mm/aaaa";
                return;
            }
        }
        
        // Validar fecha de caducidad del documento
        if (!string.IsNullOrWhiteSpace(NuevaCaducidadDocumentoTexto))
        {
            if (DateTime.TryParseExact(NuevaCaducidadDocumentoTexto, "dd/MM/yyyy", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaCad))
            {
                if (fechaCad.Date < ObtenerHoraEspana().Date)
                {
                    ErrorMessage = "El documento esta vencido. No puede realizar operaciones.";
                    return;
                }
            }
            else
            {
                ErrorMessage = "Formato de fecha de caducidad invalido. Use dd/mm/aaaa";
                return;
            }
        }
        
        ErrorMessage = string.Empty;
        OcultarTodasLasVistas();
        OnPropertyChanged(nameof(NuevoNombreCompletoPreview));
        VistaConfirmacionCliente = true;
    }
    
    /// <summary>
    /// FINALIZAR: Guarda la operación en BD y vuelve a principal
    /// </summary>
    [RelayCommand]
    private async Task FinalizarYVolverAPrincipalAsync()
    {
        // Solo guardar si no se ha guardado antes
        if (!_operacionGuardada)
        {
            var guardadoExitoso = await GuardarOperacionEnBDAsync();
            if (!guardadoExitoso)
            {
                // Si hay error, no salir del resumen
                return;
            }
        }
        
        // Limpiar datos de la operación
        ClienteSeleccionado = null;
        AmountReceived = string.Empty;
        TotalInEurosTexto = string.Empty;
        TotalInEuros = 0;
        NumeroOperacion = 0;
        NumeroOperacionDisplay = string.Empty;
        _operacionGuardada = false;
        
        OcultarTodasLasVistas();
        VistaPrincipal = true;
    }

    // ═══════════════════════════════════════════════════════════
    // ACUMULADOS DEL CLIENTE
    // ═══════════════════════════════════════════════════════════
    
    [RelayCommand]
    private async Task ActualizarAcumuladosAsync()
    {
        if (ClienteSeleccionado == null) return;
        await CargarAcumuladosClienteAsync(ClienteSeleccionado.IdCliente);
    }
    
    private async Task CargarAcumuladosClienteAsync(int idCliente)
    {
        try
        {
            IsLoading = true;
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            var ahora = ObtenerHoraEspana();
            
            // Primer día del mes actual
            var primerDiaMes = new DateTime(ahora.Year, ahora.Month, 1);
            
            // Primer día del trimestre actual
            int trimestre = (ahora.Month - 1) / 3;
            var primerDiaTrimestre = new DateTime(ahora.Year, trimestre * 3 + 1, 1);
            
            // Consulta para acumulado del mes
            var sqlMes = @"SELECT COALESCE(SUM(importe_total), 0) 
                           FROM operaciones 
                           WHERE id_cliente = @idCliente 
                             AND modulo = 'DIVISAS'
                             AND estado = 'COMPLETADA'
                             AND fecha_operacion >= @fechaInicio";
            
            await using var cmdMes = new NpgsqlCommand(sqlMes, conn);
            cmdMes.Parameters.AddWithValue("idCliente", idCliente);
            cmdMes.Parameters.AddWithValue("fechaInicio", primerDiaMes);
            AcumuladoMes = Convert.ToDecimal(await cmdMes.ExecuteScalarAsync());
            
            // Consulta para acumulado del trimestre
            var sqlTrimestre = @"SELECT COALESCE(SUM(importe_total), 0) 
                                 FROM operaciones 
                                 WHERE id_cliente = @idCliente 
                                   AND modulo = 'DIVISAS'
                                   AND estado = 'COMPLETADA'
                                   AND fecha_operacion >= @fechaInicio";
            
            await using var cmdTrimestre = new NpgsqlCommand(sqlTrimestre, conn);
            cmdTrimestre.Parameters.AddWithValue("idCliente", idCliente);
            cmdTrimestre.Parameters.AddWithValue("fechaInicio", primerDiaTrimestre);
            AcumuladoTrimestre = Convert.ToDecimal(await cmdTrimestre.ExecuteScalarAsync());
            
            // Calcular disponible y estado
            DisponibleTrimestre = LIMITE_TRIMESTRE - AcumuladoTrimestre;
            LimiteTrimestreExcedido = AcumuladoTrimestre >= LIMITE_TRIMESTRE;
            MostrarDisponibleTrimestre = !LimiteTrimestreExcedido;
            
            // Notificar cambios de colores
            OnPropertyChanged(nameof(ColorAcumuladoTrimestre));
            OnPropertyChanged(nameof(ColorTextoAcumuladoTrimestre));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar acumulados: {ex.Message}";
            AcumuladoMes = 0;
            AcumuladoTrimestre = 0;
            DisponibleTrimestre = LIMITE_TRIMESTRE;
            LimiteTrimestreExcedido = false;
            MostrarDisponibleTrimestre = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CARGA DE MARGEN (INTERNO)
    // ═══════════════════════════════════════════════════════════

    private async Task CargarMargenGananciaAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            decimal? margenLocal = null;
            decimal? margenComercio = null;
            decimal? margenGlobal = null;

            if (_idLocalActual > 0)
            {
                var sqlLocal = "SELECT comision_divisas FROM locales WHERE id_local = @id AND comision_divisas > 0";
                await using var cmdLocal = new NpgsqlCommand(sqlLocal, conn);
                cmdLocal.Parameters.AddWithValue("id", _idLocalActual);
                var result = await cmdLocal.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    margenLocal = Convert.ToDecimal(result);
            }

            if (_idComercioActual > 0 && margenLocal == null)
            {
                var sqlComercio = "SELECT porcentaje_comision_divisas FROM comercios WHERE id_comercio = @id AND porcentaje_comision_divisas > 0";
                await using var cmdComercio = new NpgsqlCommand(sqlComercio, conn);
                cmdComercio.Parameters.AddWithValue("id", _idComercioActual);
                var result = await cmdComercio.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    margenComercio = Convert.ToDecimal(result);
            }

            if (margenLocal == null && margenComercio == null)
            {
                var sqlGlobal = "SELECT valor_decimal FROM configuracion_sistema WHERE clave = 'margen_divisas_global'";
                await using var cmdGlobal = new NpgsqlCommand(sqlGlobal, conn);
                var result = await cmdGlobal.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    margenGlobal = Convert.ToDecimal(result);
            }

            _margenGanancia = margenLocal ?? margenComercio ?? margenGlobal ?? 10.00m;
        }
        catch
        {
            _margenGanancia = 10.00m;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CARGA DE DATOS
    // ═══════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        
        try
        {
            var allCurrencies = GetAllCurrenciesList();
            var rates = await _currencyService.GetExchangeRatesAsync();
            
            foreach (var currency in allCurrencies)
            {
                if (rates.TryGetValue(currency.Code, out decimal rate))
                {
                    currency.RateToEur = rate;
                    currency.RateWithMargin = rate * (1m - (_margenGanancia / 100m));
                }
                else
                {
                    currency.RateToEur = 1m;
                    currency.RateWithMargin = 1m * (1m - (_margenGanancia / 100m));
                }
                currency.DisplayText = $"{currency.Code} | {currency.Name}";
            }
            
            AvailableCurrencies = new ObservableCollection<CurrencyModel>(allCurrencies);
            await LoadFavoriteCurrenciesAsync();
            
            if (AvailableCurrencies.Count > 0 && SelectedCurrency == null)
                SelectedCurrency = AvailableCurrencies.First();
            
            var lastUpdate = _currencyService.GetLastUpdateTime();
            LastUpdateText = lastUpdate > DateTime.MinValue 
                ? $"Actualizado: {lastUpdate:HH:mm}" 
                : "Tasas predeterminadas";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar datos: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadFavoriteCurrenciesAsync()
    {
        try
        {
            var favoriteCodes = new List<string>();
            
            if (_idLocalActual > 0)
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();
                
                var sql = @"SELECT codigo_divisa FROM divisas_favoritas_local 
                            WHERE id_local = @idLocal ORDER BY orden, id";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("idLocal", _idLocalActual);
                
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    favoriteCodes.Add(reader.GetString(0));
                }
            }
            
            // Si no hay guardadas en BD, usar predefinidas
            if (favoriteCodes.Count == 0)
            {
                favoriteCodes = _favoriteCodes;
            }
            
            var favorites = await _currencyService.GetFavoriteCurrenciesAsync(favoriteCodes);
            foreach (var fav in favorites)
                fav.RateWithMargin = fav.RateToEur * (1m - (_margenGanancia / 100m));
            
            FavoriteCurrencies = new ObservableCollection<FavoriteCurrencyModel>(favorites);
        }
        catch
        {
            FavoriteCurrencies = new ObservableCollection<FavoriteCurrencyModel>();
        }
    }

    [RelayCommand]
    private async Task RefreshRatesAsync()
    {
        await _currencyService.ReloadConfigurationAsync();
        await LoadDataAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // SELECTOR DE DIVISAS (POPUP)
    // ═══════════════════════════════════════════════════════════
    
    [RelayCommand]
    private void MostrarAgregarDivisa()
    {
        BusquedaDivisa = string.Empty;
        CargarDivisasDisponibles();
        MostrarSelectorDivisas = true;
    }
    
    [RelayCommand]
    private void CerrarSelectorDivisas()
    {
        MostrarSelectorDivisas = false;
    }
    
    private void CargarDivisasDisponibles()
    {
        var favCodes = FavoriteCurrencies.Select(f => f.Code).ToHashSet();
        DivisasBusqueda.Clear();
        foreach (var divisa in AvailableCurrencies.Where(d => !favCodes.Contains(d.Code)))
            DivisasBusqueda.Add(divisa);
    }
    
    [RelayCommand]
    private async Task AgregarAFavoritasAsync(CurrencyModel? divisa)
    {
        if (divisa == null) return;
        
        if (FavoriteCurrencies.Any(f => f.Code == divisa.Code))
        {
            MostrarSelectorDivisas = false;
            return;
        }
        
        try
        {
            if (_idLocalActual > 0)
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();
                
                var sqlOrden = "SELECT COALESCE(MAX(orden), 0) + 1 FROM divisas_favoritas_local WHERE id_local = @idLocal";
                await using var cmdOrden = new NpgsqlCommand(sqlOrden, conn);
                cmdOrden.Parameters.AddWithValue("idLocal", _idLocalActual);
                var orden = Convert.ToInt32(await cmdOrden.ExecuteScalarAsync());
                
                var sql = @"INSERT INTO divisas_favoritas_local (id_local, codigo_divisa, orden) 
                            VALUES (@idLocal, @codigo, @orden) ON CONFLICT DO NOTHING";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("idLocal", _idLocalActual);
                cmd.Parameters.AddWithValue("codigo", divisa.Code);
                cmd.Parameters.AddWithValue("orden", orden);
                await cmd.ExecuteNonQueryAsync();
            }
            
            var fav = new FavoriteCurrencyModel
            {
                Code = divisa.Code,
                Name = divisa.Name,
                Country = divisa.Country,
                RateToEur = divisa.RateToEur,
                RateWithMargin = divisa.RateWithMargin
            };
            FavoriteCurrencies.Add(fav);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al agregar divisa: {ex.Message}";
        }
        
        MostrarSelectorDivisas = false;
    }
    
    [RelayCommand]
    private async Task QuitarDeFavoritasAsync(FavoriteCurrencyModel? divisa)
    {
        if (divisa == null) return;
        
        try
        {
            if (_idLocalActual > 0)
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();
                
                var sql = "DELETE FROM divisas_favoritas_local WHERE id_local = @idLocal AND codigo_divisa = @codigo";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("idLocal", _idLocalActual);
                cmd.Parameters.AddWithValue("codigo", divisa.Code);
                await cmd.ExecuteNonQueryAsync();
            }
            
            FavoriteCurrencies.Remove(divisa);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al quitar divisa: {ex.Message}";
        }
    }
    partial void OnBusquedaDivisaChanged(string value)
    {
        if (MostrarSelectorDivisas)
        {
            var favCodes = FavoriteCurrencies.Select(f => f.Code).ToHashSet();
            DivisasBusqueda.Clear();
            
            IEnumerable<CurrencyModel> resultados;
            if (string.IsNullOrWhiteSpace(value))
                resultados = AvailableCurrencies.Where(d => !favCodes.Contains(d.Code));
            else
            {
                var filtro = value.ToLower();
                resultados = AvailableCurrencies.Where(d =>
                    !favCodes.Contains(d.Code) &&
                    (d.Code.ToLower().Contains(filtro) ||
                     d.Name.ToLower().Contains(filtro) ||
                     d.Country.ToLower().Contains(filtro)));
            }

            foreach (var divisa in resultados.Take(15))
                DivisasBusqueda.Add(divisa);
        }
    }
    
    partial void OnBusquedaDivisaCalculadoraChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var filtro = value.ToLower().Trim();
        var divisaEncontrada = AvailableCurrencies.FirstOrDefault(d =>
            d.Code.ToLower() == filtro ||
            d.Code.ToLower().StartsWith(filtro) ||
            d.Name.ToLower().Contains(filtro));
        if (divisaEncontrada != null)
            SelectedCurrency = divisaEncontrada;
    }

    [RelayCommand]
    private void SeleccionarDivisaFavorita(FavoriteCurrencyModel? divisa)
    {
        if (divisa == null) return;
        var currency = AvailableCurrencies.FirstOrDefault(c => c.Code == divisa.Code);
        if (currency != null)
        {
            SelectedCurrency = currency;
            BusquedaDivisaCalculadora = $"{currency.Code} | {currency.Name}";
            CurrentRate = currency.RateWithMargin;
            RateDisplayText = $"1 {currency.Code} = {currency.RateWithMargin:N2} EUR";
            CalcularDesdeDivisa();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CALCULADORA BIDIRECCIONAL
    // ═══════════════════════════════════════════════════════════

    partial void OnSelectedCurrencyChanged(CurrencyModel? value)
    {
        if (value != null)
        {
            CurrentRate = value.RateWithMargin;
            RateDisplayText = $"1 {value.Code} = {value.RateWithMargin:N4} EUR";
            BusquedaDivisaCalculadora = $"{value.Code} | {value.Name}";
            CalcularDesdeDivisa();
        }
    }

    partial void OnAmountReceivedChanged(string value)
    {
        if (!_calculandoDesdeEuros)
        {
            CalcularDesdeDivisa();
        }
    }
    
    partial void OnTotalInEurosTextoChanged(string value)
    {
        if (!_calculandoDesdeDivisa)
        {
            CalcularDesdeEuros();
        }
    }

    /// <summary>
    /// Calcula euros a partir de la cantidad de divisa ingresada
    /// </summary>
    private void CalcularDesdeDivisa()
    {
        if (_calculandoDesdeEuros) return;
        
        _calculandoDesdeDivisa = true;
        
        try
        {
            if (SelectedCurrency == null || string.IsNullOrWhiteSpace(AmountReceived))
            {
                TotalInEuros = 0;
                TotalInEurosTexto = "0,00";
                ValidarOperacion();
                return;
            }

            var textoLimpio = AmountReceived.Replace(",", ".");
            if (!decimal.TryParse(textoLimpio, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidad))
            {
                TotalInEuros = 0;
                TotalInEurosTexto = "0,00";
                ValidarOperacion();
                return;
            }

            if (cantidad <= 0)
            {
                TotalInEuros = 0;
                TotalInEurosTexto = "0,00";
                ValidarOperacion();
                return;
            }

            TotalInEuros = cantidad * SelectedCurrency.RateWithMargin;
            TotalInEurosTexto = TotalInEuros.ToString("N2", new CultureInfo("es-ES"));
            ValidarOperacion();
        }
        finally
        {
            _calculandoDesdeDivisa = false;
        }
    }
    
    /// <summary>
    /// Calcula cantidad de divisa a partir de los euros ingresados
    /// </summary>
    private void CalcularDesdeEuros()
    {
        if (_calculandoDesdeDivisa) return;
        
        _calculandoDesdeEuros = true;
        
        try
        {
            if (SelectedCurrency == null || string.IsNullOrWhiteSpace(TotalInEurosTexto))
            {
                AmountReceived = "0.00";
                TotalInEuros = 0;
                ValidarOperacion();
                return;
            }

            var textoLimpio = TotalInEurosTexto.Replace(",", ".");
            if (!decimal.TryParse(textoLimpio, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal euros))
            {
                AmountReceived = "0.00";
                TotalInEuros = 0;
                ValidarOperacion();
                return;
            }

            if (euros <= 0)
            {
                AmountReceived = "0.00";
                TotalInEuros = 0;
                ValidarOperacion();
                return;
            }

            TotalInEuros = euros;
            
            // Calcular divisa: euros / tasa = cantidad divisa
            if (SelectedCurrency.RateWithMargin > 0)
            {
                decimal cantidadDivisa = euros / SelectedCurrency.RateWithMargin;
                AmountReceived = cantidadDivisa.ToString("N2", CultureInfo.InvariantCulture);
            }
            
            ValidarOperacion();
        }
        finally
        {
            _calculandoDesdeEuros = false;
        }
    }

    private void ValidarOperacion()
    {
        var cantidadValida = decimal.TryParse(AmountReceived?.Replace(",", "."), 
            NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidad) && cantidad > 0;
        
        PuedeRealizarOperacion = ClienteSeleccionado != null && SelectedCurrency != null && cantidadValida;
    }

    // ═══════════════════════════════════════════════════════════
    // BÚSQUEDA DE CLIENTES - FILTRADO POR COMERCIO
    // ═══════════════════════════════════════════════════════════
    
    [RelayCommand]
    private async Task BuscarClientesManualAsync()
    {
        if (string.IsNullOrWhiteSpace(BusquedaCliente))
        {
            ErrorMessage = "Ingrese un termino de busqueda";
            return;
        }
        ErrorMessage = string.Empty;
        await BuscarClientesAsync(BusquedaCliente);
    }
    
    /// <summary>
    /// Busca clientes FILTRADOS POR COMERCIO
    /// Los clientes pertenecen al comercio, se comparten entre locales del mismo comercio
    /// </summary>
    private async Task BuscarClientesAsync(string? termino)
    {
        try
        {
            IsLoading = true;
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            string whereClause;
            bool usarTermino = !string.IsNullOrWhiteSpace(termino);
            
            if (usarTermino)
            {
                whereClause = TipoBusquedaSeleccionado switch
                {
                    "Nombre" => @"(nombre ILIKE @termino OR apellidos ILIKE @termino 
                                  OR COALESCE(segundo_nombre, '') ILIKE @termino 
                                  OR COALESCE(segundo_apellido, '') ILIKE @termino
                                  OR CONCAT(nombre, ' ', apellidos) ILIKE @termino
                                  OR CONCAT(nombre, ' ', COALESCE(segundo_nombre, ''), ' ', apellidos, ' ', COALESCE(segundo_apellido, '')) ILIKE @termino)",
                    "Documento" => "documento_numero ILIKE @termino",
                    "Telefono" => "telefono ILIKE @termino",
                    _ => @"(documento_numero ILIKE @termino OR telefono ILIKE @termino OR nombre ILIKE @termino 
                           OR apellidos ILIKE @termino OR COALESCE(segundo_nombre, '') ILIKE @termino
                           OR COALESCE(segundo_apellido, '') ILIKE @termino
                           OR CONCAT(nombre, ' ', apellidos) ILIKE @termino)"
                };
            }
            else
            {
                whereClause = "1=1";
            }

            // CAMBIO PRINCIPAL: Filtrar por COMERCIO en lugar de local
            // Los clientes son unicos por comercio, compartidos entre locales del mismo comercio
            var comercioFilter = _idComercioActual > 0 
                ? " AND id_comercio_registro = @idComercio" 
                : "";

            var sql = $@"SELECT DISTINCT id_cliente, nombre, apellidos, telefono, direccion,
                               COALESCE(nacionalidad, '') as nacionalidad, 
                               COALESCE(documento_tipo, 'DNI') as documento_tipo, 
                               COALESCE(documento_numero, '') as documento_numero,
                               caducidad_documento, fecha_nacimiento,
                               segundo_nombre, segundo_apellido,
                               imagen_documento_frontal, imagen_documento_trasera
                        FROM clientes
                        WHERE activo = true AND {whereClause}{comercioFilter}
                        ORDER BY nombre, apellidos
                        LIMIT 50";

            await using var cmd = new NpgsqlCommand(sql, conn);
            if (usarTermino)
                cmd.Parameters.AddWithValue("termino", $"%{termino}%");
            if (_idComercioActual > 0)
                cmd.Parameters.AddWithValue("idComercio", _idComercioActual);

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
                    CaducidadDocumento = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                    FechaNacimiento = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    SegundoNombre = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    SegundoApellido = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    ImagenDocumentoFrontal = reader.IsDBNull(12) ? null : (byte[])reader[12],
                    ImagenDocumentoTrasera = reader.IsDBNull(13) ? null : (byte[])reader[13]
                };
                
                ClientesEncontrados.Add(cliente);
            }

            if (!ClientesEncontrados.Any())
                ErrorMessage = usarTermino ? "No se encontraron clientes con ese criterio en este comercio" : "No hay clientes registrados en este comercio";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error en busqueda: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SeleccionarClienteYContinuarAsync(ClienteModel? cliente)
    {
        if (cliente == null) return;
        ClienteSeleccionado = cliente;
        ValidarOperacion();
        
        // Cargar acumulados del cliente
        await CargarAcumuladosClienteAsync(cliente.IdCliente);
        
        OcultarTodasLasVistas();
        VistaTransaccion = true;
    }

    // ═══════════════════════════════════════════════════════════
    // FORMULARIO NUEVO CLIENTE
    // ═══════════════════════════════════════════════════════════

    private void LimpiarFormularioCliente()
    {
        _idClienteEditando = 0;
        NuevoNombre = string.Empty;
        NuevoSegundoNombre = string.Empty;
        NuevoApellido = string.Empty;
        NuevoSegundoApellido = string.Empty;
        NuevoTelefono = string.Empty;
        NuevaDireccion = string.Empty;
        NuevaNacionalidad = string.Empty;
        NuevoTipoDocumento = "DNI";
        NuevoNumeroDocumento = string.Empty;
        NuevaCaducidadDocumentoTexto = string.Empty;
        NuevaFechaNacimientoTexto = string.Empty;
        NuevaImagenDocumentoFrontal = null;
        NuevaImagenDocumentoTrasera = null;
        TieneImagenDocumentoFrontal = false;
        TieneImagenDocumentoTrasera = false;
        OnPropertyChanged(nameof(TieneAlgunaImagenDocumento));
        ImagenDocumentoFrontalPreview = null;
        ImagenDocumentoTraseraPreview = null;
    }

    /// <summary>
    /// Guarda nuevo cliente ASOCIADO AL COMERCIO
    /// Se registra id_comercio_registro, id_local_registro e id_usuario_registro
    /// </summary>
    [RelayCommand]
    private async Task GuardarNuevoClienteAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            DateTime? fechaNacimiento = null;
            DateTime? fechaCaducidad = null;

            if (!string.IsNullOrWhiteSpace(NuevaFechaNacimientoTexto))
            {
                if (DateTime.TryParseExact(NuevaFechaNacimientoTexto, "dd/MM/yyyy", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var fn))
                    fechaNacimiento = fn;
            }

            if (!string.IsNullOrWhiteSpace(NuevaCaducidadDocumentoTexto))
            {
                if (DateTime.TryParseExact(NuevaCaducidadDocumentoTexto, "dd/MM/yyyy", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var fc))
                    fechaCaducidad = fc;
            }

            // CAMBIO: Agregar id_comercio_registro e id_usuario_registro
            var sql = @"INSERT INTO clientes 
                        (nombre, segundo_nombre, apellidos, segundo_apellido, 
                         telefono, direccion, nacionalidad,
                         documento_tipo, documento_numero, caducidad_documento,
                         fecha_nacimiento, imagen_documento_frontal, imagen_documento_trasera,
                         id_comercio_registro, id_local_registro, id_usuario_registro, activo)
                        VALUES 
                        (@nombre, @segundoNombre, @apellido, @segundoApellido,
                         @telefono, @direccion, @nacionalidad,
                         @tipoDoc, @numDoc, @caducidad, @fechaNac, 
                         @imagenFrontal, @imagenTrasera,
                         @idComercio, @idLocal, @idUsuario, true)
                        RETURNING id_cliente";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("nombre", NuevoNombre);
            cmd.Parameters.AddWithValue("segundoNombre", NuevoSegundoNombre ?? "");
            cmd.Parameters.AddWithValue("apellido", NuevoApellido ?? "");
            cmd.Parameters.AddWithValue("segundoApellido", NuevoSegundoApellido ?? "");
            cmd.Parameters.AddWithValue("telefono", NuevoTelefono ?? "");
            cmd.Parameters.AddWithValue("direccion", NuevaDireccion ?? "");
            cmd.Parameters.AddWithValue("nacionalidad", NuevaNacionalidad ?? "");
            cmd.Parameters.AddWithValue("tipoDoc", NuevoTipoDocumento);
            cmd.Parameters.AddWithValue("numDoc", NuevoNumeroDocumento);
            cmd.Parameters.AddWithValue("caducidad", (object?)fechaCaducidad ?? DBNull.Value);
            cmd.Parameters.AddWithValue("fechaNac", (object?)fechaNacimiento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("imagenFrontal", (object?)NuevaImagenDocumentoFrontal ?? DBNull.Value);
            cmd.Parameters.AddWithValue("imagenTrasera", (object?)NuevaImagenDocumentoTrasera ?? DBNull.Value);
            // NUEVOS CAMPOS: comercio, local y usuario
            cmd.Parameters.AddWithValue("idComercio", _idComercioActual > 0 ? _idComercioActual : DBNull.Value);
            cmd.Parameters.AddWithValue("idLocal", _idLocalActual > 0 ? _idLocalActual : DBNull.Value);
            cmd.Parameters.AddWithValue("idUsuario", _idUsuarioActual > 0 ? _idUsuarioActual : DBNull.Value);

            var idCliente = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            var nuevoCliente = new ClienteModel
            {
                IdCliente = idCliente,
                Nombre = NuevoNombre,
                SegundoNombre = NuevoSegundoNombre ?? "",
                Apellido = NuevoApellido ?? "",
                SegundoApellido = NuevoSegundoApellido ?? "",
                Telefono = NuevoTelefono ?? "",
                Direccion = NuevaDireccion ?? "",
                Nacionalidad = NuevaNacionalidad ?? "",
                TipoDocumento = NuevoTipoDocumento,
                NumeroDocumento = NuevoNumeroDocumento,
                CaducidadDocumento = fechaCaducidad,
                FechaNacimiento = fechaNacimiento,
                ImagenDocumentoFrontal = NuevaImagenDocumentoFrontal,
                ImagenDocumentoTrasera = NuevaImagenDocumentoTrasera,
                IdComercioRegistro = _idComercioActual > 0 ? _idComercioActual : null,
                IdLocalRegistro = _idLocalActual > 0 ? _idLocalActual : null,
                IdUsuarioRegistro = _idUsuarioActual > 0 ? _idUsuarioActual : null
            };

            ClienteSeleccionado = nuevoCliente;
            LimpiarFormularioCliente();
            
            OcultarTodasLasVistas();
            VistaPrincipal = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al crear cliente: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Guarda los cambios del cliente editado y vuelve a la transaccion
    /// </summary>
    [RelayCommand]
    private async Task GuardarEdicionClienteAsync()
    {
        if (_idClienteEditando <= 0)
        {
            ErrorMessage = "No hay cliente para editar";
            return;
        }
        
        // Validaciones
        if (string.IsNullOrWhiteSpace(NuevoNombre))
        {
            ErrorMessage = "El primer nombre es obligatorio";
            return;
        }
        if (string.IsNullOrWhiteSpace(NuevoApellido))
        {
            ErrorMessage = "El primer apellido es obligatorio";
            return;
        }
        if (string.IsNullOrWhiteSpace(NuevoNumeroDocumento))
        {
            ErrorMessage = "El numero de documento es obligatorio";
            return;
        }
        
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            DateTime? fechaNacimiento = null;
            DateTime? fechaCaducidad = null;

            if (!string.IsNullOrWhiteSpace(NuevaFechaNacimientoTexto))
            {
                if (DateTime.TryParseExact(NuevaFechaNacimientoTexto, "dd/MM/yyyy", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var fn))
                    fechaNacimiento = fn;
            }

            if (!string.IsNullOrWhiteSpace(NuevaCaducidadDocumentoTexto))
            {
                if (DateTime.TryParseExact(NuevaCaducidadDocumentoTexto, "dd/MM/yyyy", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var fc))
                    fechaCaducidad = fc;
            }

            var sql = @"UPDATE clientes SET
                        nombre = @nombre,
                        segundo_nombre = @segundoNombre,
                        apellidos = @apellido,
                        segundo_apellido = @segundoApellido,
                        telefono = @telefono,
                        direccion = @direccion,
                        nacionalidad = @nacionalidad,
                        documento_tipo = @tipoDoc,
                        documento_numero = @numDoc,
                        caducidad_documento = @caducidad,
                        fecha_nacimiento = @fechaNac,
                        imagen_documento_frontal = @imagenFrontal,
                        imagen_documento_trasera = @imagenTrasera
                        WHERE id_cliente = @idCliente";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("idCliente", _idClienteEditando);
            cmd.Parameters.AddWithValue("nombre", NuevoNombre);
            cmd.Parameters.AddWithValue("segundoNombre", NuevoSegundoNombre ?? "");
            cmd.Parameters.AddWithValue("apellido", NuevoApellido ?? "");
            cmd.Parameters.AddWithValue("segundoApellido", NuevoSegundoApellido ?? "");
            cmd.Parameters.AddWithValue("telefono", NuevoTelefono ?? "");
            cmd.Parameters.AddWithValue("direccion", NuevaDireccion ?? "");
            cmd.Parameters.AddWithValue("nacionalidad", NuevaNacionalidad ?? "");
            cmd.Parameters.AddWithValue("tipoDoc", NuevoTipoDocumento);
            cmd.Parameters.AddWithValue("numDoc", NuevoNumeroDocumento);
            cmd.Parameters.AddWithValue("caducidad", (object?)fechaCaducidad ?? DBNull.Value);
            cmd.Parameters.AddWithValue("fechaNac", (object?)fechaNacimiento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("imagenFrontal", (object?)NuevaImagenDocumentoFrontal ?? DBNull.Value);
            cmd.Parameters.AddWithValue("imagenTrasera", (object?)NuevaImagenDocumentoTrasera ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();

            // Crear NUEVO objeto para forzar notificación de cambios en la vista
            var clienteActualizado = new ClienteModel
            {
                IdCliente = _idClienteEditando,
                Nombre = NuevoNombre,
                SegundoNombre = NuevoSegundoNombre ?? "",
                Apellido = NuevoApellido ?? "",
                SegundoApellido = NuevoSegundoApellido ?? "",
                Telefono = NuevoTelefono ?? "",
                Direccion = NuevaDireccion ?? "",
                Nacionalidad = NuevaNacionalidad ?? "",
                TipoDocumento = NuevoTipoDocumento,
                NumeroDocumento = NuevoNumeroDocumento,
                CaducidadDocumento = fechaCaducidad,
                FechaNacimiento = fechaNacimiento,
                ImagenDocumentoFrontal = NuevaImagenDocumentoFrontal,
                ImagenDocumentoTrasera = NuevaImagenDocumentoTrasera,
                IdComercioRegistro = ClienteSeleccionado?.IdComercioRegistro,
                IdLocalRegistro = ClienteSeleccionado?.IdLocalRegistro,
                IdUsuarioRegistro = ClienteSeleccionado?.IdUsuarioRegistro
            };
            
            // Asignar nuevo objeto para que la vista detecte el cambio
            ClienteSeleccionado = clienteActualizado;
            
            // Volver a la transaccion
            OcultarTodasLasVistas();
            VistaTransaccion = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al actualizar cliente: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // IMÁGENES DE DOCUMENTO
    // ═══════════════════════════════════════════════════════════
    
    public void SetImagenDocumentoFrontal(byte[] imagen)
    {
        NuevaImagenDocumentoFrontal = imagen;
        TieneImagenDocumentoFrontal = true;
        OnPropertyChanged(nameof(TieneAlgunaImagenDocumento));
        try
        {
            using var stream = new MemoryStream(imagen);
            ImagenDocumentoFrontalPreview = new Avalonia.Media.Imaging.Bitmap(stream);
        }
        catch { ImagenDocumentoFrontalPreview = null; }
    }
    
    public void SetImagenDocumentoTrasera(byte[] imagen)
    {
        NuevaImagenDocumentoTrasera = imagen;
        TieneImagenDocumentoTrasera = true;
        OnPropertyChanged(nameof(TieneAlgunaImagenDocumento));
        try
        {
            using var stream = new MemoryStream(imagen);
            ImagenDocumentoTraseraPreview = new Avalonia.Media.Imaging.Bitmap(stream);
        }
        catch { ImagenDocumentoTraseraPreview = null; }
    }
    
    [RelayCommand]
    private void QuitarImagenFrontal()
    {
        NuevaImagenDocumentoFrontal = null;
        TieneImagenDocumentoFrontal = false;
        OnPropertyChanged(nameof(TieneAlgunaImagenDocumento));
        ImagenDocumentoFrontalPreview = null;
    }
    
    [RelayCommand]
    private void QuitarImagenTrasera()
    {
        NuevaImagenDocumentoTrasera = null;
        TieneImagenDocumentoTrasera = false;
        OnPropertyChanged(nameof(TieneAlgunaImagenDocumento));
        ImagenDocumentoTraseraPreview = null;
    }

    [RelayCommand]
    private void MostrarPreviewImagenFrontal()
    {
        if (ImagenDocumentoFrontalPreview != null)
        {
            ImagenPreviewActual = ImagenDocumentoFrontalPreview;
            TituloPreviewImagen = "Documento - Cara Frontal";
            MostrarPreviewImagen = true;
        }
    }

    [RelayCommand]
    private void MostrarPreviewImagenTrasera()
    {
        if (ImagenDocumentoTraseraPreview != null)
        {
            ImagenPreviewActual = ImagenDocumentoTraseraPreview;
            TituloPreviewImagen = "Documento - Cara Trasera";
            MostrarPreviewImagen = true;
        }
    }

    [RelayCommand]
    private void CerrarPreviewImagen()
    {
        MostrarPreviewImagen = false;
        ImagenPreviewActual = null;
    }

    // ═══════════════════════════════════════════════════════════
    // TRANSACCIÓN - SOLO MUESTRA RESUMEN (NO GUARDA AÚN)
    // ═══════════════════════════════════════════════════════════
    
    [RelayCommand]
    private async Task RealizarTransaccionAsync()
    {
        if (!PuedeRealizarOperacion)
        {
            ErrorMessage = "No se puede realizar la operacion. Verifique los datos.";
            return;
        }
        
        IsLoading = true;
        ErrorMessage = string.Empty;
        
        try
        {
            // Solo preparar datos para mostrar en el resumen
            // NO guardar en base de datos todavía
            
            FechaOperacion = ObtenerHoraEspana();
            _operacionGuardada = false;
            
            // Generar número de operación para mostrar (pero sin guardarlo aún)
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            int idLocalParaOperacion = _idLocalActual;
            
            if (idLocalParaOperacion <= 0)
            {
                var sqlPrimerLocal = "SELECT id_local FROM locales WHERE activo = true ORDER BY id_local LIMIT 1";
                await using var cmdPrimer = new NpgsqlCommand(sqlPrimerLocal, conn);
                var result = await cmdPrimer.ExecuteScalarAsync();
                if (result != null)
                    idLocalParaOperacion = Convert.ToInt32(result);
            }
            
            // Obtener el siguiente número sin incrementar (solo para mostrar)
            var sqlVerificar = @"SELECT COALESCE(ultimo_correlativo, 0) + 1 FROM correlativos_operaciones 
                                WHERE id_local = @idLocal AND prefijo = 'DI'";
            await using var cmdVerificar = new NpgsqlCommand(sqlVerificar, conn);
            cmdVerificar.Parameters.AddWithValue("idLocal", idLocalParaOperacion);
            var siguiente = await cmdVerificar.ExecuteScalarAsync();
            int numeroSiguiente = siguiente != null && siguiente != DBNull.Value ? Convert.ToInt32(siguiente) : 1;
            
            NumeroOperacionDisplay = $"DI{numeroSiguiente:D4}";
            
            // Mostrar resumen sin guardar
            OcultarTodasLasVistas();
            VistaResumen = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al preparar transaccion: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Guarda la operación en la base de datos (llamado desde Finalizar)
    /// </summary>
    private async Task<bool> GuardarOperacionEnBDAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            var textoLimpio = AmountReceived.Replace(",", ".");
            decimal.TryParse(textoLimpio, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidadOrigen);
            
            // Obtener datos del local y comercio
            int idComercioParaOperacion = _idComercioActual;
            int idLocalParaOperacion = _idLocalActual;
            string codigoLocal = "SIN-LOCAL";
            
            // Si tenemos local, obtener su comercio y código
            if (idLocalParaOperacion > 0)
            {
                var sqlLocal = "SELECT id_comercio, codigo_local FROM locales WHERE id_local = @idLocal";
                await using var cmdLocal = new NpgsqlCommand(sqlLocal, conn);
                cmdLocal.Parameters.AddWithValue("idLocal", idLocalParaOperacion);
                await using var reader = await cmdLocal.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    idComercioParaOperacion = reader.GetInt32(0);
                    codigoLocal = reader.GetString(1);
                }
                await reader.CloseAsync();
            }
            
            // Si no tenemos local o comercio válido, obtener el primero disponible de la BD
            if (idLocalParaOperacion <= 0 || idComercioParaOperacion <= 0)
            {
                var sqlPrimerLocal = @"SELECT l.id_local, l.id_comercio, l.codigo_local 
                                       FROM locales l 
                                       WHERE l.activo = true 
                                       ORDER BY l.id_local 
                                       LIMIT 1";
                await using var cmdPrimer = new NpgsqlCommand(sqlPrimerLocal, conn);
                await using var readerPrimer = await cmdPrimer.ExecuteReaderAsync();
                if (await readerPrimer.ReadAsync())
                {
                    idLocalParaOperacion = readerPrimer.GetInt32(0);
                    idComercioParaOperacion = readerPrimer.GetInt32(1);
                    codigoLocal = readerPrimer.GetString(2);
                }
                await readerPrimer.CloseAsync();
            }
            
            // Verificar que tenemos datos válidos
            if (idLocalParaOperacion <= 0 || idComercioParaOperacion <= 0)
            {
                ErrorMessage = "Error: No se encontro un local/comercio valido en el sistema.";
                return false;
            }
            
            // Datos del usuario - obtener el primero si no hay sesión
            int idUsuarioParaOperacion = _idUsuarioActual;
            string nombreUsuario = _nombreUsuarioActual ?? "";
            string numeroUsuario = _numeroUsuarioActual ?? "";
            
            if (idUsuarioParaOperacion <= 0)
            {
                var sqlPrimerUsuario = @"SELECT id_usuario, 
                                         COALESCE(nombre, '') || ' ' || COALESCE(apellidos, '') as nombre_completo,
                                         COALESCE(numero_usuario, 'USR001') as numero
                                         FROM usuarios 
                                         WHERE activo = true 
                                         ORDER BY id_usuario 
                                         LIMIT 1";
                await using var cmdUsuario = new NpgsqlCommand(sqlPrimerUsuario, conn);
                await using var readerUsuario = await cmdUsuario.ExecuteReaderAsync();
                if (await readerUsuario.ReadAsync())
                {
                    idUsuarioParaOperacion = readerUsuario.GetInt32(0);
                    nombreUsuario = readerUsuario.GetString(1).Trim();
                    numeroUsuario = readerUsuario.GetString(2);
                }
                await readerUsuario.CloseAsync();
            }
            
            if (idUsuarioParaOperacion <= 0)
            {
                ErrorMessage = "Error: No se encontro un usuario valido en el sistema.";
                return false;
            }
            
            if (string.IsNullOrEmpty(nombreUsuario))
                nombreUsuario = "Usuario Sistema";
            if (string.IsNullOrEmpty(numeroUsuario))
                numeroUsuario = "SYS001";
            
            // Nombre del cliente
            string nombreCliente = ClienteSeleccionado?.NombreCompleto ?? "Cliente";
            if (string.IsNullOrEmpty(nombreCliente))
                nombreCliente = $"{ClienteSeleccionado?.Nombre} {ClienteSeleccionado?.Apellido}".Trim();
            if (string.IsNullOrEmpty(nombreCliente))
                nombreCliente = "Cliente sin nombre";
            
            // Generar número de operación único: DI + secuencial
            var numeroOperacionGenerado = await ObtenerSiguienteNumeroOperacionAsync(conn, idLocalParaOperacion, "DI");
            var horaOperacion = ObtenerHoraEspana();
            
            var sqlOperacion = @"INSERT INTO operaciones 
                                (numero_operacion, id_comercio, id_local, codigo_local, 
                                 id_usuario, nombre_usuario, numero_usuario,
                                 id_cliente, nombre_cliente, modulo, tipo_operacion,
                                 importe_total, estado, fecha_operacion, hora_operacion, observaciones)
                                VALUES 
                                (@numOp, @idComercio, @idLocal, @codigoLocal,
                                 @idUsuario, @nombreUsuario, @numeroUsuario,
                                 @idCliente, @nombreCliente, 'DIVISAS', 'COMPRA',
                                 @importe, 'COMPLETADA', @fecha, @hora, @obs)
                                RETURNING id_operacion";
            
            await using var cmdOp = new NpgsqlCommand(sqlOperacion, conn);
            cmdOp.Parameters.AddWithValue("numOp", numeroOperacionGenerado);
            cmdOp.Parameters.AddWithValue("idComercio", idComercioParaOperacion);
            cmdOp.Parameters.AddWithValue("idLocal", idLocalParaOperacion);
            cmdOp.Parameters.AddWithValue("codigoLocal", codigoLocal);
            cmdOp.Parameters.AddWithValue("idUsuario", idUsuarioParaOperacion);
            cmdOp.Parameters.AddWithValue("nombreUsuario", nombreUsuario);
            cmdOp.Parameters.AddWithValue("numeroUsuario", numeroUsuario);
            cmdOp.Parameters.AddWithValue("idCliente", ClienteSeleccionado!.IdCliente);
            cmdOp.Parameters.AddWithValue("nombreCliente", nombreCliente);
            cmdOp.Parameters.AddWithValue("importe", TotalInEuros);
            cmdOp.Parameters.AddWithValue("fecha", horaOperacion);
            cmdOp.Parameters.AddWithValue("hora", horaOperacion.TimeOfDay);
            cmdOp.Parameters.AddWithValue("obs", $"Compra de {cantidadOrigen} {SelectedCurrency!.Code}");
            
            NumeroOperacion = Convert.ToInt64(await cmdOp.ExecuteScalarAsync());
            
            var sqlDetalle = @"INSERT INTO operaciones_divisas 
                              (id_operacion, divisa_origen, divisa_destino, 
                               cantidad_origen, cantidad_destino, tipo_cambio, tipo_cambio_aplicado)
                              VALUES 
                              (@idOp, @divisaOrigen, 'EUR', @cantOrigen, @cantDestino, @tasa, @tasaAplicada)";
            
            await using var cmdDet = new NpgsqlCommand(sqlDetalle, conn);
            cmdDet.Parameters.AddWithValue("idOp", NumeroOperacion);
            cmdDet.Parameters.AddWithValue("divisaOrigen", SelectedCurrency.Code);
            cmdDet.Parameters.AddWithValue("cantOrigen", cantidadOrigen);
            cmdDet.Parameters.AddWithValue("cantDestino", TotalInEuros);
            cmdDet.Parameters.AddWithValue("tasa", SelectedCurrency.RateToEur);
            cmdDet.Parameters.AddWithValue("tasaAplicada", SelectedCurrency.RateWithMargin);
            
            await cmdDet.ExecuteNonQueryAsync();
            
            // Registrar las divisas recibidas por el local/comercio (balance de divisas)
            var sqlBalance = @"INSERT INTO balance_divisas 
                              (id_comercio, id_local, id_usuario, id_operacion,
                               codigo_divisa, nombre_divisa,
                               cantidad_recibida, cantidad_entregada_eur,
                               tasa_cambio_momento, tasa_cambio_aplicada,
                               tipo_movimiento, observaciones, fecha_registro)
                              VALUES 
                              (@idComercio, @idLocal, @idUsuario, @idOperacion,
                               @codigoDivisa, @nombreDivisa,
                               @cantidadRecibida, @cantidadEntregada,
                               @tasaMomento, @tasaAplicada,
                               'ENTRADA', @obs, @fechaRegistro)";
            
            await using var cmdBalance = new NpgsqlCommand(sqlBalance, conn);
            cmdBalance.Parameters.AddWithValue("idComercio", idComercioParaOperacion);
            cmdBalance.Parameters.AddWithValue("idLocal", idLocalParaOperacion);
            cmdBalance.Parameters.AddWithValue("idUsuario", idUsuarioParaOperacion);
            cmdBalance.Parameters.AddWithValue("idOperacion", NumeroOperacion);
            cmdBalance.Parameters.AddWithValue("codigoDivisa", SelectedCurrency.Code);
            cmdBalance.Parameters.AddWithValue("nombreDivisa", SelectedCurrency.Name);
            cmdBalance.Parameters.AddWithValue("cantidadRecibida", cantidadOrigen);
            cmdBalance.Parameters.AddWithValue("cantidadEntregada", TotalInEuros);
            cmdBalance.Parameters.AddWithValue("tasaMomento", SelectedCurrency.RateToEur);
            cmdBalance.Parameters.AddWithValue("tasaAplicada", SelectedCurrency.RateWithMargin);
            cmdBalance.Parameters.AddWithValue("obs", $"Compra de {cantidadOrigen} {SelectedCurrency.Code} - Cliente: {nombreCliente}");
            cmdBalance.Parameters.AddWithValue("fechaRegistro", horaOperacion);
            
            await cmdBalance.ExecuteNonQueryAsync();
            
            // Actualizar el número mostrado con el real
            NumeroOperacionDisplay = numeroOperacionGenerado;
            FechaOperacion = horaOperacion;
            _operacionGuardada = true;
            
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al guardar transaccion: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Obtiene el siguiente número de operación correlativo para un local y tipo.
    /// </summary>
    private async Task<string> ObtenerSiguienteNumeroOperacionAsync(NpgsqlConnection conn, int idLocal, string prefijo)
    {
        await using var transaction = await conn.BeginTransactionAsync();
        
        try
        {
            var sqlVerificar = @"SELECT ultimo_correlativo FROM correlativos_operaciones 
                                WHERE id_local = @idLocal AND prefijo = @prefijo
                                FOR UPDATE";
            
            await using var cmdVerificar = new NpgsqlCommand(sqlVerificar, conn, transaction);
            cmdVerificar.Parameters.AddWithValue("idLocal", idLocal);
            cmdVerificar.Parameters.AddWithValue("prefijo", prefijo);
            
            var resultado = await cmdVerificar.ExecuteScalarAsync();
            
            int nuevoCorrelativo;
            var horaEspana = ObtenerHoraEspana();
            
            if (resultado == null)
            {
                nuevoCorrelativo = 1;
                var sqlInsertar = @"INSERT INTO correlativos_operaciones (id_local, prefijo, ultimo_correlativo, fecha_ultimo_uso)
                                    VALUES (@idLocal, @prefijo, @correlativo, @fecha)";
                await using var cmdInsertar = new NpgsqlCommand(sqlInsertar, conn, transaction);
                cmdInsertar.Parameters.AddWithValue("idLocal", idLocal);
                cmdInsertar.Parameters.AddWithValue("prefijo", prefijo);
                cmdInsertar.Parameters.AddWithValue("correlativo", nuevoCorrelativo);
                cmdInsertar.Parameters.AddWithValue("fecha", horaEspana);
                await cmdInsertar.ExecuteNonQueryAsync();
            }
            else
            {
                nuevoCorrelativo = Convert.ToInt32(resultado) + 1;
                var sqlActualizar = @"UPDATE correlativos_operaciones 
                                    SET ultimo_correlativo = @correlativo, fecha_ultimo_uso = @fecha
                                    WHERE id_local = @idLocal AND prefijo = @prefijo";
                await using var cmdActualizar = new NpgsqlCommand(sqlActualizar, conn, transaction);
                cmdActualizar.Parameters.AddWithValue("idLocal", idLocal);
                cmdActualizar.Parameters.AddWithValue("prefijo", prefijo);
                cmdActualizar.Parameters.AddWithValue("correlativo", nuevoCorrelativo);
                cmdActualizar.Parameters.AddWithValue("fecha", horaEspana);
                await cmdActualizar.ExecuteNonQueryAsync();
            }
            
            await transaction.CommitAsync();
            return $"{prefijo}{nuevoCorrelativo:D4}";
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // IMPRESIÓN DE RECIBO
    // ═══════════════════════════════════════════════════════════
    
    [RelayCommand]
    private async Task ImprimirReciboAsync()
    {
        try
        {
            IsLoading = true;
            
            var pdfService = new ReciboDivisasPdfService();
            var pdfPath = await pdfService.GenerarReciboPdfAsync(
                numeroOperacion: NumeroOperacionDisplay,
                fechaOperacion: FechaOperacion,
                cliente: ClienteSeleccionado!,
                divisaOrigen: SelectedCurrency!.Code,
                nombreDivisa: SelectedCurrency.Name,
                cantidadRecibida: decimal.Parse(AmountReceived.Replace(",", "."), CultureInfo.InvariantCulture),
                totalEntregado: TotalInEuros,
                tasaCambio: SelectedCurrency.RateWithMargin,
                idLocal: _idLocalActual
            );
            
            if (!string.IsNullOrEmpty(pdfPath) && File.Exists(pdfPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al generar recibo: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // LISTA DE DIVISAS
    // ═══════════════════════════════════════════════════════════

    private List<CurrencyModel> GetAllCurrenciesList()
    {
        return new List<CurrencyModel>
        {
            new() { Code = "USD", Name = "Dolar USA", Country = "Estados Unidos" },
            new() { Code = "CAD", Name = "Dolar Canadiense", Country = "Canada" },
            new() { Code = "CHF", Name = "Franco Suizo", Country = "Suiza" },
            new() { Code = "GBP", Name = "Libra Esterlina", Country = "Reino Unido" },
            new() { Code = "CNY", Name = "Yuan Chino", Country = "China" },
            new() { Code = "JPY", Name = "Yen Japones", Country = "Japon" },
            new() { Code = "AUD", Name = "Dolar Australiano", Country = "Australia" },
            new() { Code = "MXN", Name = "Peso Mexicano", Country = "Mexico" },
            new() { Code = "BRL", Name = "Real Brasileno", Country = "Brasil" },
            new() { Code = "ARS", Name = "Peso Argentino", Country = "Argentina" },
            new() { Code = "CLP", Name = "Peso Chileno", Country = "Chile" },
            new() { Code = "COP", Name = "Peso Colombiano", Country = "Colombia" },
            new() { Code = "PEN", Name = "Sol Peruano", Country = "Peru" },
            new() { Code = "BOB", Name = "Boliviano", Country = "Bolivia" },
            new() { Code = "VES", Name = "Bolivar", Country = "Venezuela" },
            new() { Code = "DOP", Name = "Peso Dominicano", Country = "Rep. Dominicana" },
            new() { Code = "UYU", Name = "Peso Uruguayo", Country = "Uruguay" },
            new() { Code = "INR", Name = "Rupia India", Country = "India" },
            new() { Code = "KRW", Name = "Won Surcoreano", Country = "Corea del Sur" },
            new() { Code = "SGD", Name = "Dolar Singapur", Country = "Singapur" },
            new() { Code = "HKD", Name = "Dolar Hong Kong", Country = "Hong Kong" },
            new() { Code = "NZD", Name = "Dolar Neozelandes", Country = "Nueva Zelanda" },
            new() { Code = "SEK", Name = "Corona Sueca", Country = "Suecia" },
            new() { Code = "NOK", Name = "Corona Noruega", Country = "Noruega" },
            new() { Code = "DKK", Name = "Corona Danesa", Country = "Dinamarca" },
            new() { Code = "PLN", Name = "Zloty Polaco", Country = "Polonia" },
            new() { Code = "CZK", Name = "Corona Checa", Country = "Republica Checa" },
            new() { Code = "HUF", Name = "Florin Hungaro", Country = "Hungria" },
            new() { Code = "RON", Name = "Leu Rumano", Country = "Rumania" },
            new() { Code = "TRY", Name = "Lira Turca", Country = "Turquia" },
            new() { Code = "ZAR", Name = "Rand Sudafricano", Country = "Sudafrica" },
            new() { Code = "THB", Name = "Baht Tailandes", Country = "Tailandia" },
            new() { Code = "MYR", Name = "Ringgit Malayo", Country = "Malasia" },
            new() { Code = "PHP", Name = "Peso Filipino", Country = "Filipinas" },
            new() { Code = "IDR", Name = "Rupia Indonesia", Country = "Indonesia" },
            new() { Code = "AED", Name = "Dirham EAU", Country = "Emiratos Arabes" },
            new() { Code = "SAR", Name = "Riyal Saudi", Country = "Arabia Saudita" },
            new() { Code = "ILS", Name = "Shekel Israeli", Country = "Israel" },
            new() { Code = "EGP", Name = "Libra Egipcia", Country = "Egipto" },
            new() { Code = "MAD", Name = "Dirham Marroqui", Country = "Marruecos" }
        };
    }
}