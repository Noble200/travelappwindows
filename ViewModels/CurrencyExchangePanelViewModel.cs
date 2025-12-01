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

    // ═══════════════════════════════════════════════════════════
    // CLIENTE
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private ClienteModel? _clienteSeleccionado;
    [ObservableProperty] private ObservableCollection<ClienteModel> _clientesEncontrados = new();
    [ObservableProperty] private string _busquedaCliente = string.Empty;
    
    public ObservableCollection<string> TiposBusquedaCliente { get; } = new()
    {
        "Todos", "Nombre", "Documento", "Teléfono"
    };
    
    [ObservableProperty] private string _tipoBusquedaSeleccionado = "Todos";
    
    // ═══════════════════════════════════════════════════════════
    // CAMPOS NUEVO CLIENTE (con segundo nombre/apellido)
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
    [ObservableProperty] private DateTimeOffset? _nuevaCaducidadDocumento;
    [ObservableProperty] private DateTimeOffset? _nuevaFechaNacimiento;
    
    // Imágenes documento
    [ObservableProperty] private byte[]? _nuevaImagenDocumentoFrontal;
    [ObservableProperty] private byte[]? _nuevaImagenDocumentoTrasera;
    [ObservableProperty] private bool _tieneImagenDocumentoFrontal = false;
    [ObservableProperty] private bool _tieneImagenDocumentoTrasera = false;
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? _imagenDocumentoFrontalPreview;
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? _imagenDocumentoTraseraPreview;
    
    // Propiedades calculadas para confirmación
    public string NuevoNombreCompletoPreview
    {
        get
        {
            var partes = new[] { NuevoNombre, NuevoSegundoNombre, NuevoApellido, NuevoSegundoApellido };
            return string.Join(" ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }
    
    public string NuevaFechaNacimientoTexto => NuevaFechaNacimiento?.ToString("dd/MM/yyyy") ?? "-";
    public string NuevaCaducidadDocumentoTexto => NuevaCaducidadDocumento?.ToString("dd/MM/yyyy") ?? "-";
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
    // CALCULADORA
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private string _amountReceived = string.Empty;
    [ObservableProperty] private decimal _totalInEuros;
    [ObservableProperty] private decimal _currentRate;
    [ObservableProperty] private string _rateDisplayText = string.Empty;

    // ═══════════════════════════════════════════════════════════
    // OPERACIÓN / TRANSACCIÓN
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private long _numeroOperacion = 0;
    [ObservableProperty] private string _numeroOperacionDisplay = string.Empty;
    [ObservableProperty] private DateTime _fechaOperacion;

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

    private async void InitializeAsync()
    {
        await CargarMargenGananciaAsync();
        await LoadDataAsync();
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
    }
    
    [RelayCommand]
    private void IrABuscarCliente()
    {
        OcultarTodasLasVistas();
        ClientesEncontrados.Clear();
        BusquedaCliente = string.Empty;
        ErrorMessage = string.Empty;
        VistaBuscarCliente = true;
    }
    
    [RelayCommand]
    private void IrANuevoCliente()
    {
        OcultarTodasLasVistas();
        LimpiarFormularioCliente();
        ErrorMessage = string.Empty;
        VistaNuevoCliente = true;
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
            ErrorMessage = "El número de documento es obligatorio";
            return;
        }
        
        ErrorMessage = string.Empty;
        OcultarTodasLasVistas();
        OnPropertyChanged(nameof(NuevoNombreCompletoPreview));
        OnPropertyChanged(nameof(NuevaFechaNacimientoTexto));
        OnPropertyChanged(nameof(NuevaCaducidadDocumentoTexto));
        OnPropertyChanged(nameof(TieneImagenesDocumento));
        VistaConfirmacionCliente = true;
    }
    
    [RelayCommand]
    private void FinalizarYVolverAPrincipal()
    {
        ClienteSeleccionado = null;
        AmountReceived = string.Empty;
        TotalInEuros = 0;
        NumeroOperacion = 0;
        NumeroOperacionDisplay = string.Empty;
        
        OcultarTodasLasVistas();
        VistaPrincipal = true;
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
            var favorites = await _currencyService.GetFavoriteCurrenciesAsync(_favoriteCodes);
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
    private void AgregarAFavoritas(CurrencyModel? divisa)
    {
        if (divisa == null) return;
        if (!_favoriteCodes.Contains(divisa.Code))
        {
            _favoriteCodes.Add(divisa.Code);
            var fav = new FavoriteCurrencyModel
            {
                Code = divisa.Code,
                Name = divisa.Name,
                RateToEur = divisa.RateToEur,
                RateWithMargin = divisa.RateWithMargin
            };
            FavoriteCurrencies.Add(fav);
        }
        MostrarSelectorDivisas = false;
    }
    
    [RelayCommand]
    private void QuitarDeFavoritas(FavoriteCurrencyModel? divisa)
    {
        if (divisa == null) return;
        _favoriteCodes.Remove(divisa.Code);
        FavoriteCurrencies.Remove(divisa);
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
            RateDisplayText = $"1 {currency.Code} = {currency.RateWithMargin:N4} EUR";
            CalcularConversion();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CALCULADORA
    // ═══════════════════════════════════════════════════════════

    partial void OnSelectedCurrencyChanged(CurrencyModel? value)
    {
        if (value != null)
        {
            CurrentRate = value.RateWithMargin;
            RateDisplayText = $"1 {value.Code} = {value.RateWithMargin:N4} EUR";
            BusquedaDivisaCalculadora = $"{value.Code} | {value.Name}";
            CalcularConversion();
        }
    }

    partial void OnAmountReceivedChanged(string value) => CalcularConversion();

    private void CalcularConversion()
    {
        if (SelectedCurrency == null || string.IsNullOrWhiteSpace(AmountReceived))
        {
            TotalInEuros = 0;
            ValidarOperacion();
            return;
        }

        var textoLimpio = AmountReceived.Replace(",", ".");
        if (!decimal.TryParse(textoLimpio, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidad))
        {
            TotalInEuros = 0;
            ValidarOperacion();
            return;
        }

        if (cantidad <= 0)
        {
            TotalInEuros = 0;
            ValidarOperacion();
            return;
        }

        TotalInEuros = cantidad * SelectedCurrency.RateWithMargin;
        ValidarOperacion();
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
            ErrorMessage = "Ingrese un término de búsqueda o use 'Mostrar todos'";
            return;
        }
        ErrorMessage = string.Empty;
        await BuscarClientesAsync(BusquedaCliente);
    }
    
    [RelayCommand]
    private async Task MostrarTodosClientesAsync()
    {
        ErrorMessage = string.Empty;
        await BuscarClientesAsync(null);
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
                    "Teléfono" => "telefono ILIKE @termino",
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
            ErrorMessage = $"Error en búsqueda: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SeleccionarClienteYContinuar(ClienteModel? cliente)
    {
        if (cliente == null) return;
        ClienteSeleccionado = cliente;
        ValidarOperacion();
        OcultarTodasLasVistas();
        VistaTransaccion = true;
    }

    // ═══════════════════════════════════════════════════════════
    // FORMULARIO NUEVO CLIENTE
    // ═══════════════════════════════════════════════════════════

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
        NuevaCaducidadDocumento = null;
        NuevaFechaNacimiento = null;
        NuevaImagenDocumentoFrontal = null;
        NuevaImagenDocumentoTrasera = null;
        TieneImagenDocumentoFrontal = false;
        TieneImagenDocumentoTrasera = false;
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
            cmd.Parameters.AddWithValue("caducidad", (object?)NuevaCaducidadDocumento?.DateTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("fechaNac", (object?)NuevaFechaNacimiento?.DateTime ?? DBNull.Value);
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
                CaducidadDocumento = NuevaCaducidadDocumento?.DateTime,
                FechaNacimiento = NuevaFechaNacimiento?.DateTime,
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

    // ═══════════════════════════════════════════════════════════
    // IMÁGENES DE DOCUMENTO
    // ═══════════════════════════════════════════════════════════
    
    public void SetImagenDocumentoFrontal(byte[] imagen)
    {
        NuevaImagenDocumentoFrontal = imagen;
        TieneImagenDocumentoFrontal = true;
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
        ImagenDocumentoFrontalPreview = null;
    }
    
    [RelayCommand]
    private void QuitarImagenTrasera()
    {
        NuevaImagenDocumentoTrasera = null;
        TieneImagenDocumentoTrasera = false;
        ImagenDocumentoTraseraPreview = null;
    }

    // ═══════════════════════════════════════════════════════════
    // TRANSACCIÓN
    // ═══════════════════════════════════════════════════════════
    
    [RelayCommand]
    private async Task RealizarTransaccionAsync()
    {
        if (!PuedeRealizarOperacion)
        {
            ErrorMessage = "No se puede realizar la operación. Verifique los datos.";
            return;
        }
        
        IsLoading = true;
        ErrorMessage = string.Empty;
        
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            var textoLimpio = AmountReceived.Replace(",", ".");
            decimal.TryParse(textoLimpio, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidadOrigen);
            
            FechaOperacion = DateTime.Now;
            
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
                ErrorMessage = "Error: No se encontró un local/comercio válido en el sistema.";
                return;
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
                ErrorMessage = "Error: No se encontró un usuario válido en el sistema.";
                return;
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
            
            // Generar número de operación único: DI + YYYYMMDD + secuencial
            var fechaStr = FechaOperacion.ToString("yyyyMMdd");
            var sqlNumero = @"SELECT COALESCE(MAX(
                                CASE 
                                    WHEN LENGTH(numero_operacion) >= 14 
                                         AND SUBSTRING(numero_operacion FROM 11) ~ '^[0-9]+$'
                                    THEN CAST(SUBSTRING(numero_operacion FROM 11) AS INTEGER)
                                    ELSE 0 
                                END
                              ), 0) + 1 
                              FROM operaciones 
                              WHERE numero_operacion LIKE @prefijo";
            await using var cmdNum = new NpgsqlCommand(sqlNumero, conn);
            cmdNum.Parameters.AddWithValue("prefijo", $"DI{fechaStr}%");
            var secuencial = Convert.ToInt32(await cmdNum.ExecuteScalarAsync());
            var numeroOperacionGenerado = $"DI{fechaStr}{secuencial:D4}";
            
            var sqlOperacion = @"INSERT INTO operaciones 
                                (numero_operacion, id_comercio, id_local, codigo_local, 
                                 id_usuario, nombre_usuario, numero_usuario,
                                 id_cliente, nombre_cliente, modulo, tipo_operacion,
                                 importe_total, estado, fecha_operacion, observaciones)
                                VALUES 
                                (@numOp, @idComercio, @idLocal, @codigoLocal,
                                 @idUsuario, @nombreUsuario, @numeroUsuario,
                                 @idCliente, @nombreCliente, 'DIVISAS', 'COMPRA',
                                 @importe, 'COMPLETADA', @fecha, @obs)
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
            cmdOp.Parameters.AddWithValue("fecha", FechaOperacion);
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
                               tipo_movimiento, observaciones)
                              VALUES 
                              (@idComercio, @idLocal, @idUsuario, @idOperacion,
                               @codigoDivisa, @nombreDivisa,
                               @cantidadRecibida, @cantidadEntregada,
                               @tasaMomento, @tasaAplicada,
                               'ENTRADA', @obs)";
            
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
            
            await cmdBalance.ExecuteNonQueryAsync();
            
            // Usar el número generado para mostrar
            NumeroOperacionDisplay = numeroOperacionGenerado;
            
            OcultarTodasLasVistas();
            VistaResumen = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al realizar transacción: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
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
            new() { Code = "USD", Name = "Dólar USA", Country = "Estados Unidos" },
            new() { Code = "CAD", Name = "Dólar Canadiense", Country = "Canadá" },
            new() { Code = "CHF", Name = "Franco Suizo", Country = "Suiza" },
            new() { Code = "GBP", Name = "Libra Esterlina", Country = "Reino Unido" },
            new() { Code = "CNY", Name = "Yuan Chino", Country = "China" },
            new() { Code = "JPY", Name = "Yen Japonés", Country = "Japón" },
            new() { Code = "AUD", Name = "Dólar Australiano", Country = "Australia" },
            new() { Code = "MXN", Name = "Peso Mexicano", Country = "México" },
            new() { Code = "BRL", Name = "Real Brasileño", Country = "Brasil" },
            new() { Code = "ARS", Name = "Peso Argentino", Country = "Argentina" },
            new() { Code = "CLP", Name = "Peso Chileno", Country = "Chile" },
            new() { Code = "COP", Name = "Peso Colombiano", Country = "Colombia" },
            new() { Code = "PEN", Name = "Sol Peruano", Country = "Perú" },
            new() { Code = "BOB", Name = "Boliviano", Country = "Bolivia" },
            new() { Code = "VES", Name = "Bolívar", Country = "Venezuela" },
            new() { Code = "DOP", Name = "Peso Dominicano", Country = "Rep. Dominicana" },
            new() { Code = "UYU", Name = "Peso Uruguayo", Country = "Uruguay" },
            new() { Code = "INR", Name = "Rupia India", Country = "India" },
            new() { Code = "KRW", Name = "Won Surcoreano", Country = "Corea del Sur" },
            new() { Code = "SGD", Name = "Dólar Singapur", Country = "Singapur" },
            new() { Code = "HKD", Name = "Dólar Hong Kong", Country = "Hong Kong" },
            new() { Code = "NZD", Name = "Dólar Neozelandés", Country = "Nueva Zelanda" },
            new() { Code = "SEK", Name = "Corona Sueca", Country = "Suecia" },
            new() { Code = "NOK", Name = "Corona Noruega", Country = "Noruega" },
            new() { Code = "DKK", Name = "Corona Danesa", Country = "Dinamarca" },
            new() { Code = "PLN", Name = "Zloty Polaco", Country = "Polonia" },
            new() { Code = "CZK", Name = "Corona Checa", Country = "República Checa" },
            new() { Code = "HUF", Name = "Florín Húngaro", Country = "Hungría" },
            new() { Code = "RON", Name = "Leu Rumano", Country = "Rumania" },
            new() { Code = "TRY", Name = "Lira Turca", Country = "Turquía" },
            new() { Code = "ZAR", Name = "Rand Sudafricano", Country = "Sudáfrica" },
            new() { Code = "THB", Name = "Baht Tailandés", Country = "Tailandia" },
            new() { Code = "MYR", Name = "Ringgit Malayo", Country = "Malasia" },
            new() { Code = "PHP", Name = "Peso Filipino", Country = "Filipinas" },
            new() { Code = "IDR", Name = "Rupia Indonesia", Country = "Indonesia" },
            new() { Code = "AED", Name = "Dirham EAU", Country = "Emiratos Árabes" },
            new() { Code = "SAR", Name = "Riyal Saudí", Country = "Arabia Saudita" },
            new() { Code = "ILS", Name = "Shekel Israelí", Country = "Israel" },
            new() { Code = "EGP", Name = "Libra Egipcia", Country = "Egipto" },
            new() { Code = "MAD", Name = "Dirham Marroquí", Country = "Marruecos" }
        };
    }
}