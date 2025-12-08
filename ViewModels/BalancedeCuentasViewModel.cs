using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using Allva.Desktop.Services;
using Allva.Desktop.Views;

namespace Allva.Desktop.ViewModels;

public partial class BalancedeCuentasViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";
    
    private readonly int _idComercio;
    private readonly int _idLocal;
    private readonly string _codigoLocal;
    private readonly int _idUsuario;
    private readonly string _nombreUsuario;
    
    // Zona horaria de Espana (Europe/Madrid)
    private static readonly TimeZoneInfo _zonaHorariaEspana = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Romance Standard Time" : "Europe/Madrid");
    
    [ObservableProperty]
    private string localInfo = "";
    
    [ObservableProperty]
    private string tabActual = "divisa";
    
    // Fechas en formato dd/mm/aaaa
    [ObservableProperty]
    private string fechaDesdeTexto = "";
    
    [ObservableProperty]
    private string fechaHastaTexto = "";
    
    [ObservableProperty]
    private string operacionDesde = "";
    
    [ObservableProperty]
    private string operacionHasta = "";
    
    [ObservableProperty]
    private string fechaActualTexto = "";
    
    // Balance - Solo T.Euros y T.Divisa
    [ObservableProperty]
    private string totalEuros = "0.00";
    
    [ObservableProperty]
    private string totalDivisa = "0.00";
    
    // Color dinamico para T.Euros (verde si positivo, rojo si negativo)
    [ObservableProperty]
    private string totalEurosColor = "#0b5394";
    
    // Mantenemos estos para calculos internos y PDF
    [ObservableProperty]
    private string salidaEurosTotal = "0.00";
    
    [ObservableProperty]
    private string entradaEurosTotal = "0.00";
    
    private decimal _totalEurosNumerico = 0;
    private decimal _salidaEurosNumerico = 0;
    private decimal _entradaEurosNumerico = 0;
    
    // Deposito en banco
    [ObservableProperty]
    private string divisaDepositoSeleccionada = "";
    
    [ObservableProperty]
    private string cantidadDivisaDeposito = "";
    
    [ObservableProperty]
    private string cantidadEurosDeposito = "";
    
    // Transferencia a caja
    [ObservableProperty]
    private string cantidadTraspaso = "";
    
    [ObservableProperty]
    private bool isLoading = false;
    
    [ObservableProperty]
    private string errorMessage = "";
    
    [ObservableProperty]
    private string successMessage = "";
    
    [ObservableProperty]
    private bool tieneDivisas = false;
    
    public ObservableCollection<OperacionItem> Operaciones { get; } = new();
    public ObservableCollection<DivisaLocal> DivisasDelLocal { get; } = new();
    public ObservableCollection<string> DivisasParaDeposito { get; } = new();
    
    private readonly string[] _mesesEspanol = { 
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" 
    };
    
    private readonly string[] _diasSemana = {
        "domingo", "lunes", "martes", "miercoles", "jueves", "viernes", "sabado"
    };
    
    public BalancedeCuentasViewModel()
    {
        _idComercio = 0;
        _idLocal = 0;
        _codigoLocal = "---";
        _idUsuario = 0;
        _nombreUsuario = "Usuario";
        LocalInfo = $"(Oficina - {_codigoLocal})";
        InicializarFechas();
    }
    
    public BalancedeCuentasViewModel(int idComercio, int idLocal, string codigoLocal)
    {
        _idComercio = idComercio;
        _idLocal = idLocal;
        _codigoLocal = codigoLocal;
        _idUsuario = 0;
        _nombreUsuario = "Usuario";
        LocalInfo = $"(Oficina - {_codigoLocal})";
        
        InicializarFechas();
        _ = CargarDatosAsync();
    }
    
    public BalancedeCuentasViewModel(int idComercio, int idLocal, string codigoLocal, int idUsuario, string nombreUsuario)
    {
        _idComercio = idComercio;
        _idLocal = idLocal;
        _codigoLocal = codigoLocal;
        _idUsuario = idUsuario;
        _nombreUsuario = nombreUsuario;
        LocalInfo = $"(Oficina - {_codigoLocal})";
        
        InicializarFechas();
        _ = CargarDatosAsync();
    }
    
    // Cuando cambia la divisa seleccionada, autocompletar cantidad
    partial void OnDivisaDepositoSeleccionadaChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var divisa = DivisasDelLocal.FirstOrDefault(d => d.Codigo == value);
            if (divisa != null)
            {
                CantidadDivisaDeposito = divisa.Cantidad.ToString("0.##", CultureInfo.InvariantCulture);
            }
        }
    }
    
    private DateTime ObtenerHoraEspana()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zonaHorariaEspana);
    }

    private void InicializarFechas()
    {
        var hoy = ObtenerHoraEspana();
        FechaActualTexto = FormatearFechaCompleta(hoy);
        
        // Fecha desde: primer dia del mes
        FechaDesdeTexto = $"01/{hoy.Month:D2}/{hoy.Year}";
        
        // Fecha hasta: hoy
        FechaHastaTexto = $"{hoy.Day:D2}/{hoy.Month:D2}/{hoy.Year}";
    }
    
    private string FormatearFechaCompleta(DateTime fecha)
    {
        var diaSemana = _diasSemana[(int)fecha.DayOfWeek];
        var mes = _mesesEspanol[fecha.Month - 1];
        return $"{diaSemana}, {fecha.Day} de {mes} de {fecha.Year}";
    }
    
    private DateTime? ParsearFecha(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        
        if (DateTime.TryParseExact(texto, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            return fecha;
        
        if (DateTime.TryParseExact(texto, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            return fecha;
        
        if (DateTime.TryParseExact(texto, "d/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            return fecha;
        
        if (DateTime.TryParseExact(texto, "dd/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            return fecha;
            
        return null;
    }
    
    private async Task CargarDatosAsync()
    {
        await CargarOperacionesAsync();
        await CargarDivisasDelLocalAsync();
        await CargarBalancesAsync();
    }
    
    [RelayCommand]
    private void CambiarTab(string tab)
    {
        TabActual = tab;
    }
    
    [RelayCommand]
    private async Task BuscarAsync()
    {
        await CargarOperacionesAsync();
        await CargarBalancesAsync();
    }
    
    [RelayCommand]
    private void LimpiarFiltros()
    {
        var hoy = ObtenerHoraEspana();
        FechaDesdeTexto = $"01/{hoy.Month:D2}/{hoy.Year}";
        FechaHastaTexto = $"{hoy.Day:D2}/{hoy.Month:D2}/{hoy.Year}";
        OperacionDesde = "";
        OperacionHasta = "";
    }
    
    [RelayCommand]
    private async Task ImprimirHistorialAsync()
    {
        try
        {
            var ahora = ObtenerHoraEspana();
            
            // Crear ViewModel para la ventana de confirmacion
            var confirmacionVM = new ConfirmacionImpresionViewModel
            {
                FechaGeneracion = ahora.ToString("dd/MM/yyyy"),
                HoraGeneracion = ahora.ToString("HH:mm:ss"),
                NombreUsuario = _nombreUsuario,
                CodigoLocal = _codigoLocal,
                BalanceEuros = TotalEuros,
                BalanceEurosEsPositivo = _totalEurosNumerico >= 0,
                TotalDivisas = TotalDivisa,
                CantidadOperaciones = $"{Operaciones.Count} registros"
            };
            
            // Cargar filtros aplicados
            var hayFiltros = false;
            
            if (!string.IsNullOrWhiteSpace(FechaDesdeTexto))
            {
                confirmacionVM.FiltrosAplicados.Add(new FiltroAplicadoItem { Nombre = "Fecha desde:", Valor = FechaDesdeTexto });
                hayFiltros = true;
            }
            if (!string.IsNullOrWhiteSpace(FechaHastaTexto))
            {
                confirmacionVM.FiltrosAplicados.Add(new FiltroAplicadoItem { Nombre = "Fecha hasta:", Valor = FechaHastaTexto });
                hayFiltros = true;
            }
            if (!string.IsNullOrWhiteSpace(OperacionDesde))
            {
                confirmacionVM.FiltrosAplicados.Add(new FiltroAplicadoItem { Nombre = "Operacion desde:", Valor = OperacionDesde });
                hayFiltros = true;
            }
            if (!string.IsNullOrWhiteSpace(OperacionHasta))
            {
                confirmacionVM.FiltrosAplicados.Add(new FiltroAplicadoItem { Nombre = "Operacion hasta:", Valor = OperacionHasta });
                hayFiltros = true;
            }
            
            confirmacionVM.SinFiltros = !hayFiltros;
            
            // Desglose de divisas
            if (DivisasDelLocal.Count > 0)
            {
                confirmacionVM.TieneDivisas = true;
                var partes = DivisasDelLocal.Select(d => $"{d.Codigo}: {d.Cantidad:N2}");
                confirmacionVM.DesgloseDivisasTexto = string.Join("  |  ", partes);
            }
            
            // Mostrar ventana de confirmacion
            var ventanaConfirmacion = new ConfirmacionImpresionView(confirmacionVM);
            
            // Obtener ventana principal
            Window? mainWindow = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                mainWindow = desktop.MainWindow;
            }
            
            if (mainWindow != null)
            {
                await ventanaConfirmacion.ShowDialog(mainWindow);
            }
            else
            {
                ventanaConfirmacion.Show();
                await Task.Delay(100);
                while (ventanaConfirmacion.IsVisible)
                    await Task.Delay(100);
            }
            
            // Si confirmo, generar PDF
            if (ventanaConfirmacion.Confirmado)
            {
                await GenerarPdfHistorial(ahora);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            await Task.Delay(3000);
            ErrorMessage = "";
        }
    }
    
    private async Task GenerarPdfHistorial(DateTime fechaHora)
    {
        try
        {
            // Obtener ventana principal para el dialogo
            Window? mainWindow = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                mainWindow = desktop.MainWindow;
            }
            
            if (mainWindow == null)
            {
                ErrorMessage = "No se pudo abrir el dialogo de guardado";
                await Task.Delay(2000);
                ErrorMessage = "";
                return;
            }
            
            // Nombre sugerido para el archivo
            var timestamp = fechaHora.ToString("yyyyMMdd_HHmmss");
            var nombreSugerido = $"Historial_{_codigoLocal}_{timestamp}.pdf";
            
            // Mostrar dialogo para elegir ubicacion (nueva API)
            var storageProvider = mainWindow.StorageProvider;
            
            var archivo = await storageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Guardar historial de balance",
                SuggestedFileName = nombreSugerido,
                DefaultExtension = "pdf",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Archivos PDF")
                    {
                        Patterns = new[] { "*.pdf" }
                    }
                }
            });
            
            // Si el usuario cancelo el dialogo
            if (archivo == null)
            {
                return;
            }
            
            IsLoading = true;
            ErrorMessage = "";
            
            // Preparar datos del reporte
            var datosReporte = new HistorialPdfService.DatosReporte
            {
                CodigoLocal = _codigoLocal,
                NombreUsuario = _nombreUsuario,
                FechaGeneracion = fechaHora.ToString("dd/MM/yyyy"),
                HoraGeneracion = fechaHora.ToString("HH:mm:ss"),
                BalanceActualEuros = _totalEurosNumerico,
                TotalDivisasValor = DivisasDelLocal.Sum(d => d.Cantidad),
                Filtros = new HistorialPdfService.FiltrosReporte
                {
                    FechaDesde = FechaDesdeTexto,
                    FechaHasta = FechaHastaTexto,
                    OperacionDesde = OperacionDesde,
                    OperacionHasta = OperacionHasta
                }
            };
            
            // Agregar desglose de divisas
            foreach (var div in DivisasDelLocal)
            {
                datosReporte.DesgloseDivisas.Add(new HistorialPdfService.DivisaBalance
                {
                    CodigoDivisa = div.Codigo,
                    Cantidad = div.Cantidad
                });
            }
            
            // Agregar operaciones
            foreach (var op in Operaciones)
            {
                datosReporte.Operaciones.Add(new HistorialPdfService.OperacionReporte
                {
                    Fecha = op.Fecha,
                    Hora = op.Hora,
                    NumeroOperacion = op.NumeroOperacion,
                    Descripcion = op.Descripcion,
                    Divisa = op.CantidadDivisa,
                    SalidaEuros = op.SalidaEuros,
                    EntradaEuros = op.EntradaEuros
                });
            }
            
            // Generar PDF
            var pdfBytes = HistorialPdfService.GenerarPdf(datosReporte);
            
            // Guardar archivo usando StorageProvider
            await using var stream = await archivo.OpenWriteAsync();
            await stream.WriteAsync(pdfBytes);
            
            // Registrar en base de datos
            await RegistrarGeneracionPdf(fechaHora);
            
            SuccessMessage = "PDF guardado correctamente";
            await Task.Delay(3000);
            SuccessMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al generar PDF: {ex.Message}";
            await Task.Delay(3000);
            ErrorMessage = "";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task RegistrarGeneracionPdf(DateTime fechaHora)
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            // Construir texto de filtros aplicados
            var filtrosTexto = "";
            if (!string.IsNullOrWhiteSpace(FechaDesdeTexto))
                filtrosTexto += $"Fecha desde: {FechaDesdeTexto}; ";
            if (!string.IsNullOrWhiteSpace(FechaHastaTexto))
                filtrosTexto += $"Fecha hasta: {FechaHastaTexto}; ";
            if (!string.IsNullOrWhiteSpace(OperacionDesde))
                filtrosTexto += $"Op desde: {OperacionDesde}; ";
            if (!string.IsNullOrWhiteSpace(OperacionHasta))
                filtrosTexto += $"Op hasta: {OperacionHasta}; ";
            
            if (string.IsNullOrEmpty(filtrosTexto))
                filtrosTexto = "Mes en curso";
            
            var sql = @"INSERT INTO historial_generacion_pdf 
                        (id_comercio, id_local, codigo_local, id_usuario, nombre_usuario,
                         modulo, tipo_reporte, filtros_aplicados, 
                         fecha_generacion, hora_generacion, registros_incluidos)
                        VALUES 
                        (@idComercio, @idLocal, @codigoLocal, @idUsuario, @nombreUsuario,
                         'DIVISAS', 'Historial Balance', @filtros,
                         @fecha, @hora, @registros)";
            
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("idComercio", _idComercio);
            cmd.Parameters.AddWithValue("idLocal", _idLocal);
            cmd.Parameters.AddWithValue("codigoLocal", _codigoLocal);
            cmd.Parameters.AddWithValue("idUsuario", _idUsuario);
            cmd.Parameters.AddWithValue("nombreUsuario", _nombreUsuario);
            cmd.Parameters.AddWithValue("filtros", filtrosTexto);
            cmd.Parameters.AddWithValue("fecha", fechaHora.Date);
            cmd.Parameters.AddWithValue("hora", fechaHora.TimeOfDay);
            cmd.Parameters.AddWithValue("registros", Operaciones.Count);
            
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            // No interrumpir el proceso si falla el registro
            System.Diagnostics.Debug.WriteLine($"Error al registrar PDF: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task InsertarDepositoAsync()
    {
        if (string.IsNullOrWhiteSpace(DivisaDepositoSeleccionada))
        {
            ErrorMessage = "Seleccione una divisa";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (string.IsNullOrWhiteSpace(CantidadDivisaDeposito))
        {
            ErrorMessage = "Ingrese la cantidad de divisa";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (!decimal.TryParse(CantidadDivisaDeposito.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidadDivisa))
        {
            ErrorMessage = "La cantidad de divisa debe ser un numero valido";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (cantidadDivisa <= 0)
        {
            ErrorMessage = "La cantidad de divisa debe ser mayor a cero";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (string.IsNullOrWhiteSpace(CantidadEurosDeposito))
        {
            ErrorMessage = "Ingrese la cantidad de euros";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (!decimal.TryParse(CantidadEurosDeposito.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal eurosRecibidos))
        {
            ErrorMessage = "La cantidad de euros debe ser un numero valido";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (eurosRecibidos <= 0)
        {
            ErrorMessage = "La cantidad de euros debe ser mayor a cero";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        // Verificar que haya suficiente divisa disponible
        var divisaLocal = DivisasDelLocal.FirstOrDefault(d => d.Codigo == DivisaDepositoSeleccionada);
        if (divisaLocal == null || divisaLocal.Cantidad <= 0)
        {
            ErrorMessage = "No hay cantidad disponible de esta divisa";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (cantidadDivisa > divisaLocal.Cantidad)
        {
            ErrorMessage = $"Solo tiene {divisaLocal.Cantidad:N2} {DivisaDepositoSeleccionada} disponibles";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        try
        {
            IsLoading = true;
            ErrorMessage = "";
            
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            var ahora = ObtenerHoraEspana();
            
            // Descripcion incluye cantidad de divisa para mostrar en la tabla
            var descripcion = $"Dto Banco: {cantidadDivisa:N2} {DivisaDepositoSeleccionada}";
            
            // Insertar en balance_cuentas (deposito = entrada de euros)
            var sqlBalance = @"INSERT INTO balance_cuentas 
                              (id_comercio, id_local, codigo_local, id_usuario, 
                               tipo_movimiento, modulo, descripcion, divisa, monto,
                               fecha_movimiento)
                              VALUES 
                              (@idComercio, @idLocal, @codigoLocal, @idUsuario,
                               'DEPOSITO', 'DIVISAS', @descripcion, @codigoDivisa, @monto,
                               @fecha)";
            
            await using var cmdBalance = new NpgsqlCommand(sqlBalance, conn);
            cmdBalance.Parameters.AddWithValue("idComercio", _idComercio);
            cmdBalance.Parameters.AddWithValue("idLocal", _idLocal);
            cmdBalance.Parameters.AddWithValue("codigoLocal", _codigoLocal);
            cmdBalance.Parameters.AddWithValue("idUsuario", _idUsuario);
            cmdBalance.Parameters.AddWithValue("descripcion", descripcion);
            cmdBalance.Parameters.AddWithValue("codigoDivisa", DivisaDepositoSeleccionada);
            cmdBalance.Parameters.AddWithValue("monto", eurosRecibidos);
            cmdBalance.Parameters.AddWithValue("fecha", ahora);
            
            await cmdBalance.ExecuteNonQueryAsync();
            
            // Actualizar balance_divisas (restar la divisa depositada)
            var sqlActualizarDivisa = @"INSERT INTO balance_divisas 
                                        (id_comercio, id_local, id_usuario, codigo_divisa, nombre_divisa,
                                         cantidad_recibida, cantidad_entregada_eur,
                                         tasa_cambio_momento, tasa_cambio_aplicada,
                                         tipo_movimiento, fecha_registro, observaciones)
                                        VALUES 
                                        (@idComercio, @idLocal, @idUsuario, @codigoDivisa, @nombreDivisa,
                                         @cantidadDivisa, @cantidadEuros,
                                         1, 1, 'SALIDA', @fecha, 'Dto Banco')";
            
            await using var cmdDivisa = new NpgsqlCommand(sqlActualizarDivisa, conn);
            cmdDivisa.Parameters.AddWithValue("idComercio", _idComercio);
            cmdDivisa.Parameters.AddWithValue("idLocal", _idLocal);
            cmdDivisa.Parameters.AddWithValue("idUsuario", _idUsuario);
            cmdDivisa.Parameters.AddWithValue("codigoDivisa", DivisaDepositoSeleccionada);
            cmdDivisa.Parameters.AddWithValue("nombreDivisa", ObtenerNombreDivisa(DivisaDepositoSeleccionada));
            cmdDivisa.Parameters.AddWithValue("cantidadDivisa", cantidadDivisa);
            cmdDivisa.Parameters.AddWithValue("cantidadEuros", eurosRecibidos);
            cmdDivisa.Parameters.AddWithValue("fecha", ahora);
            
            await cmdDivisa.ExecuteNonQueryAsync();
            
            // Recargar datos
            await CargarDatosAsync();
            
            CantidadDivisaDeposito = "";
            CantidadEurosDeposito = "";
            SuccessMessage = "Deposito registrado correctamente";
            
            await Task.Delay(2000);
            SuccessMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task TransferirCajaAsync()
    {
        if (string.IsNullOrWhiteSpace(CantidadTraspaso))
        {
            ErrorMessage = "Ingrese la cantidad a transferir";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (!decimal.TryParse(CantidadTraspaso.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidad))
        {
            ErrorMessage = "La cantidad debe ser un numero valido";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (cantidad <= 0)
        {
            ErrorMessage = "La cantidad debe ser mayor a cero";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        // Verificar que no supere el T.Euros disponible
        if (cantidad > _totalEurosNumerico)
        {
            ErrorMessage = $"No puede transferir mas de {_totalEurosNumerico:N2} EUR";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        try
        {
            IsLoading = true;
            ErrorMessage = "";
            
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            var ahora = ObtenerHoraEspana();
            
            // Insertar en balance_cuentas
            var sqlTraspaso = @"INSERT INTO balance_cuentas 
                               (id_comercio, id_local, codigo_local, id_usuario, 
                                tipo_movimiento, modulo, descripcion, divisa, monto,
                                fecha_movimiento)
                               VALUES 
                               (@idComercio, @idLocal, @codigoLocal, @idUsuario,
                                'TRASPASO', 'DIVISAS', 'Traspaso', 'EUR', @monto,
                                @fecha)";
            
            await using var cmd = new NpgsqlCommand(sqlTraspaso, conn);
            cmd.Parameters.AddWithValue("idComercio", _idComercio);
            cmd.Parameters.AddWithValue("idLocal", _idLocal);
            cmd.Parameters.AddWithValue("codigoLocal", _codigoLocal);
            cmd.Parameters.AddWithValue("idUsuario", _idUsuario);
            cmd.Parameters.AddWithValue("monto", cantidad);
            cmd.Parameters.AddWithValue("fecha", ahora);
            
            await cmd.ExecuteNonQueryAsync();
            
            // Recargar datos
            await CargarDatosAsync();
            
            CantidadTraspaso = "";
            SuccessMessage = "Transferencia registrada correctamente";
            
            await Task.Delay(2000);
            SuccessMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private void Volver()
    {
        // Manejado desde la vista
    }
    
    [RelayCommand]
    private void SeleccionarDivisa(string codigoDivisa)
    {
        if (!string.IsNullOrEmpty(codigoDivisa))
        {
            DivisaDepositoSeleccionada = codigoDivisa;
            // El autocompletado se hace en OnDivisaDepositoSeleccionadaChanged
        }
    }
    
    private string ObtenerNombreDivisa(string codigo)
    {
        return codigo switch
        {
            "USD" => "Dolar USA",
            "EUR" => "Euro",
            "GBP" => "Libra Esterlina",
            "CHF" => "Franco Suizo",
            "CAD" => "Dolar Canadiense",
            "CNY" => "Yuan Chino",
            "PEN" => "Sol Peruano",
            _ => codigo
        };
    }
    
    private async Task CargarOperacionesAsync()
    {
        try
        {
            IsLoading = true;
            Operaciones.Clear();
            
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            var fechaDesde = ParsearFecha(FechaDesdeTexto);
            var fechaHasta = ParsearFecha(FechaHastaTexto);
            
            // Si hay filtro por numero de operacion, obtener rango de fechas de esas operaciones
            DateTime? fechaHoraMinOp = null;
            DateTime? fechaHoraMaxOp = null;
            
            if (!string.IsNullOrWhiteSpace(OperacionDesde) || !string.IsNullOrWhiteSpace(OperacionHasta))
            {
                var sqlRango = @"SELECT 
                                    MIN(fecha_operacion + hora_operacion) as fecha_min,
                                    MAX(fecha_operacion + hora_operacion) as fecha_max
                                FROM operaciones
                                WHERE id_local = @idLocal AND modulo = 'DIVISAS'";
                
                if (!string.IsNullOrWhiteSpace(OperacionDesde))
                    sqlRango += " AND numero_operacion >= @opDesde";
                if (!string.IsNullOrWhiteSpace(OperacionHasta))
                    sqlRango += " AND numero_operacion <= @opHasta";
                
                await using var cmdRango = new NpgsqlCommand(sqlRango, conn);
                cmdRango.Parameters.AddWithValue("idLocal", _idLocal);
                if (!string.IsNullOrWhiteSpace(OperacionDesde))
                    cmdRango.Parameters.AddWithValue("opDesde", OperacionDesde);
                if (!string.IsNullOrWhiteSpace(OperacionHasta))
                    cmdRango.Parameters.AddWithValue("opHasta", OperacionHasta);
                
                await using var readerRango = await cmdRango.ExecuteReaderAsync();
                if (await readerRango.ReadAsync())
                {
                    if (!readerRango.IsDBNull(0))
                        fechaHoraMinOp = readerRango.GetDateTime(0);
                    if (!readerRango.IsDBNull(1))
                        fechaHoraMaxOp = readerRango.GetDateTime(1);
                }
            }
            
            // Cargar todas las operaciones
            await CargarComprasAsync(conn, fechaDesde, fechaHasta);
            await CargarDepositosAsync(conn, fechaDesde, fechaHasta, fechaHoraMinOp, fechaHoraMaxOp);
            await CargarTraspasosAsync(conn, fechaDesde, fechaHasta, fechaHoraMinOp, fechaHoraMaxOp);
            
            // Ordenar por fecha/hora descendente y reasignar colores alternados
            var operacionesOrdenadas = Operaciones.OrderByDescending(o => o.FechaHoraOrden).ToList();
            Operaciones.Clear();
            
            for (int i = 0; i < operacionesOrdenadas.Count; i++)
            {
                operacionesOrdenadas[i].BackgroundColor = i % 2 == 0 ? "White" : "#F5F5F5";
                Operaciones.Add(operacionesOrdenadas[i]);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar operaciones: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task CargarComprasAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var sql = @"SELECT 
                        o.fecha_operacion,
                        o.hora_operacion,
                        o.numero_operacion,
                        od.divisa_origen,
                        od.cantidad_origen,
                        od.cantidad_destino
                    FROM operaciones o
                    INNER JOIN operaciones_divisas od ON o.id_operacion = od.id_operacion
                    WHERE o.id_local = @idLocal
                      AND o.modulo = 'DIVISAS'";
        
        if (fechaDesde.HasValue)
            sql += " AND o.fecha_operacion >= @fechaDesde";
        if (fechaHasta.HasValue)
            sql += " AND o.fecha_operacion <= @fechaHasta";
        
        if (!string.IsNullOrWhiteSpace(OperacionDesde))
            sql += " AND o.numero_operacion >= @opDesde";
        if (!string.IsNullOrWhiteSpace(OperacionHasta))
            sql += " AND o.numero_operacion <= @opHasta";
        
        sql += " LIMIT 100";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("idLocal", _idLocal);
        
        if (fechaDesde.HasValue)
            cmd.Parameters.AddWithValue("fechaDesde", fechaDesde.Value.Date);
        if (fechaHasta.HasValue)
            cmd.Parameters.AddWithValue("fechaHasta", fechaHasta.Value.Date.AddDays(1).AddSeconds(-1));
        if (!string.IsNullOrWhiteSpace(OperacionDesde))
            cmd.Parameters.AddWithValue("opDesde", OperacionDesde);
        if (!string.IsNullOrWhiteSpace(OperacionHasta))
            cmd.Parameters.AddWithValue("opHasta", OperacionHasta);
        
        await using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var fechaDb = reader.GetDateTime(0);
            var hora = reader.IsDBNull(1) ? TimeSpan.Zero : reader.GetTimeSpan(1);
            var numeroOp = reader.IsDBNull(2) ? "" : reader.GetString(2);
            var divisaOrigen = reader.GetString(3);
            var cantidadOrigen = reader.GetDecimal(4);
            var cantidadDestino = reader.GetDecimal(5);
            
            Operaciones.Add(new OperacionItem
            {
                Fecha = fechaDb.ToString("dd/MM/yy"),
                Hora = hora.ToString(@"hh\:mm"),
                NumeroOperacion = numeroOp,
                Descripcion = $"Compra {divisaOrigen}",
                CantidadDivisa = $"{cantidadOrigen:N2}",
                SalidaEuros = $"-{cantidadDestino:N2}",
                SalidaEurosColor = "#CC3333",
                EntradaEuros = "",
                FechaHoraOrden = fechaDb.Date.Add(hora)
            });
        }
    }
    
    private async Task CargarDepositosAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta, DateTime? fechaHoraMinOp = null, DateTime? fechaHoraMaxOp = null)
    {
        var sql = @"SELECT 
                        fecha_movimiento,
                        monto,
                        descripcion,
                        divisa
                    FROM balance_cuentas
                    WHERE id_local = @idLocal 
                    AND tipo_movimiento = 'DEPOSITO'
                    AND modulo = 'DIVISAS'";
        
        // Si hay filtro por operaciones, usar rango de fechas de operaciones
        if (fechaHoraMinOp.HasValue && fechaHoraMaxOp.HasValue)
        {
            sql += " AND fecha_movimiento >= @fechaHoraMin AND fecha_movimiento <= @fechaHoraMax";
        }
        else
        {
            if (fechaDesde.HasValue)
                sql += " AND fecha_movimiento >= @fechaDesde";
            if (fechaHasta.HasValue)
                sql += " AND fecha_movimiento <= @fechaHasta";
        }
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("idLocal", _idLocal);
        
        if (fechaHoraMinOp.HasValue && fechaHoraMaxOp.HasValue)
        {
            cmd.Parameters.AddWithValue("fechaHoraMin", fechaHoraMinOp.Value);
            cmd.Parameters.AddWithValue("fechaHoraMax", fechaHoraMaxOp.Value);
        }
        else
        {
            if (fechaDesde.HasValue)
                cmd.Parameters.AddWithValue("fechaDesde", fechaDesde.Value.Date);
            if (fechaHasta.HasValue)
                cmd.Parameters.AddWithValue("fechaHasta", fechaHasta.Value.Date.AddDays(1));
        }
        
        await using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var fecha = reader.GetDateTime(0);
            var monto = reader.GetDecimal(1);
            var descripcion = reader.IsDBNull(2) ? "Dto Banco" : reader.GetString(2);
            var divisa = reader.IsDBNull(3) ? "" : reader.GetString(3);
            
            // Extraer cantidad de divisa de la descripcion si existe
            // Formato: "Dto Banco: 500.00 USD"
            var cantidadDivisaTexto = "";
            if (descripcion.Contains(":"))
            {
                var partes = descripcion.Split(':');
                if (partes.Length > 1)
                {
                    cantidadDivisaTexto = partes[1].Trim();
                }
            }
            
            Operaciones.Add(new OperacionItem
            {
                Fecha = fecha.ToString("dd/MM/yy"),
                Hora = fecha.ToString("HH:mm"),
                NumeroOperacion = "",
                Descripcion = "Dto Banco",
                CantidadDivisa = cantidadDivisaTexto,
                SalidaEuros = "",
                SalidaEurosColor = "#666666",
                EntradaEuros = $"+{monto:N2}",
                FechaHoraOrden = fecha
            });
        }
    }
    
    private async Task CargarTraspasosAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta, DateTime? fechaHoraMinOp = null, DateTime? fechaHoraMaxOp = null)
    {
        var sql = @"SELECT 
                        fecha_movimiento,
                        monto
                    FROM balance_cuentas
                    WHERE id_local = @idLocal 
                    AND tipo_movimiento = 'TRASPASO'
                    AND modulo = 'DIVISAS'";
        
        // Si hay filtro por operaciones, usar rango de fechas de operaciones
        if (fechaHoraMinOp.HasValue && fechaHoraMaxOp.HasValue)
        {
            sql += " AND fecha_movimiento >= @fechaHoraMin AND fecha_movimiento <= @fechaHoraMax";
        }
        else
        {
            if (fechaDesde.HasValue)
                sql += " AND fecha_movimiento >= @fechaDesde";
            if (fechaHasta.HasValue)
                sql += " AND fecha_movimiento <= @fechaHasta";
        }
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("idLocal", _idLocal);
        
        if (fechaHoraMinOp.HasValue && fechaHoraMaxOp.HasValue)
        {
            cmd.Parameters.AddWithValue("fechaHoraMin", fechaHoraMinOp.Value);
            cmd.Parameters.AddWithValue("fechaHoraMax", fechaHoraMaxOp.Value);
        }
        else
        {
            if (fechaDesde.HasValue)
                cmd.Parameters.AddWithValue("fechaDesde", fechaDesde.Value.Date);
            if (fechaHasta.HasValue)
                cmd.Parameters.AddWithValue("fechaHasta", fechaHasta.Value.Date.AddDays(1));
        }
        
        await using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var fecha = reader.GetDateTime(0);
            var monto = reader.GetDecimal(1);
            
            Operaciones.Add(new OperacionItem
            {
                Fecha = fecha.ToString("dd/MM/yy"),
                Hora = fecha.ToString("HH:mm"),
                NumeroOperacion = "",
                Descripcion = "Traspaso",
                CantidadDivisa = "",
                SalidaEuros = $"{monto:N2}",
                SalidaEurosColor = "#666666",
                EntradaEuros = "",
                FechaHoraOrden = fecha
            });
        }
    }
    
    private async Task CargarDivisasDelLocalAsync()
    {
        try
        {
            DivisasDelLocal.Clear();
            DivisasParaDeposito.Clear();
            
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            // Calcular saldo por divisa: ENTRADA - SALIDA
            var sql = @"SELECT 
                            codigo_divisa,
                            SUM(CASE WHEN tipo_movimiento = 'ENTRADA' THEN cantidad_recibida ELSE 0 END) -
                            SUM(CASE WHEN tipo_movimiento = 'SALIDA' THEN cantidad_recibida ELSE 0 END) as saldo
                        FROM balance_divisas
                        WHERE id_local = @idLocal
                        GROUP BY codigo_divisa
                        HAVING SUM(CASE WHEN tipo_movimiento = 'ENTRADA' THEN cantidad_recibida ELSE 0 END) -
                               SUM(CASE WHEN tipo_movimiento = 'SALIDA' THEN cantidad_recibida ELSE 0 END) > 0
                        ORDER BY codigo_divisa";
            
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("idLocal", _idLocal);
            
            await using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var codigo = reader.GetString(0);
                var cantidad = reader.GetDecimal(1);
                
                DivisasDelLocal.Add(new DivisaLocal
                {
                    Codigo = codigo,
                    Cantidad = cantidad,
                    CantidadFormateada = $"{cantidad:N2}"
                });
                
                DivisasParaDeposito.Add(codigo);
            }
            
            TieneDivisas = DivisasDelLocal.Count > 0;
            
            if (DivisasParaDeposito.Count > 0 && string.IsNullOrEmpty(DivisaDepositoSeleccionada))
            {
                DivisaDepositoSeleccionada = DivisasParaDeposito[0];
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar divisas: {ex.Message}";
        }
    }
    
    private async Task CargarBalancesAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            // S.Euros: Suma de euros que salen (compras de divisa)
            var sqlSalidas = @"SELECT COALESCE(SUM(od.cantidad_destino), 0)
                               FROM operaciones_divisas od
                               INNER JOIN operaciones o ON od.id_operacion = o.id_operacion
                               WHERE o.id_local = @idLocal AND o.modulo = 'DIVISAS'";
            
            await using var cmdSalidas = new NpgsqlCommand(sqlSalidas, conn);
            cmdSalidas.Parameters.AddWithValue("idLocal", _idLocal);
            _salidaEurosNumerico = Convert.ToDecimal(await cmdSalidas.ExecuteScalarAsync() ?? 0);
            
            // E.Euros: Suma de euros que entran (depositos)
            var sqlEntradas = @"SELECT COALESCE(SUM(monto), 0)
                                FROM balance_cuentas 
                                WHERE id_local = @idLocal 
                                  AND tipo_movimiento = 'DEPOSITO'
                                  AND modulo = 'DIVISAS'";
            
            await using var cmdEntradas = new NpgsqlCommand(sqlEntradas, conn);
            cmdEntradas.Parameters.AddWithValue("idLocal", _idLocal);
            _entradaEurosNumerico = Convert.ToDecimal(await cmdEntradas.ExecuteScalarAsync() ?? 0);
            
            // Traspasos: Se restan del T.Euros
            var sqlTraspasos = @"SELECT COALESCE(SUM(monto), 0)
                                 FROM balance_cuentas 
                                 WHERE id_local = @idLocal 
                                   AND tipo_movimiento = 'TRASPASO'
                                   AND modulo = 'DIVISAS'";
            
            await using var cmdTraspasos = new NpgsqlCommand(sqlTraspasos, conn);
            cmdTraspasos.Parameters.AddWithValue("idLocal", _idLocal);
            var traspasos = Convert.ToDecimal(await cmdTraspasos.ExecuteScalarAsync() ?? 0);
            
            // T.Euros = E.Euros - S.Euros - Traspasos
            _totalEurosNumerico = _entradaEurosNumerico - _salidaEurosNumerico - traspasos;
            
            // T.Divisa: Suma de todas las divisas disponibles
            decimal totalDivisas = DivisasDelLocal.Sum(d => d.Cantidad);
            
            // Actualizar UI
            TotalEuros = $"{_totalEurosNumerico:N2}";
            TotalDivisa = $"{totalDivisas:N2}";
            SalidaEurosTotal = $"{_salidaEurosNumerico:N2}";
            EntradaEurosTotal = $"{_entradaEurosNumerico:N2}";
            
            // Color dinamico para T.Euros: verde si positivo, rojo si negativo
            TotalEurosColor = _totalEurosNumerico >= 0 ? "#008800" : "#CC3333";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar balances: {ex.Message}";
        }
    }
}

public class OperacionItem
{
    public string Fecha { get; set; } = "";
    public string Hora { get; set; } = "";
    public string NumeroOperacion { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string CantidadDivisa { get; set; } = "";
    public string SalidaEuros { get; set; } = "";
    public string SalidaEurosColor { get; set; } = "#CC3333";
    public string EntradaEuros { get; set; } = "";
    public string BackgroundColor { get; set; } = "White";
    public DateTime FechaHoraOrden { get; set; } = DateTime.MinValue;
}

public class DivisaLocal
{
    public string Codigo { get; set; } = "";
    public decimal Cantidad { get; set; } = 0;
    public string CantidadFormateada { get; set; } = "0.00";
}