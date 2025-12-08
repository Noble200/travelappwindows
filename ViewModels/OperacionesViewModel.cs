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

public partial class OperacionesViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";
    
    private readonly int _idComercio;
    private readonly int _idLocal;
    private readonly string _codigoLocal;
    private readonly int _idUsuario;
    private readonly string _nombreUsuario;
    
    private static readonly TimeZoneInfo _zonaHorariaEspana = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Romance Standard Time" : "Europe/Madrid");
    
    [ObservableProperty]
    private string localInfo = "";
    
    [ObservableProperty]
    private string panelActual = "divisa";
    
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
    
    [ObservableProperty]
    private string totalOperaciones = "0";
    
    [ObservableProperty]
    private string totalEurosMovidos = "0.00";
    
    [ObservableProperty]
    private string totalDivisasMovidas = "0.00";
    
    [ObservableProperty]
    private bool isLoading = false;
    
    [ObservableProperty]
    private string errorMessage = "";
    
    [ObservableProperty]
    private string successMessage = "";
    
    public ObservableCollection<OperacionDetalleItem> Operaciones { get; } = new();
    
    private readonly string[] _mesesEspanol = { 
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" 
    };
    
    private readonly string[] _diasSemana = {
        "domingo", "lunes", "martes", "miercoles", "jueves", "viernes", "sabado"
    };
    
    public OperacionesViewModel()
    {
        _idComercio = 0;
        _idLocal = 0;
        _codigoLocal = "---";
        _idUsuario = 0;
        _nombreUsuario = "Usuario";
        LocalInfo = $"(Oficina - {_codigoLocal})";
        InicializarFechas();
    }
    
    public OperacionesViewModel(int idComercio, int idLocal, string codigoLocal)
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
    
    public OperacionesViewModel(int idComercio, int idLocal, string codigoLocal, int idUsuario, string nombreUsuario)
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
    
    private DateTime ObtenerHoraEspana()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zonaHorariaEspana);
    }

    private void InicializarFechas()
    {
        var hoy = ObtenerHoraEspana();
        FechaActualTexto = FormatearFechaCompleta(hoy);
        FechaDesdeTexto = $"01/{hoy.Month:D2}/{hoy.Year}";
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
        ActualizarResumen();
    }
    
    [RelayCommand]
    private void CambiarPanel(string panel)
    {
        PanelActual = panel;
        _ = CargarOperacionesAsync();
    }
    
    [RelayCommand]
    private async Task BuscarAsync()
    {
        await CargarOperacionesAsync();
        ActualizarResumen();
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
            
            var confirmacionVM = new ConfirmacionOperacionesViewModel
            {
                FechaGeneracion = ahora.ToString("dd/MM/yyyy"),
                HoraGeneracion = ahora.ToString("HH:mm:ss"),
                NombreUsuario = _nombreUsuario,
                CodigoLocal = _codigoLocal,
                TotalOperaciones = TotalOperaciones,
                TotalEuros = TotalEurosMovidos,
                TotalDivisas = TotalDivisasMovidas,
                PanelSeleccionado = ObtenerNombrePanel(PanelActual)
            };
            
            if (!string.IsNullOrWhiteSpace(FechaDesdeTexto))
                confirmacionVM.FiltrosAplicados.Add(new FiltroOperacionItem { Nombre = "Fecha desde:", Valor = FechaDesdeTexto });
            if (!string.IsNullOrWhiteSpace(FechaHastaTexto))
                confirmacionVM.FiltrosAplicados.Add(new FiltroOperacionItem { Nombre = "Fecha hasta:", Valor = FechaHastaTexto });
            if (!string.IsNullOrWhiteSpace(OperacionDesde))
                confirmacionVM.FiltrosAplicados.Add(new FiltroOperacionItem { Nombre = "Operacion desde:", Valor = OperacionDesde });
            if (!string.IsNullOrWhiteSpace(OperacionHasta))
                confirmacionVM.FiltrosAplicados.Add(new FiltroOperacionItem { Nombre = "Operacion hasta:", Valor = OperacionHasta });
            
            confirmacionVM.SinFiltros = confirmacionVM.FiltrosAplicados.Count == 0;
            
            var ventanaConfirmacion = new ConfirmacionOperacionesView(confirmacionVM);
            
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
            
            if (ventanaConfirmacion.Confirmado)
            {
                await GenerarPdfOperaciones(ahora);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            await Task.Delay(3000);
            ErrorMessage = "";
        }
    }
    
    private string ObtenerNombrePanel(string panel)
    {
        return panel switch
        {
            "divisa" => "Divisas",
            "billetes" => "Billetes de Avion",
            "viaje" => "Pack de Viaje",
            "alimentos" => "Pack de Alimentos",
            _ => "Todos"
        };
    }
    
    private async Task GenerarPdfOperaciones(DateTime fechaHora)
    {
        try
        {
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
            
            var timestamp = fechaHora.ToString("yyyyMMdd_HHmmss");
            var nombreSugerido = $"Operaciones_{_codigoLocal}_{timestamp}.pdf";
            
            var storageProvider = mainWindow.StorageProvider;
            
            var archivo = await storageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Guardar historial de operaciones",
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
            
            if (archivo == null) return;
            
            IsLoading = true;
            ErrorMessage = "";
            
            var datosReporte = new OperacionesPdfService.DatosReporteOperaciones
            {
                CodigoLocal = _codigoLocal,
                NombreUsuario = _nombreUsuario,
                FechaGeneracion = fechaHora.ToString("dd/MM/yyyy"),
                HoraGeneracion = fechaHora.ToString("HH:mm:ss"),
                PanelSeleccionado = ObtenerNombrePanel(PanelActual),
                TotalOperaciones = Operaciones.Count,
                TotalEuros = Operaciones.Sum(o => o.CantidadPagadaNum),
                TotalDivisas = Operaciones.Sum(o => o.CantidadDivisaNum),
                Filtros = new OperacionesPdfService.FiltrosReporte
                {
                    FechaDesde = FechaDesdeTexto,
                    FechaHasta = FechaHastaTexto,
                    OperacionDesde = OperacionDesde,
                    OperacionHasta = OperacionHasta
                }
            };
            
            foreach (var op in Operaciones)
            {
                datosReporte.Operaciones.Add(new OperacionesPdfService.OperacionDetalle
                {
                    Hora = op.Hora,
                    Fecha = op.Fecha,
                    NumeroOperacion = op.NumeroOperacion,
                    Usuario = op.Usuario,
                    Descripcion = op.Descripcion,
                    CantidadDivisa = op.CantidadDivisa,
                    CantidadPagada = op.CantidadPagada,
                    Cliente = op.Cliente,
                    TipoDocumento = op.TipoDocumento,
                    NumeroDocumento = op.NumeroDocumento
                });
            }
            
            var pdfBytes = OperacionesPdfService.GenerarPdf(datosReporte);
            
            await using var stream = await archivo.OpenWriteAsync();
            await stream.WriteAsync(pdfBytes);
            
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
    
    [RelayCommand]
    private void Volver()
    {
    }
    
    private void ActualizarResumen()
    {
        TotalOperaciones = Operaciones.Count.ToString();
        TotalEurosMovidos = Operaciones.Sum(o => o.CantidadPagadaNum).ToString("N2");
        TotalDivisasMovidas = Operaciones.Sum(o => o.CantidadDivisaNum).ToString("N2");
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
            
            if (PanelActual == "divisa")
            {
                await CargarOperacionesDivisasAsync(conn, fechaDesde, fechaHasta);
                await CargarDepositosBancoAsync(conn, fechaDesde, fechaHasta);
                await CargarTraspasosCajaAsync(conn, fechaDesde, fechaHasta);
            }
            
            var operacionesOrdenadas = Operaciones.OrderByDescending(o => o.FechaHoraOrden).ToList();
            Operaciones.Clear();
            
            for (int i = 0; i < operacionesOrdenadas.Count; i++)
            {
                operacionesOrdenadas[i].BackgroundColor = i % 2 == 0 ? "White" : "#F5F5F5";
                Operaciones.Add(operacionesOrdenadas[i]);
            }
            
            ActualizarResumen();
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
    
    private async Task CargarOperacionesDivisasAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var sql = @"SELECT 
                        o.fecha_operacion,
                        o.hora_operacion,
                        o.numero_operacion,
                        u.nombre,
                        od.divisa_origen,
                        od.cantidad_origen,
                        od.cantidad_destino,
                        c.nombre,
                        c.apellidos,
                        c.documento_tipo,
                        c.documento_numero
                    FROM operaciones o
                    INNER JOIN operaciones_divisas od ON o.id_operacion = od.id_operacion
                    LEFT JOIN usuarios u ON o.id_usuario = u.id_usuario
                    LEFT JOIN clientes c ON o.id_cliente = c.id_cliente
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
        
        sql += " ORDER BY o.fecha_operacion DESC, o.hora_operacion DESC LIMIT 500";
        
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
            var usuario = reader.IsDBNull(3) ? "" : reader.GetString(3);
            var divisaOrigen = reader.GetString(4);
            var cantidadOrigen = reader.GetDecimal(5);
            var cantidadDestino = reader.GetDecimal(6);
            var nombreCliente = reader.IsDBNull(7) ? "" : reader.GetString(7);
            var apellidosCliente = reader.IsDBNull(8) ? "" : reader.GetString(8);
            var tipoDoc = reader.IsDBNull(9) ? "" : reader.GetString(9);
            var numDoc = reader.IsDBNull(10) ? "" : reader.GetString(10);
            
            var clienteNombre = $"{nombreCliente} {apellidosCliente}".Trim();
            if (string.IsNullOrWhiteSpace(clienteNombre)) clienteNombre = "-";
            
            Operaciones.Add(new OperacionDetalleItem
            {
                Hora = hora.ToString(@"hh\:mm"),
                Fecha = fechaDb.ToString("dd/MM/yy"),
                NumeroOperacion = numeroOp,
                Usuario = usuario,
                Descripcion = $"Compra {divisaOrigen}",
                CantidadDivisa = $"{cantidadOrigen:N2}",
                CantidadDivisaNum = cantidadOrigen,
                CantidadPagada = $"{cantidadDestino:N2}",
                CantidadPagadaNum = cantidadDestino,
                Cliente = clienteNombre,
                TipoDocumento = string.IsNullOrWhiteSpace(tipoDoc) ? "-" : tipoDoc,
                NumeroDocumento = string.IsNullOrWhiteSpace(numDoc) ? "-" : numDoc,
                FechaHoraOrden = fechaDb.Date.Add(hora)
            });
        }
    }
    
    private async Task CargarDepositosBancoAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var sql = @"SELECT 
                        bc.fecha_movimiento,
                        bc.monto,
                        bc.descripcion,
                        bc.divisa,
                        u.nombre
                    FROM balance_cuentas bc
                    LEFT JOIN usuarios u ON bc.id_usuario = u.id_usuario
                    WHERE bc.id_local = @idLocal 
                    AND bc.tipo_movimiento = 'DEPOSITO'
                    AND bc.modulo = 'DIVISAS'";
        
        if (fechaDesde.HasValue)
            sql += " AND bc.fecha_movimiento >= @fechaDesde";
        if (fechaHasta.HasValue)
            sql += " AND bc.fecha_movimiento <= @fechaHasta";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("idLocal", _idLocal);
        
        if (fechaDesde.HasValue)
            cmd.Parameters.AddWithValue("fechaDesde", fechaDesde.Value.Date);
        if (fechaHasta.HasValue)
            cmd.Parameters.AddWithValue("fechaHasta", fechaHasta.Value.Date.AddDays(1));
        
        await using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var fecha = reader.GetDateTime(0);
            var monto = reader.GetDecimal(1);
            var descripcion = reader.IsDBNull(2) ? "Deposito Banco" : reader.GetString(2);
            var divisa = reader.IsDBNull(3) ? "" : reader.GetString(3);
            var usuario = reader.IsDBNull(4) ? "" : reader.GetString(4);
            
            var cantidadDivisaTexto = "-";
            decimal cantidadDivisaNum = 0;
            if (descripcion.Contains(":"))
            {
                var partes = descripcion.Split(':');
                if (partes.Length > 1)
                {
                    cantidadDivisaTexto = partes[1].Trim();
                    var numParte = cantidadDivisaTexto.Split(' ')[0];
                    decimal.TryParse(numParte.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out cantidadDivisaNum);
                }
            }
            
            Operaciones.Add(new OperacionDetalleItem
            {
                Hora = fecha.ToString("HH:mm"),
                Fecha = fecha.ToString("dd/MM/yy"),
                NumeroOperacion = "-",
                Usuario = usuario,
                Descripcion = "Deposito Banco",
                CantidadDivisa = cantidadDivisaTexto,
                CantidadDivisaNum = cantidadDivisaNum,
                CantidadPagada = $"+{monto:N2}",
                CantidadPagadaNum = monto,
                Cliente = "-",
                TipoDocumento = "-",
                NumeroDocumento = "-",
                FechaHoraOrden = fecha
            });
        }
    }
    
    private async Task CargarTraspasosCajaAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var sql = @"SELECT 
                        bc.fecha_movimiento,
                        bc.monto,
                        u.nombre
                    FROM balance_cuentas bc
                    LEFT JOIN usuarios u ON bc.id_usuario = u.id_usuario
                    WHERE bc.id_local = @idLocal 
                    AND bc.tipo_movimiento = 'TRASPASO'
                    AND bc.modulo = 'DIVISAS'";
        
        if (fechaDesde.HasValue)
            sql += " AND bc.fecha_movimiento >= @fechaDesde";
        if (fechaHasta.HasValue)
            sql += " AND bc.fecha_movimiento <= @fechaHasta";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("idLocal", _idLocal);
        
        if (fechaDesde.HasValue)
            cmd.Parameters.AddWithValue("fechaDesde", fechaDesde.Value.Date);
        if (fechaHasta.HasValue)
            cmd.Parameters.AddWithValue("fechaHasta", fechaHasta.Value.Date.AddDays(1));
        
        await using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var fecha = reader.GetDateTime(0);
            var monto = reader.GetDecimal(1);
            var usuario = reader.IsDBNull(2) ? "" : reader.GetString(2);
            
            Operaciones.Add(new OperacionDetalleItem
            {
                Hora = fecha.ToString("HH:mm"),
                Fecha = fecha.ToString("dd/MM/yy"),
                NumeroOperacion = "-",
                Usuario = usuario,
                Descripcion = "Traspaso a Caja",
                CantidadDivisa = "-",
                CantidadDivisaNum = 0,
                CantidadPagada = $"{monto:N2}",
                CantidadPagadaNum = monto,
                Cliente = "-",
                TipoDocumento = "-",
                NumeroDocumento = "-",
                FechaHoraOrden = fecha
            });
        }
    }
}

public class OperacionDetalleItem
{
    public string Hora { get; set; } = "";
    public string Fecha { get; set; } = "";
    public string NumeroOperacion { get; set; } = "";
    public string Usuario { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string CantidadDivisa { get; set; } = "";
    public decimal CantidadDivisaNum { get; set; } = 0;
    public string CantidadPagada { get; set; } = "";
    public decimal CantidadPagadaNum { get; set; } = 0;
    public string Cliente { get; set; } = "";
    public string TipoDocumento { get; set; } = "";
    public string NumeroDocumento { get; set; } = "";
    public string BackgroundColor { get; set; } = "White";
    public DateTime FechaHoraOrden { get; set; } = DateTime.MinValue;
}