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

    // Evento para volver al inicio
    public event Action? OnVolverAInicio;
    
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
    private string tipoOperacionFiltro = "Todas";

    public ObservableCollection<string> TiposOperacion { get; } = new() 
    { 
        "Todas", 
        "Cambio divisa", 
        "Traspaso a caja", 
        "Deposito en banco" 
    };

    // ============================================
    // FILTROS PACK ALIMENTOS
    // ============================================

    [ObservableProperty]
    private string filtroPaisDestino = "Todos";

    [ObservableProperty]
    private string filtroNumeroOperacionAlimentos = "";

    [ObservableProperty]
    private string filtroEstadoOperacion = "Todos";

    public ObservableCollection<string> PaisesDestinoDisponibles { get; } = new() { "Todos" };

    public ObservableCollection<string> EstadosOperacionDisponibles { get; } = new() 
    { 
        "Todos", 
        "Pendiente", 
        "Pagado", 
        "Enviado", 
        "Anulado" 
    };

    // ============================================
    // PROPIEDADES DE VISIBILIDAD
    // ============================================

    public bool EsPanelDivisa => PanelActual == "divisa";
    public bool EsPanelAlimentos => PanelActual == "alimentos";
    public bool EsPanelBilletes => PanelActual == "billetes";
    public bool EsPanelViaje => PanelActual == "viaje";

    // Colores de tabs
    public string TabDivisaBackground => EsPanelDivisa ? "#ffd966" : "White";
    public string TabDivisaForeground => EsPanelDivisa ? "#0b5394" : "#595959";
    public string TabAlimentosBackground => EsPanelAlimentos ? "#ffd966" : "White";
    public string TabAlimentosForeground => EsPanelAlimentos ? "#0b5394" : "#595959";
    public string TabBilletesBackground => EsPanelBilletes ? "#ffd966" : "White";
    public string TabBilletesForeground => EsPanelBilletes ? "#0b5394" : "#595959";
    public string TabViajeBackground => EsPanelViaje ? "#ffd966" : "White";
    public string TabViajeForeground => EsPanelViaje ? "#0b5394" : "#595959";

    // ============================================
    // COLECCIONES
    // ============================================
    
    [ObservableProperty]
    private string fechaActualTexto = "";
    
    [ObservableProperty]
    private string totalOperaciones = "0";
    
    [ObservableProperty]
    private string totalEurosMovidos = "0.00";
    
    [ObservableProperty]
    private string totalDivisasMovidas = "0.00";

    // Resumen Pack Alimentos
    [ObservableProperty]
    private string totalPendientes = "0";

    [ObservableProperty]
    private string totalPagados = "0";

    [ObservableProperty]
    private string totalEnviados = "0";

    [ObservableProperty]
    private string totalAnulados = "0";

    [ObservableProperty]
    private string totalImporteAlimentos = "0.00";

    [ObservableProperty]
    private string totalImporteAlimentosColor = "#0b5394";
    
    [ObservableProperty]
    private bool isLoading = false;
    
    [ObservableProperty]
    private string errorMessage = "";
    
    [ObservableProperty]
    private string successMessage = "";

    [ObservableProperty]
    private bool mostrarFiltros = true;
    
    public ObservableCollection<OperacionDetalleItem> Operaciones { get; } = new();
    public ObservableCollection<OperacionPackAlimentoItem> OperacionesAlimentos { get; } = new();
    
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
        OnPropertyChanged(nameof(EsPanelDivisa));
        OnPropertyChanged(nameof(EsPanelAlimentos));
        OnPropertyChanged(nameof(EsPanelBilletes));
        OnPropertyChanged(nameof(EsPanelViaje));
        
        // Actualizar colores de tabs
        OnPropertyChanged(nameof(TabDivisaBackground));
        OnPropertyChanged(nameof(TabDivisaForeground));
        OnPropertyChanged(nameof(TabAlimentosBackground));
        OnPropertyChanged(nameof(TabAlimentosForeground));
        OnPropertyChanged(nameof(TabBilletesBackground));
        OnPropertyChanged(nameof(TabBilletesForeground));
        OnPropertyChanged(nameof(TabViajeBackground));
        OnPropertyChanged(nameof(TabViajeForeground));

        if (panel == "alimentos")
        {
            _ = CargarPaisesDestinoAsync();
        }

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
        TipoOperacionFiltro = "Todas";
        FiltroPaisDestino = "Todos";
        FiltroNumeroOperacionAlimentos = "";
        FiltroEstadoOperacion = "Todos";
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
        OnVolverAInicio?.Invoke();
    }
    
    [RelayCommand]
    private void ToggleFiltros()
    {
        MostrarFiltros = !MostrarFiltros;
    }

    private void ActualizarResumen()
    {
        if (PanelActual == "alimentos")
        {
            TotalOperaciones = OperacionesAlimentos.Count.ToString();
            TotalPendientes = OperacionesAlimentos.Count(o => o.EstadoEnvio.ToUpper() == "PENDIENTE").ToString();
            TotalPagados = OperacionesAlimentos.Count(o => o.EstadoEnvio.ToUpper() == "PAGADO").ToString();
            TotalEnviados = OperacionesAlimentos.Count(o => o.EstadoEnvio.ToUpper() == "ENVIADO").ToString();
            TotalAnulados = OperacionesAlimentos.Count(o => o.EstadoEnvio.ToUpper() == "ANULADO").ToString();
            
            // Calcular total como negativo (deuda del local)
            var totalImporte = OperacionesAlimentos.Sum(o => o.Importe);
            var totalNegativo = -totalImporte;
            
            // Formato con signo
            if (totalNegativo < 0)
            {
                TotalImporteAlimentos = totalNegativo.ToString("N2");
                TotalImporteAlimentosColor = "#dc3545"; // Rojo para negativo
            }
            else if (totalNegativo > 0)
            {
                TotalImporteAlimentos = $"+{totalNegativo:N2}";
                TotalImporteAlimentosColor = "#28a745"; // Verde para positivo
            }
            else
            {
                TotalImporteAlimentos = "0.00";
                TotalImporteAlimentosColor = "#0b5394"; // Azul para neutral
            }
        }
        else
        {
            TotalOperaciones = Operaciones.Count.ToString();
            TotalEurosMovidos = Operaciones.Sum(o => o.CantidadPagadaNum).ToString("N2");
            TotalDivisasMovidas = Operaciones.Sum(o => o.CantidadDivisaNum).ToString("N2");
        }
    }
    
    private async Task CargarOperacionesAsync()
    {
        try
        {
            IsLoading = true;
            Operaciones.Clear();
            OperacionesAlimentos.Clear();
            
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            var fechaDesde = ParsearFecha(FechaDesdeTexto);
            var fechaHasta = ParsearFecha(FechaHastaTexto);
            
            if (PanelActual == "divisa")
            {
                if (TipoOperacionFiltro == "Todas" || TipoOperacionFiltro == "Cambio divisa")
                    await CargarOperacionesDivisasAsync(conn, fechaDesde, fechaHasta);
                if (TipoOperacionFiltro == "Todas" || TipoOperacionFiltro == "Deposito en banco")
                    await CargarDepositosBancoAsync(conn, fechaDesde, fechaHasta);
                if (TipoOperacionFiltro == "Todas" || TipoOperacionFiltro == "Traspaso a caja")
                    await CargarTraspasosCajaAsync(conn, fechaDesde, fechaHasta);
                
                var operacionesOrdenadas = Operaciones.OrderByDescending(o => o.FechaHoraOrden).ToList();
                Operaciones.Clear();
                
                for (int i = 0; i < operacionesOrdenadas.Count; i++)
                {
                    operacionesOrdenadas[i].BackgroundColor = i % 2 == 0 ? "White" : "#F5F5F5";
                    Operaciones.Add(operacionesOrdenadas[i]);
                }
            }
            else if (PanelActual == "alimentos")
            {
                await CargarOperacionesPackAlimentosAsync(conn, fechaDesde, fechaHasta);
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

    // ============================================
    // CARGAR PAISES DESTINO PARA FILTRO
    // ============================================

    private async Task CargarPaisesDestinoAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var query = @"
                SELECT DISTINCT opa.pais_destino
                FROM operaciones_pack_alimentos opa
                INNER JOIN operaciones o ON o.id_operacion = opa.id_operacion
                WHERE o.id_local = @idLocal
                  AND opa.pais_destino IS NOT NULL
                  AND opa.pais_destino != ''
                ORDER BY opa.pais_destino";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@idLocal", _idLocal);

            await using var reader = await cmd.ExecuteReaderAsync();

            PaisesDestinoDisponibles.Clear();
            PaisesDestinoDisponibles.Add("Todos");

            while (await reader.ReadAsync())
            {
                var pais = reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(pais))
                    PaisesDestinoDisponibles.Add(pais);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar paises destino: {ex.Message}");
        }
    }

    // ============================================
    // CARGAR OPERACIONES PACK ALIMENTOS
    // ============================================

    private async Task CargarOperacionesPackAlimentosAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var whereConditions = new System.Collections.Generic.List<string>
        {
            "o.id_local = @idLocal",
            "o.modulo = 'PACK_ALIMENTOS'"
        };

        if (fechaDesde.HasValue)
            whereConditions.Add("o.fecha_operacion >= @fechaDesde");

        if (fechaHasta.HasValue)
            whereConditions.Add("o.fecha_operacion <= @fechaHasta");

        if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacionAlimentos))
            whereConditions.Add("o.numero_operacion ILIKE @numeroOp");

        if (FiltroPaisDestino != "Todos" && !string.IsNullOrWhiteSpace(FiltroPaisDestino))
            whereConditions.Add("opa.pais_destino = @paisDestino");

        if (FiltroEstadoOperacion != "Todos" && !string.IsNullOrWhiteSpace(FiltroEstadoOperacion))
            whereConditions.Add("UPPER(opa.estado_envio) = @estadoOperacion");

        var whereClause = string.Join(" AND ", whereConditions);

        var query = $@"
            SELECT 
                o.id_operacion,
                o.numero_operacion,
                o.fecha_operacion,
                o.hora_operacion,
                c.nombre as cliente_nombre,
                c.apellidos as cliente_apellido,
                o.importe_total,
                o.moneda,
                opa.nombre_pack,
                opa.pais_destino,
                opa.ciudad_destino,
                opa.estado_envio,
                cb.nombre as beneficiario_nombre,
                cb.apellido as beneficiario_apellido
            FROM operaciones o
            LEFT JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
            LEFT JOIN clientes_beneficiarios cb ON opa.id_beneficiario = cb.id_beneficiario
            LEFT JOIN clientes c ON o.id_cliente = c.id_cliente
            WHERE {whereClause}
            ORDER BY o.fecha_operacion DESC, o.hora_operacion DESC
            LIMIT 500";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@idLocal", _idLocal);

        if (fechaDesde.HasValue)
            cmd.Parameters.AddWithValue("@fechaDesde", fechaDesde.Value.Date);

        if (fechaHasta.HasValue)
            cmd.Parameters.AddWithValue("@fechaHasta", fechaHasta.Value.Date.AddDays(1).AddSeconds(-1));

        if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacionAlimentos))
            cmd.Parameters.AddWithValue("@numeroOp", $"%{FiltroNumeroOperacionAlimentos}%");

        if (FiltroPaisDestino != "Todos" && !string.IsNullOrWhiteSpace(FiltroPaisDestino))
            cmd.Parameters.AddWithValue("@paisDestino", FiltroPaisDestino);

        if (FiltroEstadoOperacion != "Todos" && !string.IsNullOrWhiteSpace(FiltroEstadoOperacion))
            cmd.Parameters.AddWithValue("@estadoOperacion", FiltroEstadoOperacion.ToUpper());

        await using var reader = await cmd.ExecuteReaderAsync();

        int index = 0;
        while (await reader.ReadAsync())
        {
            var fechaOp = reader.IsDBNull(2) ? DateTime.Today : reader.GetDateTime(2);
            var horaOp = reader.IsDBNull(3) ? TimeSpan.Zero : reader.GetTimeSpan(3);
            var estadoEnvio = reader.IsDBNull(11) ? "PENDIENTE" : reader.GetString(11);

            // Cliente: primer nombre y primer apellido
            var clienteNombre = reader.IsDBNull(4) ? "" : reader.GetString(4);
            var clienteApellido = reader.IsDBNull(5) ? "" : reader.GetString(5);
            var primerNombreCliente = clienteNombre.Split(' ').FirstOrDefault() ?? "";
            var primerApellidoCliente = clienteApellido.Split(' ').FirstOrDefault() ?? "";
            var nombreClienteCompleto = $"{primerNombreCliente} {primerApellidoCliente}".Trim();

            // Beneficiario: primer nombre y primer apellido
            var benefNombre = reader.IsDBNull(12) ? "" : reader.GetString(12);
            var benefApellido = reader.IsDBNull(13) ? "" : reader.GetString(13);
            var primerNombreBenef = benefNombre.Split(' ').FirstOrDefault() ?? "";
            var primerApellidoBenef = benefApellido.Split(' ').FirstOrDefault() ?? "";
            var nombreBenefCompleto = $"{primerNombreBenef} {primerApellidoBenef}".Trim();

            var item = new OperacionPackAlimentoItem
            {
                IdOperacion = reader.GetInt64(0),
                NumeroOperacion = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Fecha = fechaOp.ToString("dd/MM/yy"),
                Hora = horaOp.ToString(@"hh\:mm"),
                NombreCliente = nombreClienteCompleto,
                Importe = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                Moneda = reader.IsDBNull(7) ? "EUR" : reader.GetString(7),
                Descripcion = reader.IsDBNull(8) ? "Pack Alimentos" : reader.GetString(8),
                PaisDestino = reader.IsDBNull(9) ? "" : reader.GetString(9),
                CiudadDestino = reader.IsDBNull(10) ? "" : reader.GetString(10),
                NombreBeneficiario = nombreBenefCompleto,
                EstadoEnvio = estadoEnvio,
                EstadoTexto = ObtenerTextoEstado(estadoEnvio),
                EstadoColor = ObtenerColorEstado(estadoEnvio),
                BackgroundColor = index % 2 == 0 ? "White" : "#F5F5F5",
                FechaHoraOrden = fechaOp.Add(horaOp)
            };

            OperacionesAlimentos.Add(item);
            index++;
        }
    }

    private string ObtenerTextoEstado(string estado)
    {
        return estado.ToUpper() switch
        {
            "PENDIENTE" => "Pendiente",
            "PAGADO" => "Pagado",
            "ENVIADO" => "Enviado",
            "ANULADO" => "Anulado",
            _ => estado
        };
    }

    private string ObtenerColorEstado(string estado)
    {
        return estado.ToUpper() switch
        {
            "PENDIENTE" => "#ffc107",
            "PAGADO" => "#17a2b8",
            "ENVIADO" => "#28a745",
            "ANULADO" => "#dc3545",
            _ => "#6c757d"
        };
    }
    
    private async Task CargarOperacionesDivisasAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var sql = @"SELECT 
                        o.fecha_operacion,
                        o.hora_operacion,
                        o.numero_operacion,
                        u.nombre,
                        u.apellidos,
                        od.divisa_origen,
                        od.cantidad_origen,
                        od.cantidad_destino,
                        c.nombre,
                        c.apellidos,
                        c.documento_tipo,
                        c.documento_numero,
                        c.segundo_nombre,
                        c.segundo_apellido
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
            var usuarioNombre = reader.IsDBNull(3) ? "" : reader.GetString(3);
            var usuarioApellidos = reader.IsDBNull(4) ? "" : reader.GetString(4);
            var divisaOrigen = reader.GetString(5);
            var cantidadOrigen = reader.GetDecimal(6);
            var cantidadDestino = reader.GetDecimal(7);
            var nombreCliente = reader.IsDBNull(8) ? "" : reader.GetString(8);
            var apellidosCliente = reader.IsDBNull(9) ? "" : reader.GetString(9);
            var tipoDoc = reader.IsDBNull(10) ? "" : reader.GetString(10);
            var numDoc = reader.IsDBNull(11) ? "" : reader.GetString(11);
            var segundoNombre = reader.IsDBNull(12) ? "" : reader.GetString(12);
            var segundoApellido = reader.IsDBNull(13) ? "" : reader.GetString(13);

            var usuarioCompleto = $"{usuarioNombre} {usuarioApellidos}".Trim();
            if (string.IsNullOrWhiteSpace(usuarioCompleto)) usuarioCompleto = "-";

            var partesNombreCliente = new[] { nombreCliente, segundoNombre, apellidosCliente, segundoApellido };
            var clienteNombre = string.Join(" ", partesNombreCliente.Where(p => !string.IsNullOrWhiteSpace(p)));
            if (string.IsNullOrWhiteSpace(clienteNombre)) clienteNombre = "-";
            
            Operaciones.Add(new OperacionDetalleItem
            {
                Hora = hora.ToString(@"hh\:mm"),
                Fecha = fechaDb.ToString("dd/MM/yy"),
                NumeroOperacion = numeroOp,
                Usuario = usuarioCompleto,
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

public class OperacionPackAlimentoItem
{
    public long IdOperacion { get; set; }
    public string NumeroOperacion { get; set; } = "";
    public string Fecha { get; set; } = "";
    public string Hora { get; set; } = "";
    public string NombreCliente { get; set; } = "";
    public string NombreBeneficiario { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string PaisDestino { get; set; } = "";
    public string CiudadDestino { get; set; } = "";
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = "EUR";
    public string EstadoEnvio { get; set; } = "";
    public string EstadoTexto { get; set; } = "";
    public string EstadoColor { get; set; } = "#6c757d";
    public string BackgroundColor { get; set; } = "White";
    public DateTime FechaHoraOrden { get; set; }

    public string ImporteFormateado => $"{Importe:N2} {Moneda}";
}