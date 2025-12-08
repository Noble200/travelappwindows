using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models.Admin;
using Npgsql;

namespace Allva.Desktop.ViewModels.Admin;

public partial class ManageDivisasViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    [ObservableProperty]
    private decimal _margenGlobal = 10.00m;
    
    [ObservableProperty]
    private string _margenGlobalTexto = "10.00";

    // Colecciones originales (sin filtrar)
    private ObservableCollection<ComercioConDivisaModel> _comercios = new();
    private ObservableCollection<LocalConDivisaModel> _locales = new();

    // Colecciones filtradas (las que se muestran)
    [ObservableProperty]
    private ObservableCollection<ComercioConDivisaModel> _comerciosFiltrados = new();

    [ObservableProperty]
    private ObservableCollection<LocalConDivisaModel> _localesFiltrados = new();

    [ObservableProperty]
    private ComercioConDivisaModel? _comercioSeleccionado;

    [ObservableProperty]
    private LocalConDivisaModel? _localSeleccionado;

    [ObservableProperty]
    private bool _cargando;

    [ObservableProperty]
    private bool _mostrarMensajeExito;

    [ObservableProperty]
    private string _mensajeExito = string.Empty;

    [ObservableProperty]
    private bool _mostrarPanelEdicion = false;

    [ObservableProperty]
    private string _tituloPanelEdicion = "Editar Margen";

    [ObservableProperty]
    private string _vistaActual = "comercios";

    [ObservableProperty]
    private string _nuevoMargenTexto = "10.00";

    [ObservableProperty]
    private string _tipoEdicion = "GLOBAL";

    [ObservableProperty]
    private string _textoBusqueda = string.Empty;

    // Propiedades para controlar qué vista está activa
    public bool EsVistaComercios => VistaActual == "comercios";
    public bool EsVistaLocales => VistaActual == "locales";

    public ManageDivisasViewModel()
    {
        _ = CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        Cargando = true;

        try
        {
            await CargarMargenGlobalAsync();
            await CargarComerciosAsync();
            await CargarLocalesAsync();
            AplicarFiltro();
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al cargar datos: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    private async Task CargarMargenGlobalAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var checkTableSql = @"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_name = 'configuracion_sistema'
                )";
            
            await using var checkCmd = new NpgsqlCommand(checkTableSql, conn);
            var tableExists = (bool)(await checkCmd.ExecuteScalarAsync() ?? false);
            
            if (!tableExists)
            {
                var createTableSql = @"
                    CREATE TABLE configuracion_sistema (
                        id_config SERIAL PRIMARY KEY,
                        clave VARCHAR(100) UNIQUE NOT NULL,
                        valor_texto TEXT,
                        valor_decimal NUMERIC(10,2),
                        valor_entero INTEGER,
                        valor_booleano BOOLEAN,
                        descripcion TEXT,
                        fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        fecha_modificacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )";
                await using var createCmd = new NpgsqlCommand(createTableSql, conn);
                await createCmd.ExecuteNonQueryAsync();
                
                var insertSql = @"
                    INSERT INTO configuracion_sistema (clave, valor_decimal, descripcion)
                    VALUES ('margen_divisas_global', 10.00, 'Margen global para operaciones de divisas')";
                await using var insertCmd = new NpgsqlCommand(insertSql, conn);
                await insertCmd.ExecuteNonQueryAsync();
            }

            var sql = @"
                SELECT valor_decimal 
                FROM configuracion_sistema 
                WHERE clave = 'margen_divisas_global'
                LIMIT 1";

            await using var cmd = new NpgsqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync();

            if (result != null && result != DBNull.Value)
            {
                MargenGlobal = Convert.ToDecimal(result);
            }
            else
            {
                MargenGlobal = 10.00m;
            }

            MargenGlobalTexto = MargenGlobal.ToString("N2");
        }
        catch
        {
            MargenGlobal = 10.00m;
            MargenGlobalTexto = "10.00";
        }
    }

    private async Task CargarComerciosAsync()
    {
        _comercios.Clear();

        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var checkColumnSql = @"
                SELECT EXISTS (
                    SELECT FROM information_schema.columns 
                    WHERE table_name = 'comercios' 
                    AND column_name = 'porcentaje_comision_divisas'
                )";
            
            await using var checkCmd = new NpgsqlCommand(checkColumnSql, conn);
            var columnExists = (bool)(await checkCmd.ExecuteScalarAsync() ?? false);
            
            if (!columnExists)
            {
                var alterSql = "ALTER TABLE comercios ADD COLUMN porcentaje_comision_divisas NUMERIC(5,2) DEFAULT 0.00";
                await using var alterCmd = new NpgsqlCommand(alterSql, conn);
                await alterCmd.ExecuteNonQueryAsync();
            }

            var sql = @"
                SELECT 
                    c.id_comercio,
                    c.nombre_comercio,
                    c.nombre_srl,
                    c.pais,
                    c.activo,
                    COALESCE(c.porcentaje_comision_divisas, 0) as margen,
                    (SELECT COUNT(*) FROM locales l WHERE l.id_comercio = c.id_comercio) as cantidad_locales
                FROM comercios c
                ORDER BY c.nombre_comercio";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var comercio = new ComercioConDivisaModel
                {
                    IdComercio = reader.GetInt32(0),
                    NombreComercio = reader.GetString(1),
                    NombreSrl = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Pais = reader.GetString(3),
                    Activo = reader.GetBoolean(4),
                    MargenPorcentaje = reader.GetDecimal(5),
                    CantidadLocales = reader.GetInt32(6)
                };

                if (comercio.MargenPorcentaje == 0)
                {
                    comercio.MargenPorcentaje = MargenGlobal;
                }

                _comercios.Add(comercio);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al cargar comercios: {ex.Message}");
        }
    }

    private async Task CargarLocalesAsync()
    {
        _locales.Clear();

        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var checkColumnSql = @"
                SELECT EXISTS (
                    SELECT FROM information_schema.columns 
                    WHERE table_name = 'locales' 
                    AND column_name = 'comision_divisas'
                )";
            
            await using var checkCmd = new NpgsqlCommand(checkColumnSql, conn);
            var columnExists = (bool)(await checkCmd.ExecuteScalarAsync() ?? false);
            
            if (!columnExists)
            {
                var alterSql = "ALTER TABLE locales ADD COLUMN comision_divisas NUMERIC(5,2) DEFAULT 0.00";
                await using var alterCmd = new NpgsqlCommand(alterSql, conn);
                await alterCmd.ExecuteNonQueryAsync();
            }

            var sql = @"
                SELECT 
                    l.id_local,
                    l.id_comercio,
                    l.codigo_local,
                    l.nombre_local,
                    c.nombre_comercio,
                    l.activo,
                    COALESCE(l.comision_divisas, 0) as comision_local,
                    COALESCE(c.porcentaje_comision_divisas, 0) as margen_comercio
                FROM locales l
                INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                ORDER BY c.nombre_comercio, l.nombre_local";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var comisionLocal = reader.GetDecimal(6);
                var margenComercio = reader.GetDecimal(7);

                decimal margenEfectivo;
                string estadoMargen;

                if (comisionLocal > 0)
                {
                    margenEfectivo = comisionLocal;
                    estadoMargen = "Propio";
                }
                else if (margenComercio > 0)
                {
                    margenEfectivo = margenComercio;
                    estadoMargen = "Del comercio";
                }
                else
                {
                    margenEfectivo = MargenGlobal;
                    estadoMargen = "Global";
                }

                var local = new LocalConDivisaModel
                {
                    IdLocal = reader.GetInt32(0),
                    IdComercio = reader.GetInt32(1),
                    CodigoLocal = reader.GetString(2),
                    NombreLocal = reader.GetString(3),
                    NombreComercio = reader.GetString(4),
                    Activo = reader.GetBoolean(5),
                    ComisionDivisas = comisionLocal,
                    UsaComisionPropia = comisionLocal > 0,
                    MargenEfectivo = margenEfectivo,
                    EstadoMargen = estadoMargen
                };

                _locales.Add(local);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al cargar locales: {ex.Message}");
        }
    }

    /// <summary>
    /// Aplica el filtro de búsqueda a la lista correspondiente
    /// Comercios: busca por nombre
    /// Locales: busca por código, o por nombre de comercio (muestra todos los locales del comercio)
    /// </summary>
    private void AplicarFiltro()
    {
        var filtro = TextoBusqueda?.ToLower().Trim() ?? "";

        if (EsVistaComercios)
        {
            // Comercios: buscar solo por nombre
            ComerciosFiltrados.Clear();
            var comerciosFiltrados = string.IsNullOrWhiteSpace(filtro)
                ? _comercios
                : _comercios.Where(c => c.NombreComercio.ToLower().Contains(filtro));

            foreach (var comercio in comerciosFiltrados)
            {
                ComerciosFiltrados.Add(comercio);
            }
        }
        else
        {
            // Locales: buscar por código primero, si no encuentra, buscar por nombre de comercio
            LocalesFiltrados.Clear();
            
            if (string.IsNullOrWhiteSpace(filtro))
            {
                foreach (var local in _locales)
                {
                    LocalesFiltrados.Add(local);
                }
            }
            else
            {
                // Primero buscar por código de local
                var localesPorCodigo = _locales.Where(l => 
                    l.CodigoLocal.ToLower().Contains(filtro)).ToList();
                
                if (localesPorCodigo.Any())
                {
                    // Si encontró por código, mostrar esos
                    foreach (var local in localesPorCodigo)
                    {
                        LocalesFiltrados.Add(local);
                    }
                }
                else
                {
                    // Si no encontró por código, buscar por nombre de comercio
                    // y mostrar TODOS los locales de ese comercio
                    var localesPorComercio = _locales.Where(l => 
                        l.NombreComercio.ToLower().Contains(filtro));
                    
                    foreach (var local in localesPorComercio)
                    {
                        LocalesFiltrados.Add(local);
                    }
                }
            }
        }
    }

    // Se ejecuta cuando cambia el texto de búsqueda
    partial void OnTextoBusquedaChanged(string value)
    {
        AplicarFiltro();
    }

    // Se ejecuta cuando cambia la vista actual
    partial void OnVistaActualChanged(string value)
    {
        OnPropertyChanged(nameof(EsVistaComercios));
        OnPropertyChanged(nameof(EsVistaLocales));
        TextoBusqueda = ""; // Limpiar búsqueda al cambiar de vista
        AplicarFiltro();
    }

    [RelayCommand]
    private void CambiarVista(string? vista)
    {
        if (string.IsNullOrEmpty(vista)) return;
        VistaActual = vista;
        MostrarPanelEdicion = false;
    }

    [RelayCommand]
    private void EditarMargenGlobal()
    {
        TipoEdicion = "GLOBAL";
        TituloPanelEdicion = "Editar Margen Global";
        NuevoMargenTexto = MargenGlobal.ToString("N2");
        ComercioSeleccionado = null;
        LocalSeleccionado = null;
        MostrarPanelEdicion = true;
    }

    [RelayCommand]
    private void SeleccionarComercio(ComercioConDivisaModel? comercio)
    {
        if (comercio == null) return;

        TipoEdicion = "COMERCIO";
        TituloPanelEdicion = $"Editar Margen: {comercio.NombreComercio}";
        NuevoMargenTexto = comercio.MargenPorcentaje.ToString("N2");
        ComercioSeleccionado = comercio;
        LocalSeleccionado = null;
        MostrarPanelEdicion = true;
    }

    [RelayCommand]
    private void SeleccionarLocal(LocalConDivisaModel? local)
    {
        if (local == null) return;

        TipoEdicion = "LOCAL";
        TituloPanelEdicion = $"Editar Margen: {local.NombreLocal}";
        NuevoMargenTexto = local.MargenEfectivo.ToString("N2");
        LocalSeleccionado = local;
        ComercioSeleccionado = null;
        MostrarPanelEdicion = true;
    }

    [RelayCommand]
    private async Task GuardarMargenAsync()
    {
        var textoLimpio = NuevoMargenTexto.Replace(",", ".");
        if (!decimal.TryParse(textoLimpio, System.Globalization.NumberStyles.Any, 
            System.Globalization.CultureInfo.InvariantCulture, out decimal nuevoMargen))
        {
            MostrarMensaje("El margen ingresado no es válido");
            return;
        }

        if (nuevoMargen < 0 || nuevoMargen > 100)
        {
            MostrarMensaje("El margen debe estar entre 0% y 100%");
            return;
        }

        Cargando = true;

        try
        {
            switch (TipoEdicion)
            {
                case "GLOBAL":
                    await GuardarMargenGlobalAsync(nuevoMargen);
                    MostrarMensaje($"Margen global actualizado a {nuevoMargen:N2}%");
                    break;

                case "COMERCIO":
                    await GuardarMargenComercioAsync(nuevoMargen);
                    MostrarMensaje($"Margen del comercio actualizado a {nuevoMargen:N2}%");
                    break;

                case "LOCAL":
                    await GuardarMargenLocalAsync(nuevoMargen);
                    MostrarMensaje($"Margen del local actualizado a {nuevoMargen:N2}%");
                    break;
            }

            await CargarDatosAsync();
            MostrarPanelEdicion = false;
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al guardar: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private async Task AplicarMargenGlobalATodosAsync()
    {
        Cargando = true;

        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sqlComercios = "UPDATE comercios SET porcentaje_comision_divisas = @margen";
            await using (var cmd = new NpgsqlCommand(sqlComercios, conn))
            {
                cmd.Parameters.AddWithValue("margen", MargenGlobal);
                await cmd.ExecuteNonQueryAsync();
            }

            var sqlLocales = "UPDATE locales SET comision_divisas = 0";
            await using (var cmd = new NpgsqlCommand(sqlLocales, conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            await CargarDatosAsync();
            MostrarMensaje($"Margen {MargenGlobal:N2}% aplicado a todos");
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private void CancelarEdicion()
    {
        MostrarPanelEdicion = false;
    }

    [RelayCommand]
    private async Task RefrescarAsync()
    {
        await CargarDatosAsync();
    }

    [RelayCommand]
    private async Task ResetearMargenComercioAsync()
    {
        if (ComercioSeleccionado == null) return;

        Cargando = true;

        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = "UPDATE comercios SET porcentaje_comision_divisas = 0 WHERE id_comercio = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", ComercioSeleccionado.IdComercio);
            await cmd.ExecuteNonQueryAsync();

            await CargarDatosAsync();
            MostrarPanelEdicion = false;
            MostrarMensaje("El comercio ahora usa el margen global");
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private async Task ResetearMargenLocalAsync()
    {
        if (LocalSeleccionado == null) return;

        Cargando = true;

        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = "UPDATE locales SET comision_divisas = 0 WHERE id_local = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", LocalSeleccionado.IdLocal);
            await cmd.ExecuteNonQueryAsync();

            await CargarDatosAsync();
            MostrarPanelEdicion = false;
            MostrarMensaje("El local ahora usa el margen del comercio");
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    private async Task GuardarMargenGlobalAsync(decimal nuevoMargen)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        // Actualizar el valor global en configuracion_sistema
        var sql = @"
            INSERT INTO configuracion_sistema (clave, valor_decimal, descripcion)
            VALUES ('margen_divisas_global', @valor, 'Margen global para operaciones de divisas')
            ON CONFLICT (clave) 
            DO UPDATE SET valor_decimal = @valor, fecha_modificacion = CURRENT_TIMESTAMP";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("valor", nuevoMargen);
        await cmd.ExecuteNonQueryAsync();

        // Resetear todos los comercios para que usen el margen global
        var sqlComercios = "UPDATE comercios SET porcentaje_comision_divisas = 0";
        await using var cmdComercios = new NpgsqlCommand(sqlComercios, conn);
        await cmdComercios.ExecuteNonQueryAsync();

        // Resetear todos los locales para que usen el margen global
        var sqlLocales = "UPDATE locales SET comision_divisas = 0";
        await using var cmdLocales = new NpgsqlCommand(sqlLocales, conn);
        await cmdLocales.ExecuteNonQueryAsync();

        MargenGlobal = nuevoMargen;
        MargenGlobalTexto = nuevoMargen.ToString("N2");
    }

    private async Task GuardarMargenComercioAsync(decimal nuevoMargen)
    {
        if (ComercioSeleccionado == null) return;

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        var sql = "UPDATE comercios SET porcentaje_comision_divisas = @margen WHERE id_comercio = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("margen", nuevoMargen);
        cmd.Parameters.AddWithValue("id", ComercioSeleccionado.IdComercio);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task GuardarMargenLocalAsync(decimal nuevoMargen)
    {
        if (LocalSeleccionado == null) return;

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        var sql = "UPDATE locales SET comision_divisas = @margen WHERE id_local = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("margen", nuevoMargen);
        cmd.Parameters.AddWithValue("id", LocalSeleccionado.IdLocal);
        await cmd.ExecuteNonQueryAsync();
    }

    private void MostrarMensaje(string mensaje)
    {
        MensajeExito = mensaje;
        MostrarMensajeExito = true;

        Task.Delay(3000).ContinueWith(_ =>
        {
            MostrarMensajeExito = false;
        });
    }
}