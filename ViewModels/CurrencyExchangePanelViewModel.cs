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

/// <summary>
/// ViewModel para el Panel de Cambio de Divisas
/// IMPORTANTE: El margen de ganancia NUNCA se muestra al usuario
/// Solo se aplica internamente en los cálculos
/// </summary>
public partial class CurrencyExchangePanelViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";
    
    private readonly CurrencyExchangeService _currencyService;
    
    // Margen de ganancia (INTERNO - NUNCA MOSTRAR AL USUARIO)
    private decimal _margenGanancia = 10.00m;
    private int _idLocalActual = 0;
    private int _idComercioActual = 0;

    // ============================================
    // CLIENTE
    // ============================================
    
    [ObservableProperty]
    private ClienteModel? _clienteSeleccionado;
    
    [ObservableProperty]
    private ObservableCollection<ClienteModel> _clientesEncontrados = new();
    
    [ObservableProperty]
    private string _busquedaCliente = string.Empty;
    
    [ObservableProperty]
    private bool _mostrarResultadosBusqueda = false;
    
    // Bandera para evitar búsqueda al seleccionar cliente
    private bool _seleccionandoCliente = false;
    
    [ObservableProperty]
    private bool _mostrarFormularioCliente = false;
    
    // Campos del formulario de nuevo cliente
    [ObservableProperty]
    private string _nuevoNombre = string.Empty;
    
    [ObservableProperty]
    private string _nuevoApellido = string.Empty;
    
    [ObservableProperty]
    private string _nuevoTelefono = string.Empty;
    
    [ObservableProperty]
    private string _nuevaDireccion = string.Empty;
    
    [ObservableProperty]
    private string _nuevaNacionalidad = string.Empty;
    
    [ObservableProperty]
    private string _nuevoTipoDocumento = "NIE";
    
    [ObservableProperty]
    private string _nuevoNumeroDocumento = string.Empty;
    
    [ObservableProperty]
    private DateTimeOffset? _nuevaCaducidadDocumento;
    
    [ObservableProperty]
    private DateTimeOffset? _nuevaFechaNacimiento;
    
    [ObservableProperty]
    private byte[]? _nuevaImagenDocumento;
    
    [ObservableProperty]
    private string _nuevoNombreArchivoDocumento = string.Empty;
    
    [ObservableProperty]
    private bool _tieneImagenDocumento = false;
    
    [ObservableProperty]
    private Avalonia.Media.Imaging.Bitmap? _imagenDocumentoPreview;

    // ============================================
    // DIVISAS
    // ============================================
    
    [ObservableProperty]
    private ObservableCollection<FavoriteCurrencyModel> _favoriteCurrencies = new();
    
    [ObservableProperty]
    private ObservableCollection<CurrencyModel> _availableCurrencies = new();
    
    [ObservableProperty]
    private ObservableCollection<CurrencyModel> _divisasBusqueda = new();
    
    [ObservableProperty]
    private string _busquedaDivisa = string.Empty;
    
    [ObservableProperty]
    private CurrencyModel? _selectedCurrency;
    
    [ObservableProperty]
    private bool _mostrarListaDivisas = false;
    
    [ObservableProperty]
    private bool _mostrarSelectorDivisas = false;

    // ============================================
    // CALCULADORA
    // ============================================
    
    [ObservableProperty]
    private string _amountReceived = string.Empty;
    
    [ObservableProperty]
    private decimal _totalInEuros;
    
    [ObservableProperty]
    private decimal _currentRate;
    
    [ObservableProperty]
    private string _rateDisplayText = string.Empty;

    // ============================================
    // ESTADOS
    // ============================================
    
    [ObservableProperty]
    private bool _isLoading = false;
    
    [ObservableProperty]
    private string _errorMessage = string.Empty;
    
    [ObservableProperty]
    private string _lastUpdateText = string.Empty;
    
    [ObservableProperty]
    private bool _puedeRealizarOperacion = false;

    // Lista de tipos de documento
    public ObservableCollection<string> TiposDocumento { get; } = new()
    {
        "DNI", "NIE", "Pasaporte"
    };

    private List<string> _favoriteCodes = new() { "USD", "CAD", "CHF", "GBP", "CNY" };

    // ============================================
    // CONSTRUCTOR
    // ============================================

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

    private async void InitializeAsync()
    {
        await CargarMargenGananciaAsync();
        await LoadDataAsync();
    }

    // ============================================
    // CARGA DE MARGEN (INTERNO - NUNCA MOSTRAR)
    // ============================================

    private async Task CargarMargenGananciaAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            decimal? margenLocal = null;
            decimal? margenComercio = null;
            decimal? margenGlobal = null;

            // Buscar margen del local
            if (_idLocalActual > 0)
            {
                var sqlLocal = "SELECT comision_divisas FROM locales WHERE id_local = @id AND comision_divisas > 0";
                await using var cmdLocal = new NpgsqlCommand(sqlLocal, conn);
                cmdLocal.Parameters.AddWithValue("id", _idLocalActual);
                var result = await cmdLocal.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    margenLocal = Convert.ToDecimal(result);
            }

            // Buscar margen del comercio
            if (_idComercioActual > 0 && margenLocal == null)
            {
                var sqlComercio = "SELECT porcentaje_comision_divisas FROM comercios WHERE id_comercio = @id AND porcentaje_comision_divisas > 0";
                await using var cmdComercio = new NpgsqlCommand(sqlComercio, conn);
                cmdComercio.Parameters.AddWithValue("id", _idComercioActual);
                var result = await cmdComercio.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    margenComercio = Convert.ToDecimal(result);
            }

            // Buscar margen global
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

    // ============================================
    // CARGA DE DATOS
    // ============================================

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
                    // Aplicar margen internamente (NUNCA mostrar al usuario)
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
            {
                SelectedCurrency = AvailableCurrencies.First();
            }
            
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
            
            // Aplicar margen a favoritas (INTERNO)
            foreach (var fav in favorites)
            {
                fav.RateWithMargin = fav.RateToEur * (1m - (_margenGanancia / 100m));
            }
            
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

    // ============================================
    // BÚSQUEDA DE DIVISAS
    // ============================================

    partial void OnBusquedaDivisaChanged(string value)
    {
        // Si el selector de divisas está abierto, filtrar
        if (MostrarSelectorDivisas)
        {
            var favCodes = FavoriteCurrencies.Select(f => f.Code).ToHashSet();
            DivisasBusqueda.Clear();
            
            IEnumerable<CurrencyModel> resultados;
            
            if (string.IsNullOrWhiteSpace(value))
            {
                resultados = AvailableCurrencies.Where(d => !favCodes.Contains(d.Code));
            }
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
            {
                DivisasBusqueda.Add(divisa);
            }
        }
    }

    [RelayCommand]
    private void SeleccionarDivisa(CurrencyModel? divisa)
    {
        if (divisa == null) return;
        
        SelectedCurrency = divisa;
        BusquedaDivisa = $"{divisa.Code} | {divisa.Name}";
        // Mostrar tasa CON margen (el usuario no sabe que tiene margen)
        CurrentRate = divisa.RateWithMargin;
        RateDisplayText = $"1 {divisa.Code} = {divisa.RateWithMargin:N4} EUR";
        MostrarListaDivisas = false;
        
        CalcularConversion();
    }

    [RelayCommand]
    private void SeleccionarDivisaFavorita(FavoriteCurrencyModel? divisa)
    {
        if (divisa == null) return;
        
        var currency = AvailableCurrencies.FirstOrDefault(c => c.Code == divisa.Code);
        if (currency != null)
        {
            SeleccionarDivisa(currency);
        }
    }

    // ============================================
    // CALCULADORA
    // ============================================

    partial void OnSelectedCurrencyChanged(CurrencyModel? value)
    {
        if (value != null)
        {
            // Usar tasa CON margen
            CurrentRate = value.RateWithMargin;
            RateDisplayText = $"1 {value.Code} = {value.RateWithMargin:N4} EUR";
            CalcularConversion();
        }
    }

    partial void OnAmountReceivedChanged(string value)
    {
        CalcularConversion();
    }

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

        // Calcular con tasa que ya tiene margen aplicado
        TotalInEuros = cantidad * SelectedCurrency.RateWithMargin;
        ValidarOperacion();
    }

    private void ValidarOperacion()
    {
        var cantidadValida = decimal.TryParse(AmountReceived?.Replace(",", "."), 
            NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidad) && cantidad > 0;
        
        PuedeRealizarOperacion = ClienteSeleccionado != null && 
                                  SelectedCurrency != null && 
                                  cantidadValida;
    }

    // ============================================
    // BÚSQUEDA DE CLIENTES
    // ============================================

    partial void OnBusquedaClienteChanged(string value)
    {
        // No buscar si estamos seleccionando un cliente
        if (_seleccionandoCliente)
            return;
            
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            MostrarResultadosBusqueda = false;
            ClientesEncontrados.Clear();
            return;
        }

        // Limpiar antes de buscar para evitar duplicados
        ClientesEncontrados.Clear();
        _ = BuscarClientesAsync(value);
    }

    private async Task BuscarClientesAsync(string termino)
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = @"SELECT DISTINCT id_cliente, nombre, apellidos, telefono, direccion,
                               COALESCE(nacionalidad, '') as nacionalidad, 
                               COALESCE(documento_tipo, 'DNI') as documento_tipo, 
                               COALESCE(documento_numero, '') as documento_numero,
                               caducidad_documento, fecha_nacimiento
                        FROM clientes
                        WHERE activo = true
                          AND (documento_numero ILIKE @termino
                               OR telefono ILIKE @termino
                               OR nombre ILIKE @termino
                               OR apellidos ILIKE @termino
                               OR CONCAT(nombre, ' ', apellidos) ILIKE @termino)
                        ORDER BY nombre, apellidos
                        LIMIT 10";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("termino", $"%{termino}%");

            ClientesEncontrados.Clear();
            var idsAgregados = new HashSet<int>();
            
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var idCliente = reader.GetInt32(0);
                
                // Evitar duplicados
                if (idsAgregados.Contains(idCliente))
                    continue;
                    
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
                    FechaNacimiento = reader.IsDBNull(9) ? null : reader.GetDateTime(9)
                };
                
                ClientesEncontrados.Add(cliente);
            }

            MostrarResultadosBusqueda = ClientesEncontrados.Any();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error en búsqueda: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private async Task BuscarClientesManualAsync()
    {
        if (!string.IsNullOrWhiteSpace(BusquedaCliente))
        {
            await BuscarClientesAsync(BusquedaCliente);
        }
    }

    [RelayCommand]
    private void SeleccionarCliente(ClienteModel? cliente)
    {
        if (cliente == null) return;
        
        _seleccionandoCliente = true;
        
        ClienteSeleccionado = cliente;
        BusquedaCliente = cliente.NombreCompleto;
        MostrarResultadosBusqueda = false;
        ClientesEncontrados.Clear();
        ValidarOperacion();
        
        _seleccionandoCliente = false;
    }

    [RelayCommand]
    private void LimpiarCliente()
    {
        _seleccionandoCliente = true;
        ClienteSeleccionado = null;
        BusquedaCliente = string.Empty;
        ClientesEncontrados.Clear();
        MostrarResultadosBusqueda = false;
        _seleccionandoCliente = false;
        ValidarOperacion();
    }
    
    [RelayCommand]
    private void CerrarBusqueda()
    {
        MostrarResultadosBusqueda = false;
        ClientesEncontrados.Clear();
    }

    // ============================================
    // FORMULARIO NUEVO CLIENTE
    // ============================================

    [RelayCommand]
    private void MostrarNuevoCliente()
    {
        LimpiarFormularioCliente();
        MostrarFormularioCliente = true;
        MostrarResultadosBusqueda = false;
    }

    [RelayCommand]
    private void CancelarNuevoCliente()
    {
        MostrarFormularioCliente = false;
        LimpiarFormularioCliente();
    }

    private void LimpiarFormularioCliente()
    {
        NuevoNombre = string.Empty;
        NuevoApellido = string.Empty;
        NuevoTelefono = string.Empty;
        NuevaDireccion = string.Empty;
        NuevaNacionalidad = string.Empty;
        NuevoTipoDocumento = "DNI";
        NuevoNumeroDocumento = string.Empty;
        NuevaCaducidadDocumento = null;
        NuevaFechaNacimiento = null;
        NuevaImagenDocumento = null;
        NuevoNombreArchivoDocumento = string.Empty;
        TieneImagenDocumento = false;
        ImagenDocumentoPreview = null;
    }

    [RelayCommand]
    private async Task GuardarNuevoClienteAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoNombre))
        {
            ErrorMessage = "El nombre es obligatorio";
            return;
        }
        
        if (string.IsNullOrWhiteSpace(NuevoNumeroDocumento))
        {
            ErrorMessage = "El número de documento es obligatorio";
            return;
        }

        IsLoading = true;

        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = @"INSERT INTO clientes 
                        (nombre, apellidos, telefono, direccion, nacionalidad,
                         documento_tipo, documento_numero, caducidad_documento,
                         fecha_nacimiento, imagen_documento, nombre_archivo_documento,
                         id_local_registro, activo)
                        VALUES 
                        (@nombre, @apellido, @telefono, @direccion, @nacionalidad,
                         @tipoDoc, @numDoc, @caducidad, @fechaNac, @imagen, @nombreArchivo,
                         @idLocal, true)
                        RETURNING id_cliente";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("nombre", NuevoNombre);
            cmd.Parameters.AddWithValue("apellido", NuevoApellido ?? "");
            cmd.Parameters.AddWithValue("telefono", NuevoTelefono ?? "");
            cmd.Parameters.AddWithValue("direccion", NuevaDireccion ?? "");
            cmd.Parameters.AddWithValue("nacionalidad", NuevaNacionalidad ?? "");
            cmd.Parameters.AddWithValue("tipoDoc", NuevoTipoDocumento);
            cmd.Parameters.AddWithValue("numDoc", NuevoNumeroDocumento);
            cmd.Parameters.AddWithValue("caducidad", (object?)NuevaCaducidadDocumento?.DateTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("fechaNac", (object?)NuevaFechaNacimiento?.DateTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("imagen", (object?)NuevaImagenDocumento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("nombreArchivo", NuevoNombreArchivoDocumento ?? "");
            cmd.Parameters.AddWithValue("idLocal", _idLocalActual > 0 ? _idLocalActual : DBNull.Value);

            var idCliente = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            var nuevoCliente = new ClienteModel
            {
                IdCliente = idCliente,
                Nombre = NuevoNombre,
                Apellido = NuevoApellido ?? "",
                Telefono = NuevoTelefono ?? "",
                Direccion = NuevaDireccion ?? "",
                Nacionalidad = NuevaNacionalidad ?? "",
                TipoDocumento = NuevoTipoDocumento,
                NumeroDocumento = NuevoNumeroDocumento,
                CaducidadDocumento = NuevaCaducidadDocumento?.DateTime,
                FechaNacimiento = NuevaFechaNacimiento?.DateTime
            };

            ClienteSeleccionado = nuevoCliente;
            BusquedaCliente = nuevoCliente.NombreCompleto;
            MostrarFormularioCliente = false;
            LimpiarFormularioCliente();
            ValidarOperacion();
            
            ErrorMessage = "";
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

    public void SetImagenDocumento(byte[] imagen, string nombreArchivo)
    {
        NuevaImagenDocumento = imagen;
        NuevoNombreArchivoDocumento = nombreArchivo;
        TieneImagenDocumento = true;
        
        // Crear preview de la imagen
        try
        {
            using var stream = new MemoryStream(imagen);
            ImagenDocumentoPreview = new Avalonia.Media.Imaging.Bitmap(stream);
        }
        catch
        {
            // Si no es una imagen válida (ej: PDF), no mostrar preview
            ImagenDocumentoPreview = null;
        }
    }
    
    [RelayCommand]
    private void QuitarImagenDocumento()
    {
        NuevaImagenDocumento = null;
        NuevoNombreArchivoDocumento = string.Empty;
        TieneImagenDocumento = false;
        ImagenDocumentoPreview = null;
    }

    // ============================================
    // GESTIÓN DE FAVORITAS
    // ============================================

    [RelayCommand]
    private void MostrarAgregarDivisa()
    {
        // Mostrar todas las divisas que no están en favoritas
        DivisasBusqueda.Clear();
        var favCodes = FavoriteCurrencies.Select(f => f.Code).ToHashSet();
        
        foreach (var divisa in AvailableCurrencies.Where(d => !favCodes.Contains(d.Code)))
        {
            DivisasBusqueda.Add(divisa);
        }
        
        BusquedaDivisa = string.Empty;
        MostrarSelectorDivisas = true;
    }
    
    [RelayCommand]
    private void CerrarSelectorDivisas()
    {
        MostrarSelectorDivisas = false;
        BusquedaDivisa = string.Empty;
    }

    [RelayCommand]
    private void AgregarAFavoritas(CurrencyModel? currency)
    {
        if (currency == null) return;
        
        if (!_favoriteCodes.Contains(currency.Code))
        {
            _favoriteCodes.Add(currency.Code);
            
            // Agregar a la colección visible
            var favorite = new FavoriteCurrencyModel
            {
                Code = currency.Code,
                Name = currency.Name,
                Country = currency.Country,
                RateToEur = currency.RateToEur,
                RateWithMargin = currency.RateWithMargin
            };
            FavoriteCurrencies.Add(favorite);
            
            // Remover de la lista de búsqueda
            DivisasBusqueda.Remove(currency);
        }
    }
    
    [RelayCommand]
    private void QuitarDeFavoritas(FavoriteCurrencyModel? currency)
    {
        if (currency == null) return;
        
        _favoriteCodes.Remove(currency.Code);
        FavoriteCurrencies.Remove(currency);
        
        // Si el selector está abierto, agregar de vuelta a la lista
        if (MostrarSelectorDivisas)
        {
            var original = AvailableCurrencies.FirstOrDefault(c => c.Code == currency.Code);
            if (original != null && !DivisasBusqueda.Contains(original))
            {
                DivisasBusqueda.Add(original);
            }
        }
    }

    [RelayCommand]
    private void RemoveFromFavorites(FavoriteCurrencyModel? currency)
    {
        if (currency == null) return;
        
        _favoriteCodes.Remove(currency.Code);
        var toRemove = FavoriteCurrencies.FirstOrDefault(f => f.Code == currency.Code);
        if (toRemove != null)
        {
            FavoriteCurrencies.Remove(toRemove);
        }
    }

    // ============================================
    // LISTA DE DIVISAS
    // ============================================

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