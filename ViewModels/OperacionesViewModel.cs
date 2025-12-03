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
    
    // Fechas en formato dd/mm/aaaa (como NuevoCliente)
    [ObservableProperty]
    private string fechaDesdeTexto = "";
    
    [ObservableProperty]
    private string fechaHastaTexto = "";
    
    [ObservableProperty]
    private string operacionDesde = "";
    
    [ObservableProperty]
    private string operacionHasta = "";
    
    [ObservableProperty]
    private string divisaSeleccionada = "Todas";
    
    [ObservableProperty]
    private string tipoOperacionFiltro = "Todas";
    
    [ObservableProperty]
    private string fechaActualTexto = "";
    
    // Balance SOLO en euros (intercambios negativo, depositos positivo)
    [ObservableProperty]
    private string balanceEuros = "0.00 EUR";
    
    [ObservableProperty]
    private string balanceEurosColor = "#008800";
    
    // Beneficio del dia (se calcula automatico)
    [ObservableProperty]
    private string beneficioDelDia = "0.00 EUR";
    
    private decimal _beneficioDelDiaNumerico = 0;
    private decimal _balanceEurosNumerico = 0;
    
    // Deposito en banco
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
    
    // Cierre del dia
    [ObservableProperty]
    private bool diaCerrado = false;
    
    [ObservableProperty]
    private string mensajeDiaCerrado = "";
    
    public ObservableCollection<OperacionItem> Operaciones { get; } = new();
    public ObservableCollection<string> DivisasDisponibles { get; } = new() { "Todas", "USD", "EUR", "GBP", "CHF", "CAD", "CNY" };
    public ObservableCollection<string> DivisasParaBanco { get; } = new() { "USD", "GBP", "CHF", "CAD", "CNY" };
    
    // Tipos: Compra (intercambio), Deposito (banco), Traspaso a caja (futuro)
    public ObservableCollection<string> TiposOperacion { get; } = new() { "Todas", "Compra", "Deposito", "Traspaso a caja" };
    
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
        FechaActualTexto = FormatearFechaCompleta(hoy);
        
        // Fecha desde: primer dia del mes (formato dd/mm/aaaa)
        FechaDesdeTexto = $"01/{hoy.Month:D2}/{hoy.Year}";
        
        // Fecha hasta: hoy (formato dd/mm/aaaa)
        FechaHastaTexto = $"{hoy.Day:D2}/{hoy.Month:D2}/{hoy.Year}";
    }
    
    private string FormatearFechaCompleta(DateTime fecha)
    {
        var diaSemana = _diasSemana[(int)fecha.DayOfWeek];
        var mes = _mesesEspanol[fecha.Month - 1];
        return $"{diaSemana}, {fecha.Day} de {mes} de {fecha.Year}";
    }
    
    /// <summary>
    /// Parsea fecha en formato dd/mm/aaaa
    /// </summary>
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
    
    private DateTime? ObtenerFechaDesde()
    {
        return ParsearFecha(FechaDesdeTexto);
    }
    
    private DateTime? ObtenerFechaHasta()
    {
        return ParsearFecha(FechaHastaTexto);
    }
    
    private async Task CargarDatosAsync()
    {
        await VerificarDiaCerradoAsync();
        await CargarOperacionesAsync();
        await CargarBalanceEurosAsync();
        await CargarBeneficioDelDiaAsync();
    }
    
    private async Task VerificarDiaCerradoAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            var sql = @"SELECT COUNT(*) FROM cierres_dia 
                        WHERE id_local = @idLocal 
                        AND DATE(fecha_cierre) = CURRENT_DATE";
            
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("idLocal", _idLocal);
            
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            DiaCerrado = count > 0;
            MensajeDiaCerrado = DiaCerrado ? "El dia ya fue cerrado" : "";
        }
        catch
        {
            DiaCerrado = false;
        }
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
        await CargarBalanceEurosAsync();
    }
    
    [RelayCommand]
    private void LimpiarFiltros()
    {
        var hoy = DateTime.Now;
        FechaDesdeTexto = $"01/{hoy.Month:D2}/{hoy.Year}";
        FechaHastaTexto = $"{hoy.Day:D2}/{hoy.Month:D2}/{hoy.Year}";
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
            
            var numeroOperacion = await ObtenerSiguienteNumeroOperacionAsync(conn, _idLocal, "DIR");
            
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
            cmdBalance.Parameters.AddWithValue("observaciones", $"Deposito {DivisaBancoSeleccionada} - {numeroOperacion}");
            
            await cmdBalance.ExecuteNonQueryAsync();
            
            await CargarDatosAsync();
            
            CantidadDivisaDepositar = "";
            CantidadEurosRecibidos = "";
            SuccessMessage = $"Deposito registrado - {numeroOperacion}";
            
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
            
            if (resultado == null)
            {
                nuevoCorrelativo = 1;
                var sqlInsertar = @"INSERT INTO correlativos_operaciones (id_local, prefijo, ultimo_correlativo, fecha_ultimo_uso)
                                    VALUES (@idLocal, @prefijo, @correlativo, @fecha)";
                await using var cmdInsertar = new NpgsqlCommand(sqlInsertar, conn, transaction);
                cmdInsertar.Parameters.AddWithValue("idLocal", idLocal);
                cmdInsertar.Parameters.AddWithValue("prefijo", prefijo);
                cmdInsertar.Parameters.AddWithValue("correlativo", nuevoCorrelativo);
                cmdInsertar.Parameters.AddWithValue("fecha", DateTime.Now);
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
                cmdActualizar.Parameters.AddWithValue("fecha", DateTime.Now);
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
    
    [RelayCommand]
    private async Task RetirarBeneficioAsync()
    {
        if (DiaCerrado)
        {
            ErrorMessage = "El dia ya fue cerrado. No se puede retirar beneficio nuevamente.";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
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
            
            await using var transaction = await conn.BeginTransactionAsync();
            
            try
            {
                // Obtener numero correlativo RET
                var sqlVerificarCorrelativo = @"SELECT ultimo_correlativo FROM correlativos_operaciones 
                                                WHERE id_local = @idLocal AND prefijo = 'RET'
                                                FOR UPDATE";
                
                await using var cmdVerificar = new NpgsqlCommand(sqlVerificarCorrelativo, conn, transaction);
                cmdVerificar.Parameters.AddWithValue("idLocal", _idLocal);
                
                var resultadoCorrelativo = await cmdVerificar.ExecuteScalarAsync();
                int nuevoCorrelativo;
                
                if (resultadoCorrelativo == null)
                {
                    nuevoCorrelativo = 1;
                    var sqlInsertarCorrelativo = @"INSERT INTO correlativos_operaciones (id_local, prefijo, ultimo_correlativo, fecha_ultimo_uso)
                                                   VALUES (@idLocal, 'RET', @correlativo, @fecha)";
                    await using var cmdInsertarCorr = new NpgsqlCommand(sqlInsertarCorrelativo, conn, transaction);
                    cmdInsertarCorr.Parameters.AddWithValue("idLocal", _idLocal);
                    cmdInsertarCorr.Parameters.AddWithValue("correlativo", nuevoCorrelativo);
                    cmdInsertarCorr.Parameters.AddWithValue("fecha", DateTime.Now);
                    await cmdInsertarCorr.ExecuteNonQueryAsync();
                }
                else
                {
                    nuevoCorrelativo = Convert.ToInt32(resultadoCorrelativo) + 1;
                    var sqlActualizarCorrelativo = @"UPDATE correlativos_operaciones 
                                                     SET ultimo_correlativo = @correlativo, fecha_ultimo_uso = @fecha
                                                     WHERE id_local = @idLocal AND prefijo = 'RET'";
                    await using var cmdActualizarCorr = new NpgsqlCommand(sqlActualizarCorrelativo, conn, transaction);
                    cmdActualizarCorr.Parameters.AddWithValue("idLocal", _idLocal);
                    cmdActualizarCorr.Parameters.AddWithValue("correlativo", nuevoCorrelativo);
                    cmdActualizarCorr.Parameters.AddWithValue("fecha", DateTime.Now);
                    await cmdActualizarCorr.ExecuteNonQueryAsync();
                }
                
                var numeroRetiro = $"RET{nuevoCorrelativo:D4}";
                
                // Registrar cierre del dia (solo para este local)
                var sqlCierre = @"INSERT INTO cierres_dia 
                                  (id_comercio, id_local, fecha_cierre, beneficio_dia, balance_euros, observaciones)
                                  VALUES 
                                  (@idComercio, @idLocal, @fecha, @beneficio, @balance, @obs)";
                
                await using var cmdCierre = new NpgsqlCommand(sqlCierre, conn, transaction);
                cmdCierre.Parameters.AddWithValue("idComercio", _idComercio);
                cmdCierre.Parameters.AddWithValue("idLocal", _idLocal);
                cmdCierre.Parameters.AddWithValue("fecha", DateTime.Now);
                cmdCierre.Parameters.AddWithValue("beneficio", _beneficioDelDiaNumerico);
                cmdCierre.Parameters.AddWithValue("balance", _balanceEurosNumerico);
                cmdCierre.Parameters.AddWithValue("obs", $"Cierre del dia {DateTime.Now:dd/MM/yyyy} - {numeroRetiro}");
                
                await cmdCierre.ExecuteNonQueryAsync();
                
                await transaction.CommitAsync();
                
                DiaCerrado = true;
                MensajeDiaCerrado = "El dia ya fue cerrado";
                
                await CargarDatosAsync();
                
                SuccessMessage = $"Dia cerrado. Beneficio de {_beneficioDelDiaNumerico:N2} EUR retirado - {numeroRetiro}";
                await Task.Delay(3000);
                SuccessMessage = "";
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
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
            
            // Cargar Compras (intercambios de divisa)
            if (TipoOperacionFiltro == "Todas" || TipoOperacionFiltro == "Compra")
            {
                rowIndex = await CargarComprasAsync(conn, fechaDesde, fechaHasta, rowIndex);
            }
            
            // Cargar Depositos (cambios en banco)
            if (TipoOperacionFiltro == "Todas" || TipoOperacionFiltro == "Deposito")
            {
                rowIndex = await CargarDepositosAsync(conn, fechaDesde, fechaHasta, rowIndex);
            }
            
            // Traspaso a caja - futuro
            if (TipoOperacionFiltro == "Traspaso a caja")
            {
                // TODO: Implementar cuando exista
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
    
    private async Task<int> CargarComprasAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta, int rowIndex)
    {
        // Columnas correctas segun schema: tipo_cambio, tipo_cambio_aplicado, beneficio
        var sql = @"SELECT 
                        o.fecha_operacion,
                        o.numero_operacion,
                        od.divisa_origen,
                        od.cantidad_origen,
                        od.cantidad_destino,
                        od.tipo_cambio,
                        od.beneficio
                    FROM operaciones o
                    INNER JOIN operaciones_divisas od ON o.id_operacion = od.id_operacion
                    WHERE o.id_local = @idLocal
                      AND o.modulo = 'DIVISAS'";
        
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
        cmd.Parameters.AddWithValue("idLocal", _idLocal);
        
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
            var cantidadDestino = reader.GetDecimal(4); // EUR entregados
            var tipoCambio = reader.IsDBNull(5) ? 0m : reader.GetDecimal(5);
            var beneficio = reader.IsDBNull(6) ? 0m : reader.GetDecimal(6);
            
            var bgColor = rowIndex % 2 == 0 ? "White" : "#F5F5F5";
            
            // Descripcion simplificada: "Compra USD"
            Operaciones.Add(new OperacionItem
            {
                Fecha = fecha.ToString("dd/MM/yy"),
                NumeroOperacion = numeroOp,
                TipoOperacion = "Compra",
                Descripcion = $"Compra {divisaOrigen}",
                CantidadDivisa = $"{cantidadOrigen:N2} {divisaOrigen}",
                EurosEntregados = $"{cantidadDestino:N2}",
                EurosRecibidos = "",
                Beneficio = beneficio > 0 ? $"+{beneficio:N2}" : $"{beneficio:N2}",
                BeneficioColor = beneficio >= 0 ? "#008800" : "#CC3333",
                BackgroundColor = bgColor
            });
            
            rowIndex++;
        }
        
        return rowIndex;
    }
    
    private async Task<int> CargarDepositosAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta, int rowIndex)
    {
        var sql = @"SELECT 
                        fecha_registro,
                        codigo_divisa,
                        cantidad_recibida,
                        cantidad_entregada_eur,
                        observaciones
                    FROM balance_divisas
                    WHERE id_local = @idLocal 
                      AND tipo_movimiento = 'SALIDA'";
        
        if (fechaDesde.HasValue)
            sql += " AND fecha_registro >= @fechaDesde";
        if (fechaHasta.HasValue)
            sql += " AND fecha_registro <= @fechaHasta";
        
        if (!string.IsNullOrEmpty(DivisaSeleccionada) && DivisaSeleccionada != "Todas")
            sql += " AND codigo_divisa = @divisa";
            
        sql += " ORDER BY fecha_registro DESC";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("idLocal", _idLocal);
        
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
            var observaciones = reader.IsDBNull(4) ? "" : reader.GetString(4);
            var bgColor = rowIndex % 2 == 0 ? "White" : "#F5F5F5";
            
            // Extraer numero de operacion de observaciones
            var numeroOp = "";
            if (!string.IsNullOrEmpty(observaciones) && observaciones.Contains("DIR"))
            {
                var inicio = observaciones.IndexOf("DIR");
                if (inicio >= 0)
                {
                    var fin = observaciones.IndexOf(' ', inicio);
                    if (fin < 0) fin = observaciones.Length;
                    numeroOp = observaciones.Substring(inicio, fin - inicio);
                }
            }
            
            // Descripcion simplificada: "Deposito USD"
            Operaciones.Add(new OperacionItem
            {
                Fecha = fecha.ToString("dd/MM/yy"),
                NumeroOperacion = numeroOp,
                TipoOperacion = "Deposito",
                Descripcion = $"Deposito {divisa}",
                CantidadDivisa = $"{cantidadDivisa:N2} {divisa}",
                EurosEntregados = "",
                EurosRecibidos = $"{cantidadEuros:N2}",
                Beneficio = "",
                BeneficioColor = "#666666",
                BackgroundColor = bgColor
            });
            
            rowIndex++;
        }
        
        return rowIndex;
    }
    
    /// <summary>
    /// Balance SOLO en euros: Intercambios = negativo, Depositos = positivo
    /// </summary>
    private async Task CargarBalanceEurosAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            // Depositos (entran euros) = POSITIVO
            var sqlDepositos = @"SELECT COALESCE(SUM(cantidad_entregada_eur), 0)
                                 FROM balance_divisas 
                                 WHERE id_local = @idLocal AND tipo_movimiento = 'SALIDA'";
            
            await using var cmdDepositos = new NpgsqlCommand(sqlDepositos, conn);
            cmdDepositos.Parameters.AddWithValue("idLocal", _idLocal);
            var totalDepositos = Convert.ToDecimal(await cmdDepositos.ExecuteScalarAsync() ?? 0);
            
            // Compras/Intercambios (salen euros) = NEGATIVO
            var sqlCompras = @"SELECT COALESCE(SUM(od.cantidad_destino), 0)
                               FROM operaciones_divisas od
                               INNER JOIN operaciones o ON od.id_operacion = o.id_operacion
                               WHERE o.id_local = @idLocal AND o.modulo = 'DIVISAS'";
            
            await using var cmdCompras = new NpgsqlCommand(sqlCompras, conn);
            cmdCompras.Parameters.AddWithValue("idLocal", _idLocal);
            var totalCompras = Convert.ToDecimal(await cmdCompras.ExecuteScalarAsync() ?? 0);
            
            // Balance = Depositos - Compras
            _balanceEurosNumerico = totalDepositos - totalCompras;
            
            if (_balanceEurosNumerico >= 0)
            {
                BalanceEuros = $"+{_balanceEurosNumerico:N2} EUR";
                BalanceEurosColor = "#008800";
            }
            else
            {
                BalanceEuros = $"{_balanceEurosNumerico:N2} EUR";
                BalanceEurosColor = "#CC3333";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar balance: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Beneficio del dia: usa el campo beneficio de operaciones_divisas (solo operaciones de hoy)
    /// </summary>
    private async Task CargarBeneficioDelDiaAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            // Usar el campo beneficio directamente de la tabla
            var sql = @"SELECT COALESCE(SUM(od.beneficio), 0)
                        FROM operaciones_divisas od
                        INNER JOIN operaciones o ON od.id_operacion = o.id_operacion
                        WHERE o.id_local = @idLocal
                          AND o.modulo = 'DIVISAS'
                          AND DATE(o.fecha_operacion) = CURRENT_DATE";
            
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("idLocal", _idLocal);
            _beneficioDelDiaNumerico = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);
            
            BeneficioDelDia = _beneficioDelDiaNumerico > 0 
                ? $"{_beneficioDelDiaNumerico:N2} EUR" 
                : "0.00 EUR";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar beneficio: {ex.Message}";
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