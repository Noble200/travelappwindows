using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Allva.Desktop.ViewModels;

public partial class OperacionesViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";
    
    private readonly int _idComercio;
    private readonly int _idLocal;
    private readonly string _codigoLocal;
    
    [ObservableProperty]
    private string localInfo = "";
    
    [ObservableProperty]
    private string tabActual = "divisa";
    
    [ObservableProperty]
    private bool mostrarFiltroDivisa = true;
    
    // Filtros de fecha escritos
    [ObservableProperty]
    private string fechaDesdeDia = "";
    
    [ObservableProperty]
    private string fechaDesdeMes = "";
    
    [ObservableProperty]
    private string fechaDesdeAnio = "";
    
    [ObservableProperty]
    private string fechaHastaDia = "";
    
    [ObservableProperty]
    private string fechaHastaMes = "";
    
    [ObservableProperty]
    private string fechaHastaAnio = "";
    
    [ObservableProperty]
    private string operacionDesde = "";
    
    [ObservableProperty]
    private string operacionHasta = "";
    
    [ObservableProperty]
    private string divisaSeleccionada = "Todas";
    
    [ObservableProperty]
    private string tipoOperacionFiltro = "Todas";
    
    // Fecha actual formateada
    [ObservableProperty]
    private string fechaActualTexto = "";
    
    // Balances
    [ObservableProperty]
    private string balanceDivisa = "0$";
    
    [ObservableProperty]
    private string balanceEuro = "0€";
    
    [ObservableProperty]
    private string balanceBeneficio = "0€";
    
    [ObservableProperty]
    private string beneficioDelDia = "0€";
    
    private decimal _beneficioDelDiaNumerico = 0;
    
    // Transaccion banco
    [ObservableProperty]
    private string divisaBancoSeleccionada = "USD";
    
    [ObservableProperty]
    private string cantidadDivisaDepositar = "";
    
    [ObservableProperty]
    private string cantidadEurosRecibidos = "";
    
    [ObservableProperty]
    private bool isLoading = false;
    
    [ObservableProperty]
    private string errorMessage = "";
    
    [ObservableProperty]
    private string successMessage = "";
    
    public ObservableCollection<OperacionItem> Operaciones { get; } = new();
    public ObservableCollection<string> DivisasDisponibles { get; } = new() { "Todas", "USD", "EUR", "GBP", "CHF", "CAD", "CNY" };
    public ObservableCollection<string> DivisasParaBanco { get; } = new() { "USD", "GBP", "CHF", "CAD", "CNY" };
    public ObservableCollection<string> TiposOperacion { get; } = new() { "Todas", "Compras", "Depositos" };
    
    // Nombres de meses en español
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
        LocalInfo = $"(Oficina - {_codigoLocal})";
        InicializarFechas();
    }
    
    public OperacionesViewModel(int idComercio, int idLocal, string codigoLocal)
    {
        _idComercio = idComercio;
        _idLocal = idLocal;
        _codigoLocal = codigoLocal;
        LocalInfo = $"(Oficina - {_codigoLocal})";
        
        InicializarFechas();
        _ = CargarDatosAsync();
    }
    
    private void InicializarFechas()
    {
        var hoy = DateTime.Now;
        
        // Fecha actual formateada
        FechaActualTexto = FormatearFechaCompleta(hoy);
        
        // Fecha desde: primer dia del mes
        FechaDesdeDia = "1";
        FechaDesdeMes = hoy.Month.ToString();
        FechaDesdeAnio = hoy.Year.ToString();
        
        // Fecha hasta: hoy
        FechaHastaDia = hoy.Day.ToString();
        FechaHastaMes = hoy.Month.ToString();
        FechaHastaAnio = hoy.Year.ToString();
    }
    
    private string FormatearFechaCompleta(DateTime fecha)
    {
        var diaSemana = _diasSemana[(int)fecha.DayOfWeek];
        var mes = _mesesEspanol[fecha.Month - 1];
        return $"{diaSemana}, {fecha.Day} de {mes} de {fecha.Year}";
    }
    
    private DateTime? ObtenerFechaDesde()
    {
        if (int.TryParse(FechaDesdeDia, out int dia) &&
            int.TryParse(FechaDesdeMes, out int mes) &&
            int.TryParse(FechaDesdeAnio, out int anio))
        {
            try
            {
                return new DateTime(anio, mes, dia);
            }
            catch { }
        }
        return null;
    }
    
    private DateTime? ObtenerFechaHasta()
    {
        if (int.TryParse(FechaHastaDia, out int dia) &&
            int.TryParse(FechaHastaMes, out int mes) &&
            int.TryParse(FechaHastaAnio, out int anio))
        {
            try
            {
                return new DateTime(anio, mes, dia);
            }
            catch { }
        }
        return null;
    }
    
    private async Task CargarDatosAsync()
    {
        await CargarOperacionesAsync();
        await CargarBalancesAsync();
    }
    
    [RelayCommand]
    private void CambiarTab(string tab)
    {
        TabActual = tab;
        MostrarFiltroDivisa = tab == "divisa";
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
        var hoy = DateTime.Now;
        FechaDesdeDia = "1";
        FechaDesdeMes = hoy.Month.ToString();
        FechaDesdeAnio = hoy.Year.ToString();
        FechaHastaDia = hoy.Day.ToString();
        FechaHastaMes = hoy.Month.ToString();
        FechaHastaAnio = hoy.Year.ToString();
        OperacionDesde = "";
        OperacionHasta = "";
        DivisaSeleccionada = "Todas";
        TipoOperacionFiltro = "Todas";
    }
    
    [RelayCommand]
    private async Task ImprimirHistorialAsync()
    {
        SuccessMessage = "Funcion de impresion en desarrollo";
        await Task.Delay(2000);
        SuccessMessage = "";
    }
    
    [RelayCommand]
    private async Task InsertarDepositoBancoAsync()
    {
        if (string.IsNullOrWhiteSpace(CantidadDivisaDepositar) || 
            string.IsNullOrWhiteSpace(CantidadEurosRecibidos))
        {
            ErrorMessage = "Complete los campos de cantidad";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (!decimal.TryParse(CantidadDivisaDepositar.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidadDivisa) ||
            !decimal.TryParse(CantidadEurosRecibidos.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidadEuros))
        {
            ErrorMessage = "Las cantidades deben ser numeros validos";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (cantidadDivisa <= 0 || cantidadEuros <= 0)
        {
            ErrorMessage = "Las cantidades deben ser mayores a cero";
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
            
            // Obtener siguiente numero de operacion para el comercio
            var sqlNumero = @"SELECT COALESCE(MAX(
                                CASE WHEN numero_operacion ~ '^[0-9]+$' 
                                     THEN CAST(numero_operacion AS INTEGER) 
                                     ELSE 0 END
                              ), 0) + 1
                              FROM operaciones WHERE id_comercio = @idComercio";
            
            await using var cmdNum = new NpgsqlCommand(sqlNumero, conn);
            cmdNum.Parameters.AddWithValue("idComercio", _idComercio);
            var siguienteNumero = Convert.ToInt64(await cmdNum.ExecuteScalarAsync() ?? 1);
            var numeroOperacion = siguienteNumero.ToString("D6");
            
            // Insertar en balance_divisas como SALIDA (deposito al banco)
            var sqlBalance = @"INSERT INTO balance_divisas 
                              (id_comercio, id_local, codigo_divisa, nombre_divisa,
                               cantidad_recibida, cantidad_entregada_eur,
                               tasa_cambio_momento, tasa_cambio_aplicada,
                               tipo_movimiento, observaciones)
                              VALUES 
                              (@idComercio, @idLocal, @codigoDivisa, @nombreDivisa,
                               @cantidadDivisa, @cantidadEuros,
                               1, 1, 'SALIDA', @observaciones)";
            
            await using var cmdBalance = new NpgsqlCommand(sqlBalance, conn);
            cmdBalance.Parameters.AddWithValue("idComercio", _idComercio);
            cmdBalance.Parameters.AddWithValue("idLocal", _idLocal);
            cmdBalance.Parameters.AddWithValue("codigoDivisa", DivisaBancoSeleccionada);
            cmdBalance.Parameters.AddWithValue("nombreDivisa", ObtenerNombreDivisa(DivisaBancoSeleccionada));
            cmdBalance.Parameters.AddWithValue("cantidadDivisa", cantidadDivisa);
            cmdBalance.Parameters.AddWithValue("cantidadEuros", cantidadEuros);
            cmdBalance.Parameters.AddWithValue("observaciones", $"Deposito banco - Op:{numeroOperacion}");
            
            await cmdBalance.ExecuteNonQueryAsync();
            
            await CargarDatosAsync();
            
            CantidadDivisaDepositar = "";
            CantidadEurosRecibidos = "";
            SuccessMessage = $"Deposito registrado - Operacion {numeroOperacion}";
            
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
    private async Task RetirarBeneficioAsync()
    {
        if (_beneficioDelDiaNumerico <= 0)
        {
            ErrorMessage = "No hay beneficio disponible para retirar hoy";
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
            
            // Registrar retiro de beneficio del dia
            var sql = @"INSERT INTO balance_cuentas 
                        (id_comercio, id_local, tipo_movimiento, monto, divisa, descripcion)
                        VALUES 
                        (@idComercio, @idLocal, 'RETIRO_BENEFICIO', @monto, 'EUR', @descripcion)";
            
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("idComercio", _idComercio);
            cmd.Parameters.AddWithValue("idLocal", _idLocal);
            cmd.Parameters.AddWithValue("monto", _beneficioDelDiaNumerico);
            cmd.Parameters.AddWithValue("descripcion", $"Retiro beneficio del {DateTime.Now:dd/MM/yyyy}");
            
            await cmd.ExecuteNonQueryAsync();
            
            await CargarDatosAsync();
            
            SuccessMessage = $"Beneficio de {_beneficioDelDiaNumerico:N2}€ retirado correctamente";
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
            _ => codigo
        };
    }
    
    private string ObtenerSimboloDivisa(string codigo)
    {
        return codigo switch
        {
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            "CHF" => "Fr",
            "CAD" => "C$",
            "CNY" => "¥",
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
            
            var fechaDesde = ObtenerFechaDesde();
            var fechaHasta = ObtenerFechaHasta();
            
            var rowIndex = 0;
            
            // Cargar compras de divisas (si el filtro lo permite)
            if (TipoOperacionFiltro == "Todas" || TipoOperacionFiltro == "Compras")
            {
                rowIndex = await CargarComprasDivisasAsync(conn, fechaDesde, fechaHasta, rowIndex);
            }
            
            // Cargar depositos bancarios (si el filtro lo permite)
            if (TipoOperacionFiltro == "Todas" || TipoOperacionFiltro == "Depositos")
            {
                await CargarDepositosBancoAsync(conn, fechaDesde, fechaHasta, rowIndex);
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
    
    private async Task<int> CargarComprasDivisasAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta, int rowIndex)
    {
        var sql = @"SELECT 
                        o.fecha_operacion,
                        o.numero_operacion,
                        od.divisa_origen,
                        od.cantidad_origen,
                        od.cantidad_destino,
                        COALESCE(od.beneficio, 0) as beneficio
                    FROM operaciones o
                    INNER JOIN operaciones_divisas od ON o.id_operacion = od.id_operacion
                    WHERE o.id_comercio = @idComercio";
        
        if (fechaDesde.HasValue)
            sql += " AND o.fecha_operacion >= @fechaDesde";
        if (fechaHasta.HasValue)
            sql += " AND o.fecha_operacion <= @fechaHasta";
        
        if (!string.IsNullOrEmpty(DivisaSeleccionada) && DivisaSeleccionada != "Todas")
            sql += " AND od.divisa_origen = @divisa";
        
        if (!string.IsNullOrWhiteSpace(OperacionDesde))
            sql += " AND o.numero_operacion >= @opDesde";
        if (!string.IsNullOrWhiteSpace(OperacionHasta))
            sql += " AND o.numero_operacion <= @opHasta";
        
        sql += " ORDER BY o.fecha_operacion DESC, o.id_operacion DESC LIMIT 100";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("idComercio", _idComercio);
        
        if (fechaDesde.HasValue)
            cmd.Parameters.AddWithValue("fechaDesde", fechaDesde.Value.Date);
        if (fechaHasta.HasValue)
            cmd.Parameters.AddWithValue("fechaHasta", fechaHasta.Value.Date.AddDays(1).AddSeconds(-1));
        if (!string.IsNullOrEmpty(DivisaSeleccionada) && DivisaSeleccionada != "Todas")
            cmd.Parameters.AddWithValue("divisa", DivisaSeleccionada);
        if (!string.IsNullOrWhiteSpace(OperacionDesde))
            cmd.Parameters.AddWithValue("opDesde", OperacionDesde);
        if (!string.IsNullOrWhiteSpace(OperacionHasta))
            cmd.Parameters.AddWithValue("opHasta", OperacionHasta);
        
        await using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var fecha = reader.GetDateTime(0);
            var numeroOp = reader.IsDBNull(1) ? "" : reader.GetString(1);
            var divisaOrigen = reader.GetString(2);
            var cantidadOrigen = reader.GetDecimal(3);
            var cantidadDestino = reader.GetDecimal(4);
            var beneficio = reader.GetDecimal(5);
            
            var simbolo = ObtenerSimboloDivisa(divisaOrigen);
            var bgColor = rowIndex % 2 == 0 ? "White" : "#F5F5F5";
            
            // Una sola fila por operacion de compra
            Operaciones.Add(new OperacionItem
            {
                Fecha = fecha.ToString("dd/MM/yy"),
                NumeroOperacion = numeroOp,
                TipoOperacion = "COMPRA",
                Descripcion = $"Compra {cantidadOrigen:N0}{simbolo}",
                CantidadDivisa = $"+{cantidadOrigen:N0}{simbolo}",
                EurosEntregados = $"-{cantidadDestino:N0}€",
                EurosRecibidos = "",
                Beneficio = beneficio > 0 ? $"+{beneficio:N0}€" : "0€",
                BeneficioColor = beneficio > 0 ? "#008800" : "#666666",
                BackgroundColor = bgColor
            });
            
            rowIndex++;
        }
        
        return rowIndex;
    }
    
    private async Task CargarDepositosBancoAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta, int rowIndex)
    {
        var sql = @"SELECT 
                        fecha_registro,
                        codigo_divisa,
                        cantidad_recibida,
                        cantidad_entregada_eur,
                        observaciones
                    FROM balance_divisas
                    WHERE id_comercio = @idComercio 
                      AND tipo_movimiento = 'SALIDA'";
        
        if (fechaDesde.HasValue)
            sql += " AND fecha_registro >= @fechaDesde";
        if (fechaHasta.HasValue)
            sql += " AND fecha_registro <= @fechaHasta";
        
        if (!string.IsNullOrEmpty(DivisaSeleccionada) && DivisaSeleccionada != "Todas")
            sql += " AND codigo_divisa = @divisa";
            
        sql += " ORDER BY fecha_registro DESC";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("idComercio", _idComercio);
        
        if (fechaDesde.HasValue)
            cmd.Parameters.AddWithValue("fechaDesde", fechaDesde.Value.Date);
        if (fechaHasta.HasValue)
            cmd.Parameters.AddWithValue("fechaHasta", fechaHasta.Value.Date.AddDays(1));
        if (!string.IsNullOrEmpty(DivisaSeleccionada) && DivisaSeleccionada != "Todas")
            cmd.Parameters.AddWithValue("divisa", DivisaSeleccionada);
        
        await using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var fecha = reader.GetDateTime(0);
            var divisa = reader.GetString(1);
            var cantidadDivisa = reader.GetDecimal(2);
            var cantidadEuros = reader.GetDecimal(3);
            var simbolo = ObtenerSimboloDivisa(divisa);
            var bgColor = rowIndex % 2 == 0 ? "White" : "#F5F5F5";
            
            // Deposito banco: sin beneficio, solo intercambio
            Operaciones.Add(new OperacionItem
            {
                Fecha = fecha.ToString("dd/MM/yy"),
                NumeroOperacion = "",
                TipoOperacion = "DEPOSITO",
                Descripcion = $"Deposito {divisa}",
                CantidadDivisa = $"-{cantidadDivisa:N0}{simbolo}",
                EurosEntregados = "",
                EurosRecibidos = $"+{cantidadEuros:N0}€",
                Beneficio = "",
                BeneficioColor = "#666666",
                BackgroundColor = bgColor
            });
            
            rowIndex++;
        }
    }
    
    private async Task CargarBalancesAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            // Balance de divisas (entradas - salidas)
            var sqlDivisas = @"SELECT 
                                  codigo_divisa,
                                  SUM(CASE WHEN tipo_movimiento = 'ENTRADA' THEN cantidad_recibida ELSE 0 END) -
                                  SUM(CASE WHEN tipo_movimiento = 'SALIDA' THEN cantidad_recibida ELSE 0 END) as balance
                               FROM balance_divisas 
                               WHERE id_comercio = @idComercio
                               GROUP BY codigo_divisa";
            
            await using var cmdDivisas = new NpgsqlCommand(sqlDivisas, conn);
            cmdDivisas.Parameters.AddWithValue("idComercio", _idComercio);
            
            decimal totalDivisaUSD = 0;
            await using var readerDiv = await cmdDivisas.ExecuteReaderAsync();
            while (await readerDiv.ReadAsync())
            {
                var divisa = readerDiv.GetString(0);
                var balance = readerDiv.GetDecimal(1);
                if (divisa == "USD")
                    totalDivisaUSD = balance;
            }
            await readerDiv.CloseAsync();
            
            BalanceDivisa = $"{totalDivisaUSD:N0}$";
            
            // Balance de euros (recibidos de depositos)
            var sqlEuros = @"SELECT COALESCE(SUM(cantidad_entregada_eur), 0)
                             FROM balance_divisas 
                             WHERE id_comercio = @idComercio AND tipo_movimiento = 'SALIDA'";
            
            await using var cmdEuros = new NpgsqlCommand(sqlEuros, conn);
            cmdEuros.Parameters.AddWithValue("idComercio", _idComercio);
            var totalEurosRecibidos = Convert.ToDecimal(await cmdEuros.ExecuteScalarAsync() ?? 0);
            
            BalanceEuro = $"{totalEurosRecibidos:N0}€";
            
            // Beneficio total acumulado
            var sqlBeneficioTotal = @"SELECT COALESCE(SUM(COALESCE(od.beneficio, 0)), 0)
                                      FROM operaciones_divisas od
                                      INNER JOIN operaciones o ON od.id_operacion = o.id_operacion
                                      WHERE o.id_comercio = @idComercio";
            
            await using var cmdBeneficio = new NpgsqlCommand(sqlBeneficioTotal, conn);
            cmdBeneficio.Parameters.AddWithValue("idComercio", _idComercio);
            var beneficioTotal = Convert.ToDecimal(await cmdBeneficio.ExecuteScalarAsync() ?? 0);
            
            BalanceBeneficio = $"{beneficioTotal:N0}€";
            
            // Beneficio del dia actual (para retirar)
            var sqlBeneficioDia = @"SELECT COALESCE(SUM(COALESCE(od.beneficio, 0)), 0)
                                    FROM operaciones_divisas od
                                    INNER JOIN operaciones o ON od.id_operacion = o.id_operacion
                                    WHERE o.id_comercio = @idComercio
                                      AND DATE(o.fecha_operacion) = CURRENT_DATE";
            
            await using var cmdBeneficioDia = new NpgsqlCommand(sqlBeneficioDia, conn);
            cmdBeneficioDia.Parameters.AddWithValue("idComercio", _idComercio);
            _beneficioDelDiaNumerico = Convert.ToDecimal(await cmdBeneficioDia.ExecuteScalarAsync() ?? 0);
            
            BeneficioDelDia = _beneficioDelDiaNumerico > 0 ? $"{_beneficioDelDiaNumerico:N0}€" : "0€";
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
    public string NumeroOperacion { get; set; } = "";
    public string TipoOperacion { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string CantidadDivisa { get; set; } = "";
    public string EurosEntregados { get; set; } = "";
    public string EurosRecibidos { get; set; } = "";
    public string Beneficio { get; set; } = "";
    public string BeneficioColor { get; set; } = "#008800";
    public string BackgroundColor { get; set; } = "White";
}