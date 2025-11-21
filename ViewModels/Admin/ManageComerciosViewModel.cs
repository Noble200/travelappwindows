using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models.Admin;
using Allva.Desktop.Models;
using Allva.Desktop.Services;
using Npgsql;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Allva.Desktop.ViewModels.Admin;

/// <summary>
/// ViewModel para la gestion de comercios en el panel de administracion
/// 
/// LOGICA DE CODIGOS DE LOCAL:
/// - Prefijo (4 letras): Basado en el nombre del comercio, compartido por todos sus locales
/// - Numero (4 digitos): Secuencial GLOBAL del sistema (0001, 0002, 0003...)
/// - Al eliminar un local, su numero queda disponible para reutilizarse
/// - Ejemplo: Local 1 de Comercio "Allva" = ALLV0001, Local 2 = ALLV0002
///           Local 1 de Comercio "Beta" = BETA0003 (continua numeracion global)
/// </summary>
public partial class ManageComerciosViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    [ObservableProperty]
    private ObservableCollection<ComercioModel> _comercios = new();

    [ObservableProperty]
    private ObservableCollection<ComercioModel> _comerciosFiltrados = new();

    // Nueva coleccion para mostrar locales cuando se busca por Local o Codigo
    [ObservableProperty]
    private ObservableCollection<LocalConComercioModel> _localesFiltrados = new();

    // Indica si se esta mostrando lista de locales o de comercios
    [ObservableProperty]
    private bool _mostrandoLocales = false;

    [ObservableProperty]
    private ComercioModel? _comercioSeleccionado;

    [ObservableProperty]
    private bool _cargando;

    [ObservableProperty]
    private bool _mostrarMensajeExito;

    [ObservableProperty]
    private string _mensajeExito = string.Empty;

    [ObservableProperty]
    private bool _mostrarDialogoConfirmacion = false;

    [ObservableProperty]
    private ComercioModel? _comercioAEliminar;

    [ObservableProperty]
    private bool _mostrarPanelDerecho = false;

    [ObservableProperty]
    private string _tituloPanelDerecho = "Detalles del Comercio";

    [ObservableProperty]
    private object? _contenidoPanelDerecho;

    [ObservableProperty]
    private bool _esModoCreacion = false;

    public string TituloBotonGuardar => EsModoCreacion ? "CREAR COMERCIO" : "GUARDAR CAMBIOS";

    [ObservableProperty]
    private bool _mostrarFormulario;

    [ObservableProperty]
    private bool _modoEdicion;

    [ObservableProperty]
    private string _tituloFormulario = "Crear Comercio";

    [ObservableProperty]
    private string _formNombreComercio = string.Empty;

    [ObservableProperty]
    private string _formNombreSrl = string.Empty;

    [ObservableProperty]
    private string _formDireccionCentral = string.Empty;

    [ObservableProperty]
    private string _formNumeroContacto = string.Empty;

    [ObservableProperty]
    private string _formMailContacto = string.Empty;

    [ObservableProperty]
    private string _formPais = string.Empty;

    [ObservableProperty]
    private string _formObservaciones = string.Empty;

    [ObservableProperty]
    private decimal _formPorcentajeComisionDivisas = 0;

    [ObservableProperty]
    private bool _formActivo = true;

    [ObservableProperty]
    private ObservableCollection<LocalFormModel> _localesComercio = new();

    // Prefijo del comercio actual (4 letras basadas en el nombre)
    private string _prefijoComercioActual = string.Empty;

    [ObservableProperty]
    private string _filtroBusqueda = string.Empty;

    [ObservableProperty]
    private string _filtroTipoBusqueda = "Todos";

    [ObservableProperty]
    private string _filtroPais = string.Empty;

    [ObservableProperty]
    private string _filtroModulo = "Todos";

    [ObservableProperty]
    private ObservableCollection<string> _modulosDisponibles = new()
    {
        "Todos",
        "Compra divisa",
        "Packs de alimentos",
        "Billetes de avion",
        "Packs de viajes"
    };

    [ObservableProperty]
    private ObservableCollection<string> _tiposBusquedaDisponibles = new()
    {
        "Todos",
        "Por Comercio",
        "Por Local",
        "Por Codigo"
    };

    [ObservableProperty]
    private string _filtroUltimaActividad = "Todos";

    [ObservableProperty]
    private ObservableCollection<string> _opcionesUltimaActividad = new()
    {
        "Todos",
        "Compra divisa",
        "Pack alimentos",
        "Billetes de avion",
        "Pack de viajes"
    };

    [ObservableProperty]
    private ObservableCollection<string> _paisesDisponibles = new();

    [ObservableProperty]
    private ObservableCollection<ArchivoComercioModel> _archivosComercioSeleccionado = new();

    [ObservableProperty]
    private ObservableCollection<string> _archivosParaSubir = new();

    private readonly ArchivoService _archivoService = new();

    public int TotalComercios => Comercios.Count;
    public int ComerciosActivos => Comercios.Count(c => c.Activo);
    public int ComerciosInactivos => Comercios.Count(c => !c.Activo);
    public int TotalLocales => Comercios.Sum(c => c.CantidadLocales);

    public ManageComerciosViewModel()
    {
        _ = InicializarSistemaCorrelativos();
        _ = CargarDatosDesdeBaseDatos();
    }

    private async Task CargarDatosDesdeBaseDatos()
    {
        Cargando = true;
        
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var comercios = await CargarComercios(connection);
            
            Comercios.Clear();
            foreach (var comercio in comercios)
            {
                comercio.Locales = await CargarLocalesDelComercio(connection, comercio.IdComercio);
                
                foreach (var local in comercio.Locales)
                {
                    local.Usuarios = await CargarUsuariosDelLocal(connection, local.IdLocal);
                }
                
                comercio.TotalUsuarios = await ContarUsuariosDelComercio(connection, comercio.IdComercio);
                Comercios.Add(comercio);
            }

            OnPropertyChanged(nameof(TotalComercios));
            OnPropertyChanged(nameof(ComerciosActivos));
            OnPropertyChanged(nameof(ComerciosInactivos));
            OnPropertyChanged(nameof(TotalLocales));
            
            await InicializarFiltros();
        }
        catch (Exception ex)
        {
            MensajeExito = $"Error al cargar datos: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
        finally
        {
            Cargando = false;
        }
    }

    private async Task<List<ComercioModel>> CargarComercios(NpgsqlConnection connection)
    {
        var comercios = new List<ComercioModel>();
        
        var query = @"SELECT id_comercio, nombre_comercio, nombre_srl, direccion_central,
                             numero_contacto, mail_contacto, pais, observaciones,
                             porcentaje_comision_divisas, activo, fecha_registro,
                             fecha_ultima_modificacion
                      FROM comercios 
                      ORDER BY nombre_comercio";
        
        using var cmd = new NpgsqlCommand(query, connection);
        using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            comercios.Add(new ComercioModel
            {
                IdComercio = reader.GetInt32(0),
                NombreComercio = reader.GetString(1),
                NombreSrl = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                DireccionCentral = reader.GetString(3),
                NumeroContacto = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                MailContacto = reader.GetString(5),
                Pais = reader.GetString(6),
                Observaciones = reader.IsDBNull(7) ? null : reader.GetString(7),
                PorcentajeComisionDivisas = reader.GetDecimal(8),
                Activo = reader.GetBoolean(9),
                FechaRegistro = reader.GetDateTime(10),
                FechaUltimaModificacion = reader.GetDateTime(11)
            });
        }
        
        return comercios;
    }

    private async Task<List<LocalSimpleModel>> CargarLocalesDelComercio(NpgsqlConnection connection, int idComercio)
    {
        var locales = new List<LocalSimpleModel>();
        
        var query = @"SELECT id_local, codigo_local, nombre_local,
                             pais, codigo_postal, tipo_via,
                             direccion, local_numero, escalera, piso, 
                             movil, telefono, email, observaciones,
                             activo, modulo_divisas, modulo_pack_alimentos, 
                             modulo_billetes_avion, modulo_pack_viajes
                      FROM locales 
                      WHERE id_comercio = @IdComercio
                      ORDER BY codigo_local";
        
        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@IdComercio", idComercio);
        
        using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            locales.Add(new LocalSimpleModel
            {
                IdLocal = reader.GetInt32(0),
                CodigoLocal = reader.GetString(1),
                NombreLocal = reader.GetString(2),
                Pais = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                CodigoPostal = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                TipoVia = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Direccion = reader.GetString(6),
                LocalNumero = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                Escalera = reader.IsDBNull(8) ? null : reader.GetString(8),
                Piso = reader.IsDBNull(9) ? null : reader.GetString(9),
                Movil = reader.IsDBNull(10) ? null : reader.GetString(10),
                Telefono = reader.IsDBNull(11) ? null : reader.GetString(11),
                Email = reader.IsDBNull(12) ? null : reader.GetString(12),
                Observaciones = reader.IsDBNull(13) ? null : reader.GetString(13),
                Activo = reader.GetBoolean(14),
                ModuloDivisas = reader.GetBoolean(15),
                ModuloPackAlimentos = reader.GetBoolean(16),
                ModuloBilletesAvion = reader.GetBoolean(17),
                ModuloPackViajes = reader.GetBoolean(18),
                Usuarios = new List<UserSimpleModel>()
            });
        }
        
        return locales;
    }

    private async Task<List<UserSimpleModel>> CargarUsuariosDelLocal(NpgsqlConnection connection, int idLocal)
    {
        var usuarios = new List<UserSimpleModel>();
        
        var query = @"SELECT u.id_usuario, u.numero_usuario, u.nombre, u.apellidos, u.es_flooter
                      FROM usuarios u
                      WHERE u.id_local = @IdLocal OR (u.es_flooter = true AND EXISTS (
                          SELECT 1 FROM usuario_locales ul 
                          WHERE ul.id_usuario = u.id_usuario AND ul.id_local = @IdLocal
                      ))
                      ORDER BY u.nombre, u.apellidos";
        
        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@IdLocal", idLocal);
        
        using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            usuarios.Add(new UserSimpleModel
            {
                IdUsuario = reader.GetInt32(0),
                NumeroUsuario = reader.GetString(1),
                NombreCompleto = $"{reader.GetString(2)} {reader.GetString(3)}",
                EsFlooter = reader.GetBoolean(4)
            });
        }
        
        return usuarios;
    }

    private async Task<int> ContarUsuariosDelComercio(NpgsqlConnection connection, int idComercio)
    {
        var query = @"SELECT COUNT(*) 
                      FROM usuarios u
                      INNER JOIN locales l ON u.id_local = l.id_local
                      WHERE l.id_comercio = @IdComercio";
        
        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@IdComercio", idComercio);
        
        var result = await cmd.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    [RelayCommand]
    private void MostrarFormularioComercio()
    {
        LimpiarFormulario();
        EsModoCreacion = true;
        OnPropertyChanged(nameof(TituloBotonGuardar));
        ModoEdicion = false;
        TituloFormulario = "Crear Nuevo Comercio";
        TituloPanelDerecho = "Crear Nuevo Comercio";
        MostrarFormulario = true;
        MostrarPanelDerecho = true;
    }

    [RelayCommand]
    private async Task EditarComercio(ComercioModel comercio)
    {
        ComercioSeleccionado = comercio;
        await CargarDatosEnFormulario(comercio);
        EsModoCreacion = false;
        OnPropertyChanged(nameof(TituloBotonGuardar));
        ModoEdicion = true;
        TituloFormulario = "Editar Comercio";
        TituloPanelDerecho = $"Editar: {comercio.NombreComercio}";
        MostrarFormulario = true;
        MostrarPanelDerecho = true;

        await CargarArchivosComercio(comercio.IdComercio);
    }

    [RelayCommand]
    private async Task VerDetallesComercio(ComercioModel comercio)
    {
        try
        {
            Cargando = true;
            
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            var localesActualizados = await CargarLocalesDelComercio(connection, comercio.IdComercio);
            
            foreach (var local in localesActualizados)
            {
                var usuarios = await CargarUsuariosDelLocal(connection, local.IdLocal);
                local.Usuarios = usuarios;
            }
            
            comercio.Locales = localesActualizados;
            
            ComercioSeleccionado = null;
            await Task.Delay(10);
            ComercioSeleccionado = comercio;
            
            TituloPanelDerecho = $"Detalles: {comercio.NombreComercio}";
            MostrarFormulario = false;
            MostrarPanelDerecho = true;
            
            await CargarArchivosComercio(comercio.IdComercio);
        }
        catch (Exception ex)
        {
            MensajeExito = $"Error al cargar detalles: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Ver detalles de un local desde la lista de locales filtrados
    /// </summary>
    [RelayCommand]
    private async Task VerDetallesLocal(LocalConComercioModel localConComercio)
    {
        try
        {
            // Buscar el comercio al que pertenece el local
            var comercio = Comercios.FirstOrDefault(c => c.IdComercio == localConComercio.IdComercio);
            if (comercio != null)
            {
                await VerDetallesComercio(comercio);
            }
        }
        catch (Exception ex)
        {
            MensajeExito = $"Error al cargar detalles del local: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
    }

    [RelayCommand]
    private async Task CerrarPanelDerecho()
    {
        bool hayLocalesNoGuardados = LocalesComercio.Any(l => l.IdLocal == 0);
        
        if (MostrarFormulario && hayLocalesNoGuardados)
        {
            await LiberarNumerosLocalesNoGuardados();
        }
        
        MostrarPanelDerecho = false;
        MostrarFormulario = false;
        ContenidoPanelDerecho = null;
        ComercioSeleccionado = null;
        ArchivosComercioSeleccionado.Clear();
        LimpiarFormulario();
    }

    private async Task LiberarNumerosLocalesNoGuardados()
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();
            
            foreach (var local in LocalesComercio)
            {
                if (local.IdLocal == 0 && !string.IsNullOrEmpty(local.CodigoLocal) && local.CodigoLocal.Length >= 8)
                {
                    await LiberarNumeroLocal(connection, transaction, local.CodigoLocal);
                    Console.WriteLine($"Numero liberado (local no guardado): {local.CodigoLocal}");
                }
            }
            
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al liberar numeros de locales no guardados: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CancelarFormulario()
    {
        await LiberarNumerosLocalesNoGuardados();
        await CerrarPanelDerecho();
    }

    [RelayCommand]
    private async Task GuardarComercio()
    {
        if (!ValidarFormulario(out string mensajeError))
        {
            MensajeExito = mensajeError;
            MostrarMensajeExito = true;
            await Task.Delay(4000);
            MostrarMensajeExito = false;
            return;
        }

        Cargando = true;

        try
        {
            if (ModoEdicion && ComercioSeleccionado != null)
            {
                await ActualizarComercio();
                MensajeExito = "Comercio actualizado correctamente";
            }
            else
            {
                await CrearNuevoComercio();
                MensajeExito = "Comercio creado correctamente";
            }

            await CargarDatosDesdeBaseDatos();
            await CerrarPanelDerecho();

            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
        catch (Exception ex)
        {
            MensajeExito = $"Error al guardar: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(5000);
            MostrarMensajeExito = false;
        }
        finally
        {
            Cargando = false;
        }
    }

    private async Task CrearNuevoComercio()
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        using var transaction = await connection.BeginTransactionAsync();
        
        try
        {
            var queryComercio = @"
                INSERT INTO comercios (
                    nombre_comercio, nombre_srl, direccion_central, 
                    numero_contacto, mail_contacto, pais, observaciones,
                    porcentaje_comision_divisas, activo, fecha_registro, fecha_ultima_modificacion
                )
                VALUES (
                    @NombreComercio, @NombreSrl, @Direccion, 
                    @Telefono, @Email, @Pais, @Observaciones,
                    @Comision, @Activo, @FechaRegistro, @FechaModificacion
                )
                RETURNING id_comercio";
            
            using var cmdComercio = new NpgsqlCommand(queryComercio, connection, transaction);
            cmdComercio.Parameters.AddWithValue("@NombreComercio", FormNombreComercio);
            cmdComercio.Parameters.AddWithValue("@NombreSrl", FormNombreSrl ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Direccion", FormDireccionCentral ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Telefono", FormNumeroContacto ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Email", FormMailContacto);
            cmdComercio.Parameters.AddWithValue("@Pais", FormPais ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Observaciones", 
                string.IsNullOrWhiteSpace(FormObservaciones) ? DBNull.Value : FormObservaciones);
            cmdComercio.Parameters.AddWithValue("@Comision", FormPorcentajeComisionDivisas);
            cmdComercio.Parameters.AddWithValue("@Activo", FormActivo);
            cmdComercio.Parameters.AddWithValue("@FechaRegistro", DateTime.Now);
            cmdComercio.Parameters.AddWithValue("@FechaModificacion", DateTime.Now);
            
            var idComercio = Convert.ToInt32(await cmdComercio.ExecuteScalarAsync());
            
            foreach (var local in LocalesComercio)
            {
                var queryLocal = @"
                    INSERT INTO locales (
                        id_comercio, codigo_local, nombre_local, direccion, local_numero,
                        escalera, piso, movil, telefono, email, observaciones, numero_usuarios_max,
                        activo, modulo_divisas, modulo_pack_alimentos, 
                        modulo_billetes_avion, modulo_pack_viajes,
                        pais, codigo_postal, tipo_via
                    )
                    VALUES (
                        @IdComercio, @CodigoLocal, @NombreLocal, @Direccion, @LocalNumero,
                        @Escalera, @Piso, @Movil, @Telefono, @Email, @Observaciones, @NumeroUsuariosMax,
                        @Activo, @ModuloDivisas, @ModuloPackAlimentos,
                        @ModuloBilletesAvion, @ModuloPackViajes,
                        @Pais, @CodigoPostal, @TipoVia
                    )";
                
                using var cmdLocal = new NpgsqlCommand(queryLocal, connection, transaction);
                cmdLocal.Parameters.AddWithValue("@IdComercio", idComercio);
                cmdLocal.Parameters.AddWithValue("@CodigoLocal", local.CodigoLocal);
                cmdLocal.Parameters.AddWithValue("@NombreLocal", local.NombreLocal);
                cmdLocal.Parameters.AddWithValue("@Direccion", local.Direccion);
                cmdLocal.Parameters.AddWithValue("@LocalNumero", local.LocalNumero ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@Escalera", 
                    string.IsNullOrWhiteSpace(local.Escalera) ? DBNull.Value : local.Escalera);
                cmdLocal.Parameters.AddWithValue("@Piso", 
                    string.IsNullOrWhiteSpace(local.Piso) ? DBNull.Value : local.Piso);
                cmdLocal.Parameters.AddWithValue("@Movil", 
                    string.IsNullOrWhiteSpace(local.Movil) ? DBNull.Value : local.Movil);
                cmdLocal.Parameters.AddWithValue("@Telefono", 
                    string.IsNullOrWhiteSpace(local.Telefono) ? DBNull.Value : local.Telefono);
                cmdLocal.Parameters.AddWithValue("@Email", 
                    string.IsNullOrWhiteSpace(local.Email) ? DBNull.Value : local.Email);
                cmdLocal.Parameters.AddWithValue("@NumeroUsuariosMax", local.NumeroUsuariosMax);
                cmdLocal.Parameters.AddWithValue("@Observaciones", 
                    string.IsNullOrWhiteSpace(local.Observaciones) ? DBNull.Value : local.Observaciones);
                cmdLocal.Parameters.AddWithValue("@Activo", local.Activo);
                cmdLocal.Parameters.AddWithValue("@ModuloDivisas", local.ModuloDivisas);
                cmdLocal.Parameters.AddWithValue("@ModuloPackAlimentos", local.ModuloPackAlimentos);
                cmdLocal.Parameters.AddWithValue("@ModuloBilletesAvion", local.ModuloBilletesAvion);
                cmdLocal.Parameters.AddWithValue("@ModuloPackViajes", local.ModuloPackViajes);
                cmdLocal.Parameters.AddWithValue("@Pais", local.Pais ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@CodigoPostal", local.CodigoPostal ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@TipoVia", local.TipoVia ?? string.Empty);
                
                await cmdLocal.ExecuteNonQueryAsync();
            }
            
            await transaction.CommitAsync();
            
            if (ArchivosParaSubir.Any())
            {
                foreach (var rutaArchivo in ArchivosParaSubir)
                {
                    try
                    {
                        Console.WriteLine($"Subiendo archivo: {rutaArchivo}");
                        await _archivoService.SubirArchivo(idComercio, rutaArchivo, null, null);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error subiendo archivo {rutaArchivo}: {ex.Message}");
                    }
                }
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ActualizarComercio()
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        using var transaction = await connection.BeginTransactionAsync();
        
        try
        {
            var queryComercio = @"
                UPDATE comercios 
                SET nombre_comercio = @NombreComercio,
                    nombre_srl = @NombreSrl,
                    direccion_central = @Direccion,
                    numero_contacto = @Telefono,
                    mail_contacto = @Email,
                    pais = @Pais,
                    observaciones = @Observaciones,
                    porcentaje_comision_divisas = @Comision,
                    activo = @Activo,
                    fecha_ultima_modificacion = @FechaModificacion
                WHERE id_comercio = @IdComercio";
            
            using var cmdComercio = new NpgsqlCommand(queryComercio, connection, transaction);
            cmdComercio.Parameters.AddWithValue("@IdComercio", ComercioSeleccionado!.IdComercio);
            cmdComercio.Parameters.AddWithValue("@NombreComercio", FormNombreComercio);
            cmdComercio.Parameters.AddWithValue("@NombreSrl", FormNombreSrl ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Direccion", FormDireccionCentral ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Telefono", FormNumeroContacto ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Email", FormMailContacto);
            cmdComercio.Parameters.AddWithValue("@Pais", FormPais ?? string.Empty);
            cmdComercio.Parameters.AddWithValue("@Observaciones", 
                string.IsNullOrWhiteSpace(FormObservaciones) ? DBNull.Value : FormObservaciones);
            cmdComercio.Parameters.AddWithValue("@Comision", FormPorcentajeComisionDivisas);
            cmdComercio.Parameters.AddWithValue("@Activo", FormActivo);
            cmdComercio.Parameters.AddWithValue("@FechaModificacion", DateTime.Now);
            
            await cmdComercio.ExecuteNonQueryAsync();
            
            var queryExistentes = @"SELECT codigo_local FROM locales WHERE id_comercio = @IdComercio";
            var codigosExistentesEnBD = new List<string>();
            
            using (var cmdExistentes = new NpgsqlCommand(queryExistentes, connection, transaction))
            {
                cmdExistentes.Parameters.AddWithValue("@IdComercio", ComercioSeleccionado.IdComercio);
                using var readerExistentes = await cmdExistentes.ExecuteReaderAsync();
                while (await readerExistentes.ReadAsync())
                {
                    codigosExistentesEnBD.Add(readerExistentes.GetString(0));
                }
            }
            
            var codigosActuales = LocalesComercio.Select(l => l.CodigoLocal).ToList();
            var codigosEliminados = codigosExistentesEnBD.Except(codigosActuales).ToList();
            
            foreach (var codigoEliminado in codigosEliminados)
            {
                await LiberarNumeroLocal(connection, transaction, codigoEliminado);
                
                var queryEliminarLocal = "DELETE FROM locales WHERE codigo_local = @CodigoLocal";
                using var cmdEliminar = new NpgsqlCommand(queryEliminarLocal, connection, transaction);
                cmdEliminar.Parameters.AddWithValue("@CodigoLocal", codigoEliminado);
                await cmdEliminar.ExecuteNonQueryAsync();
            }
            
            foreach (var local in LocalesComercio)
            {
                var queryUpsert = @"
                    INSERT INTO locales (
                        id_comercio, codigo_local, nombre_local, direccion, local_numero,
                        escalera, piso, movil, telefono, email, observaciones, numero_usuarios_max,
                        activo, modulo_divisas, modulo_pack_alimentos, 
                        modulo_billetes_avion, modulo_pack_viajes,
                        pais, codigo_postal, tipo_via
                    )
                    VALUES (
                        @IdComercio, @CodigoLocal, @NombreLocal, @Direccion, @LocalNumero,
                        @Escalera, @Piso, @Movil, @Telefono, @Email, @Observaciones, @NumeroUsuariosMax,
                        @Activo, @ModuloDivisas, @ModuloPackAlimentos,
                        @ModuloBilletesAvion, @ModuloPackViajes,
                        @Pais, @CodigoPostal, @TipoVia
                    )
                    ON CONFLICT (codigo_local) 
                    DO UPDATE SET
                        nombre_local = EXCLUDED.nombre_local,
                        direccion = EXCLUDED.direccion,
                        local_numero = EXCLUDED.local_numero,
                        escalera = EXCLUDED.escalera,
                        piso = EXCLUDED.piso,
                        movil = EXCLUDED.movil,
                        telefono = EXCLUDED.telefono,
                        email = EXCLUDED.email,
                        observaciones = EXCLUDED.observaciones,
                        numero_usuarios_max = EXCLUDED.numero_usuarios_max,
                        activo = EXCLUDED.activo,
                        modulo_divisas = EXCLUDED.modulo_divisas,
                        modulo_pack_alimentos = EXCLUDED.modulo_pack_alimentos,
                        modulo_billetes_avion = EXCLUDED.modulo_billetes_avion,
                        modulo_pack_viajes = EXCLUDED.modulo_pack_viajes,
                        pais = EXCLUDED.pais,
                        codigo_postal = EXCLUDED.codigo_postal,
                        tipo_via = EXCLUDED.tipo_via";
                
                using var cmdLocal = new NpgsqlCommand(queryUpsert, connection, transaction);
                cmdLocal.Parameters.AddWithValue("@IdComercio", ComercioSeleccionado.IdComercio);
                cmdLocal.Parameters.AddWithValue("@CodigoLocal", local.CodigoLocal);
                cmdLocal.Parameters.AddWithValue("@NombreLocal", local.NombreLocal);
                cmdLocal.Parameters.AddWithValue("@Direccion", local.Direccion);
                cmdLocal.Parameters.AddWithValue("@LocalNumero", local.LocalNumero ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@Escalera", 
                    string.IsNullOrWhiteSpace(local.Escalera) ? DBNull.Value : local.Escalera);
                cmdLocal.Parameters.AddWithValue("@Piso", 
                    string.IsNullOrWhiteSpace(local.Piso) ? DBNull.Value : local.Piso);
                cmdLocal.Parameters.AddWithValue("@Movil", 
                    string.IsNullOrWhiteSpace(local.Movil) ? DBNull.Value : local.Movil);
                cmdLocal.Parameters.AddWithValue("@Telefono", 
                    string.IsNullOrWhiteSpace(local.Telefono) ? DBNull.Value : local.Telefono);
                cmdLocal.Parameters.AddWithValue("@Email", 
                    string.IsNullOrWhiteSpace(local.Email) ? DBNull.Value : local.Email);
                cmdLocal.Parameters.AddWithValue("@NumeroUsuariosMax", local.NumeroUsuariosMax);
                cmdLocal.Parameters.AddWithValue("@Observaciones", 
                    string.IsNullOrWhiteSpace(local.Observaciones) ? DBNull.Value : local.Observaciones);
                cmdLocal.Parameters.AddWithValue("@Activo", local.Activo);
                cmdLocal.Parameters.AddWithValue("@ModuloDivisas", local.ModuloDivisas);
                cmdLocal.Parameters.AddWithValue("@ModuloPackAlimentos", local.ModuloPackAlimentos);
                cmdLocal.Parameters.AddWithValue("@ModuloBilletesAvion", local.ModuloBilletesAvion);
                cmdLocal.Parameters.AddWithValue("@ModuloPackViajes", local.ModuloPackViajes);
                cmdLocal.Parameters.AddWithValue("@Pais", local.Pais ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@CodigoPostal", local.CodigoPostal ?? string.Empty);
                cmdLocal.Parameters.AddWithValue("@TipoVia", local.TipoVia ?? string.Empty);
                
                await cmdLocal.ExecuteNonQueryAsync();
            }
            
            await transaction.CommitAsync();
            
            if (ArchivosParaSubir.Any())
            {
                foreach (var rutaArchivo in ArchivosParaSubir)
                {
                    try
                    {
                        Console.WriteLine($"Subiendo archivo: {rutaArchivo}");
                        await _archivoService.SubirArchivo(ComercioSeleccionado.IdComercio, rutaArchivo, null, null);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error subiendo archivo {rutaArchivo}: {ex.Message}");
                    }
                }
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [RelayCommand]
    private void EliminarComercio(ComercioModel comercio)
    {
        ComercioAEliminar = comercio;
        ComercioSeleccionado = comercio;
        MostrarDialogoConfirmacion = true;
    }

    [RelayCommand]
    private void CancelarEliminarComercio()
    {
        MostrarDialogoConfirmacion = false;
        ComercioAEliminar = null;
    }

    [RelayCommand]
    private async Task ConfirmarEliminarComercio()
    {
        if (ComercioAEliminar == null) return;

        MostrarDialogoConfirmacion = false;
        Cargando = true;

        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            using var transaction = await connection.BeginTransactionAsync();

            foreach (var local in ComercioAEliminar.Locales)
            {
                await LiberarNumeroLocal(connection, transaction, local.CodigoLocal);
            }

            await _archivoService.EliminarArchivosDeComercio(ComercioAEliminar.IdComercio);

            var query = "DELETE FROM comercios WHERE id_comercio = @IdComercio";
            using var cmd = new NpgsqlCommand(query, connection, transaction);
            cmd.Parameters.AddWithValue("@IdComercio", ComercioAEliminar.IdComercio);
            
            await cmd.ExecuteNonQueryAsync();
            await transaction.CommitAsync();

            await CargarDatosDesdeBaseDatos();

            MensajeExito = $"Comercio {ComercioAEliminar.NombreComercio} eliminado correctamente";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
        catch (Exception ex)
        {
            MensajeExito = $"Error al eliminar: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
        finally
        {
            ComercioAEliminar = null;
            Cargando = false;
        }
    }

    [RelayCommand]
    private async Task CambiarEstadoLocal(LocalFormModel local)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var nuevoEstado = !local.Activo;
            var query = "UPDATE locales SET activo = @Activo WHERE codigo_local = @CodigoLocal";
            
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Activo", nuevoEstado);
            cmd.Parameters.AddWithValue("@CodigoLocal", local.CodigoLocal);
            
            await cmd.ExecuteNonQueryAsync();

            local.Activo = nuevoEstado;
            
            if (ComercioSeleccionado != null)
            {
                var localEnDetalle = ComercioSeleccionado.Locales.FirstOrDefault(l => l.CodigoLocal == local.CodigoLocal);
                if (localEnDetalle != null)
                {
                    localEnDetalle.Activo = nuevoEstado;
                }
            }

            MensajeExito = $"Local {local.NombreLocal} marcado como {(nuevoEstado ? "Activo" : "Inactivo")}";
            MostrarMensajeExito = true;
            await Task.Delay(2000);
            MostrarMensajeExito = false;
        }
        catch (Exception ex)
        {
            MensajeExito = $"Error al cambiar estado: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
    }

    [RelayCommand]
    private async Task CambiarEstadoLocalDetalle(LocalSimpleModel local)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var nuevoEstado = !local.Activo;
            var query = "UPDATE locales SET activo = @Activo WHERE codigo_local = @CodigoLocal";
            
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Activo", nuevoEstado);
            cmd.Parameters.AddWithValue("@CodigoLocal", local.CodigoLocal);
            
            await cmd.ExecuteNonQueryAsync();

            local.Activo = nuevoEstado;

            MensajeExito = $"Local {local.NombreLocal} marcado como {(nuevoEstado ? "Activo" : "Inactivo")}";
            MostrarMensajeExito = true;
            await Task.Delay(2000);
            MostrarMensajeExito = false;
        }
        catch (Exception ex)
        {
            MensajeExito = $"Error al cambiar estado: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
    }

    /// <summary>
    /// Cambiar estado de un local desde la lista de locales filtrados
    /// </summary>
    [RelayCommand]
    private async Task CambiarEstadoLocalFiltrado(LocalConComercioModel localConComercio)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var nuevoEstado = !localConComercio.Activo;
            var query = "UPDATE locales SET activo = @Activo WHERE codigo_local = @CodigoLocal";
            
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Activo", nuevoEstado);
            cmd.Parameters.AddWithValue("@CodigoLocal", localConComercio.CodigoLocal);
            
            await cmd.ExecuteNonQueryAsync();

            localConComercio.Activo = nuevoEstado;

            // Actualizar tambien en la lista de comercios
            var comercio = Comercios.FirstOrDefault(c => c.IdComercio == localConComercio.IdComercio);
            if (comercio != null)
            {
                var localEnComercio = comercio.Locales.FirstOrDefault(l => l.CodigoLocal == localConComercio.CodigoLocal);
                if (localEnComercio != null)
                {
                    localEnComercio.Activo = nuevoEstado;
                }
            }

            MensajeExito = $"Local {localConComercio.NombreLocal} marcado como {(nuevoEstado ? "Activo" : "Inactivo")}";
            MostrarMensajeExito = true;
            await Task.Delay(2000);
            MostrarMensajeExito = false;
        }
        catch (Exception ex)
        {
            MensajeExito = $"Error al cambiar estado: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
    }

    [RelayCommand]
    private void ToggleLocalDetalles(LocalSimpleModel local)
    {
        if (local != null)
        {
            local.MostrarDetalles = !local.MostrarDetalles;
        }
    }

    [RelayCommand]
    private void AplicarFiltros()
    {
        // Si el tipo de busqueda es "Por Local" o "Por Codigo", mostrar lista de locales
        if (FiltroTipoBusqueda == "Por Local" || FiltroTipoBusqueda == "Por Codigo")
        {
            AplicarFiltrosLocales();
        }
        else
        {
            AplicarFiltrosComercios();
        }
    }

    /// <summary>
    /// Aplica filtros y muestra lista de COMERCIOS
    /// </summary>
    private void AplicarFiltrosComercios()
    {
        MostrandoLocales = false;
        LocalesFiltrados.Clear();
        
        var filtrados = Comercios.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(FiltroBusqueda))
        {
            var busqueda = FiltroBusqueda.Trim();
            
            filtrados = filtrados.Where(c =>
                c.NombreComercio.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                c.MailContacto.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                (c.NumeroContacto?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.DireccionCentral?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                c.Pais.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
            );
        }
        
        if (!string.IsNullOrWhiteSpace(FiltroPais))
        {
            filtrados = filtrados.Where(c =>
                c.Pais.Contains(FiltroPais, StringComparison.OrdinalIgnoreCase) ||
                c.Locales.Any(l => l.Pais.Contains(FiltroPais, StringComparison.OrdinalIgnoreCase))
            );
        }
        
        if (!string.IsNullOrEmpty(FiltroModulo) && FiltroModulo != "Todos")
        {
            filtrados = filtrados.Where(c => c.Locales.Any(l => 
                (FiltroModulo == "Compra divisa" && l.ModuloDivisas) ||
                (FiltroModulo == "Packs de alimentos" && l.ModuloPackAlimentos) ||
                (FiltroModulo == "Billetes de avion" && l.ModuloBilletesAvion) ||
                (FiltroModulo == "Packs de viajes" && l.ModuloPackViajes)
            ));
        }
        
        ComerciosFiltrados.Clear();
        foreach (var comercio in filtrados.OrderBy(c => c.NombreComercio))
        {
            ComerciosFiltrados.Add(comercio);
        }
    }

    /// <summary>
    /// Aplica filtros y muestra lista de LOCALES (con info del comercio padre)
    /// </summary>
    private void AplicarFiltrosLocales()
    {
        MostrandoLocales = true;
        ComerciosFiltrados.Clear();
        LocalesFiltrados.Clear();
        
        var busqueda = FiltroBusqueda?.Trim() ?? string.Empty;
        
        foreach (var comercio in Comercios)
        {
            foreach (var local in comercio.Locales)
            {
                bool coincide = false;
                
                if (FiltroTipoBusqueda == "Por Local")
                {
                    // Buscar por datos del local
                    coincide = string.IsNullOrEmpty(busqueda) ||
                        (local.CodigoLocal?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (local.NombreLocal?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (local.Direccion?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (local.Email?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (local.Movil?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (local.Telefono?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (local.CodigoPostal?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (local.TipoVia?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (local.Pais?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false);
                }
                else if (FiltroTipoBusqueda == "Por Codigo")
                {
                    // Buscar por codigo del local
                    coincide = string.IsNullOrEmpty(busqueda) ||
                        (local.CodigoLocal?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false);
                }
                
                // Filtro por pais
                if (coincide && !string.IsNullOrWhiteSpace(FiltroPais))
                {
                    coincide = (local.Pais?.Contains(FiltroPais, StringComparison.OrdinalIgnoreCase) ?? false);
                }
                
                // Filtro por modulo
                if (coincide && !string.IsNullOrEmpty(FiltroModulo) && FiltroModulo != "Todos")
                {
                    coincide = 
                        (FiltroModulo == "Compra divisa" && local.ModuloDivisas) ||
                        (FiltroModulo == "Packs de alimentos" && local.ModuloPackAlimentos) ||
                        (FiltroModulo == "Billetes de avion" && local.ModuloBilletesAvion) ||
                        (FiltroModulo == "Packs de viajes" && local.ModuloPackViajes);
                }
                
                if (coincide)
                {
                    LocalesFiltrados.Add(new LocalConComercioModel
                    {
                        // Datos del local
                        IdLocal = local.IdLocal,
                        CodigoLocal = local.CodigoLocal ?? string.Empty,
                        NombreLocal = local.NombreLocal ?? string.Empty,
                        Pais = local.Pais ?? string.Empty,
                        CodigoPostal = local.CodigoPostal ?? string.Empty,
                        TipoVia = local.TipoVia ?? string.Empty,
                        Direccion = local.Direccion ?? string.Empty,
                        LocalNumero = local.LocalNumero ?? string.Empty,
                        Escalera = local.Escalera ?? string.Empty,
                        Piso = local.Piso ?? string.Empty,
                        Movil = local.Movil ?? string.Empty,
                        Telefono = local.Telefono ?? string.Empty,
                        Email = local.Email ?? string.Empty,
                        Activo = local.Activo,
                        ModuloDivisas = local.ModuloDivisas,
                        ModuloPackAlimentos = local.ModuloPackAlimentos,
                        ModuloBilletesAvion = local.ModuloBilletesAvion,
                        ModuloPackViajes = local.ModuloPackViajes,
                        CantidadUsuariosFijos = local.CantidadUsuariosFijos,
                        CantidadUsuariosFlooter = local.CantidadUsuariosFlooter,
                        // Datos del comercio padre
                        IdComercio = comercio.IdComercio,
                        NombreComercio = comercio.NombreComercio ?? string.Empty,
                        PaisComercio = comercio.Pais ?? string.Empty
                    });
                }
            }
        }
    }

    [RelayCommand]
    private void LimpiarFiltros()
    {
        FiltroBusqueda = string.Empty;
        FiltroTipoBusqueda = "Todos";
        FiltroModulo = "Todos";
        FiltroPais = string.Empty;
        FiltroUltimaActividad = "Todos";
        MostrandoLocales = false;
        
        LocalesFiltrados.Clear();
        ComerciosFiltrados.Clear();
        foreach (var comercio in Comercios.OrderBy(c => c.NombreComercio))
        {
            ComerciosFiltrados.Add(comercio);
        }
    }

    [RelayCommand]
    private async Task AgregarLocal()
    {
        var nuevoLocal = new LocalFormModel
        {
            CodigoLocal = await GenerarCodigoLocal(),
            NombreLocal = $"Local {LocalesComercio.Count + 1}",
            Direccion = string.Empty,
            LocalNumero = string.Empty,
            Activo = true,
            Pais = string.Empty,
            CodigoPostal = string.Empty,
            TipoVia = string.Empty,
            NumeroUsuariosMax = 10,
            ModuloDivisas = false,
            ModuloPackAlimentos = false,
            ModuloBilletesAvion = false,
            ModuloPackViajes = false
        };
        
        LocalesComercio.Add(nuevoLocal);
    }

    [RelayCommand]
    private async Task QuitarLocal(LocalFormModel local)
    {
        if (local == null) return;
        
        if (!string.IsNullOrEmpty(local.CodigoLocal) && local.CodigoLocal.Length >= 8)
        {
            try
            {
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();
                
                await LiberarNumeroLocal(connection, transaction, local.CodigoLocal);
                
                await transaction.CommitAsync();
                
                Console.WriteLine($"Numero liberado del local eliminado: {local.CodigoLocal}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al liberar numero del local: {ex.Message}");
            }
        }
        
        LocalesComercio.Remove(local);
    }

    [RelayCommand]
    private async Task SeleccionarArchivos()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null) return;

            var storage = topLevel.StorageProvider;
            
            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar archivos del comercio",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Documentos") 
                    { 
                        Patterns = new[] { "*.pdf", "*.doc", "*.docx", "*.txt" } 
                    },
                    new FilePickerFileType("Imagenes") 
                    { 
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif" } 
                    },
                    new FilePickerFileType("Todos") 
                    { 
                        Patterns = new[] { "*" } 
                    }
                }
            });

            foreach (var file in files)
            {
                var rutaCompleta = file.Path.LocalPath;
                if (!ArchivosParaSubir.Contains(rutaCompleta))
                {
                    ArchivosParaSubir.Add(rutaCompleta);
                    Console.WriteLine($"Archivo agregado para subir: {rutaCompleta}");
                }
            }
            
            if (files.Count > 0)
            {
                MensajeExito = $"{files.Count} archivo(s) seleccionado(s)";
                MostrarMensajeExito = true;
                await Task.Delay(2000);
                MostrarMensajeExito = false;
            }
        }
        catch (Exception ex)
        {
            MensajeExito = $"Error al seleccionar archivos: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
    }

    [RelayCommand]
    private void QuitarArchivo(string archivo)
    {
        ArchivosParaSubir.Remove(archivo);
    }

    [RelayCommand]
    private async Task DescargarArchivo(ArchivoComercioModel archivo)
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null) return;

            var storage = topLevel.StorageProvider;
            
            var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Guardar archivo",
                SuggestedFileName = archivo.NombreArchivo,
                FileTypeChoices = new[] { new FilePickerFileType("Todos") { Patterns = new[] { "*" } } }
            });

            if (file != null)
            {
                var rutaDestino = file.Path.LocalPath;
                await _archivoService.DescargarArchivo(
                    ComercioSeleccionado!.IdComercio, 
                    archivo.IdArchivo,
                    rutaDestino
                );
                
                MensajeExito = $"Archivo guardado: {archivo.NombreArchivo}";
                MostrarMensajeExito = true;
                await Task.Delay(3000);
                MostrarMensajeExito = false;
            }
        }
        catch (Exception ex)
        {
            MensajeExito = $"Error al guardar: {ex.Message}";
            MostrarMensajeExito = true;
            await Task.Delay(5000);
            MostrarMensajeExito = false;
        }
    }

    // ============================================
    // SISTEMA DE CODIGOS DE LOCAL - 4 LETRAS + 4 DIGITOS SECUENCIALES GLOBALES
    // ============================================

    private async Task InicializarSistemaCorrelativos()
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var queryCrearTablaLiberados = @"
                CREATE TABLE IF NOT EXISTS numeros_locales_liberados (
                    numero INTEGER PRIMARY KEY,
                    fecha_liberacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";
            
            using var cmd1 = new NpgsqlCommand(queryCrearTablaLiberados, connection);
            await cmd1.ExecuteNonQueryAsync();

            Console.WriteLine("Sistema de correlativos globales inicializado correctamente");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inicializando sistema de correlativos: {ex.Message}");
        }
    }

    private async Task<string> GenerarCodigoLocal()
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AllvaLog_CodigoLocal.txt");
        
        void Log(string mensaje)
        {
            var linea = $"[{DateTime.Now:HH:mm:ss}] {mensaje}";
            Console.WriteLine(linea);
            try { File.AppendAllText(logPath, linea + Environment.NewLine); } catch { }
        }
        
        Log("========== INICIO GenerarCodigoLocal ==========");
        Log($"FormNombreComercio: '{FormNombreComercio}'");
        
        if (string.IsNullOrEmpty(FormNombreComercio))
        {
            Log("FormNombreComercio vacio, retornando TEMP0001");
            return "TEMP0001";
        }

        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        Log("Conexion abierta");

        try
        {
            if (string.IsNullOrEmpty(_prefijoComercioActual))
            {
                _prefijoComercioActual = GenerarPrefijo4Letras(FormNombreComercio);
                Log($"Prefijo generado: '{_prefijoComercioActual}'");
            }
            else
            {
                Log($"Prefijo existente: '{_prefijoComercioActual}'");
            }

            int numeroLocal = 0;
            
            try
            {
                var queryBuscarLiberado = @"
                    SELECT nl.numero 
                    FROM numeros_locales_liberados nl
                    WHERE NOT EXISTS (
                        SELECT 1 FROM locales l 
                        WHERE SUBSTRING(l.codigo_local FROM '.{4}$') = LPAD(nl.numero::TEXT, 4, '0')
                    )
                    ORDER BY nl.numero ASC 
                    LIMIT 1";
                
                using var cmdBuscar = new NpgsqlCommand(queryBuscarLiberado, connection);
                var numeroLiberado = await cmdBuscar.ExecuteScalarAsync();
                
                if (numeroLiberado != null && numeroLiberado != DBNull.Value)
                {
                    numeroLocal = Convert.ToInt32(numeroLiberado);
                    Log($"Numero liberado encontrado (verificado que no existe): {numeroLocal}");
                    
                    var queryEliminarLiberado = "DELETE FROM numeros_locales_liberados WHERE numero = @Numero";
                    using var cmdEliminar = new NpgsqlCommand(queryEliminarLiberado, connection);
                    cmdEliminar.Parameters.AddWithValue("@Numero", numeroLocal);
                    await cmdEliminar.ExecuteNonQueryAsync();
                    Log($"Numero {numeroLocal} eliminado de tabla liberados");
                }
                else
                {
                    Log("No hay numeros liberados disponibles (o todos ya existen en locales)");
                    
                    var queryLimpiar = @"
                        DELETE FROM numeros_locales_liberados nl
                        WHERE EXISTS (
                            SELECT 1 FROM locales l 
                            WHERE SUBSTRING(l.codigo_local FROM '.{4}$') = LPAD(nl.numero::TEXT, 4, '0')
                        )";
                    using var cmdLimpiar = new NpgsqlCommand(queryLimpiar, connection);
                    var eliminados = await cmdLimpiar.ExecuteNonQueryAsync();
                    if (eliminados > 0)
                    {
                        Log($"Limpieza: {eliminados} numeros duplicados eliminados de tabla liberados");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error buscando liberados (tabla puede no existir): {ex.Message}");
            }
            
            if (numeroLocal == 0)
            {
                Log("Buscando maximo en BD...");
                
                var queryTodosLocales = "SELECT codigo_local FROM locales WHERE codigo_local IS NOT NULL";
                using var cmdTodos = new NpgsqlCommand(queryTodosLocales, connection);
                using var reader = await cmdTodos.ExecuteReaderAsync();
                
                int maximoActual = 0;
                int contadorLocales = 0;
                
                while (await reader.ReadAsync())
                {
                    contadorLocales++;
                    var codigo = reader.GetString(0);
                    Log($"Local #{contadorLocales}: '{codigo}'");
                    
                    if (!string.IsNullOrEmpty(codigo) && codigo.Length >= 4)
                    {
                        var ultimosCuatro = codigo.Substring(codigo.Length - 4);
                        Log($"  Ultimos 4: '{ultimosCuatro}'");
                        
                        if (int.TryParse(ultimosCuatro, out int num))
                        {
                            Log($"  Numero parseado: {num}");
                            if (num > maximoActual)
                            {
                                maximoActual = num;
                                Log($"  NUEVO MAXIMO: {maximoActual}");
                            }
                        }
                        else
                        {
                            Log($"  ERROR: No se pudo parsear '{ultimosCuatro}'");
                        }
                    }
                }
                
                await reader.CloseAsync();
                Log($"Total locales en BD: {contadorLocales}, Maximo de BD: {maximoActual}");
                
                Log($"Locales en memoria (LocalesComercio): {LocalesComercio.Count}");
                foreach (var localEnMemoria in LocalesComercio)
                {
                    Log($"  Memoria: '{localEnMemoria.CodigoLocal}'");
                    if (!string.IsNullOrEmpty(localEnMemoria.CodigoLocal) && localEnMemoria.CodigoLocal.Length >= 4)
                    {
                        var ultimosCuatro = localEnMemoria.CodigoLocal.Substring(localEnMemoria.CodigoLocal.Length - 4);
                        if (int.TryParse(ultimosCuatro, out int numEnMemoria))
                        {
                            Log($"    Numero en memoria: {numEnMemoria}");
                            if (numEnMemoria > maximoActual)
                            {
                                maximoActual = numEnMemoria;
                                Log($"    NUEVO MAXIMO desde memoria: {maximoActual}");
                            }
                        }
                    }
                }
                
                numeroLocal = maximoActual + 1;
                Log($"CALCULO FINAL: maximoActual({maximoActual}) + 1 = {numeroLocal}");
            }

            var codigo_final = $"{_prefijoComercioActual}{numeroLocal:D4}";
            
            Log($"CODIGO GENERADO: {codigo_final}");
            Log("========== FIN GenerarCodigoLocal ==========");
            return codigo_final;
        }
        catch (Exception ex)
        {
            Log($"ERROR GENERAL: {ex.Message}");
            Log($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    private string GenerarPrefijo4Letras(string nombreComercio)
    {
        var letrasDisponibles = new string(nombreComercio
            .Where(char.IsLetter)
            .ToArray())
            .ToUpper();

        if (letrasDisponibles.Length >= 4)
        {
            return letrasDisponibles.Substring(0, 4);
        }
        else
        {
            return letrasDisponibles.PadRight(4, 'X');
        }
    }

    private async Task LiberarNumeroLocal(NpgsqlConnection connection, NpgsqlTransaction transaction, string codigoLocal)
    {
        try
        {
            if (codigoLocal.Length >= 4)
            {
                var numeroTexto = codigoLocal.Substring(codigoLocal.Length - 4);
                if (int.TryParse(numeroTexto, out int numero))
                {
                    var query = @"
                        INSERT INTO numeros_locales_liberados (numero, fecha_liberacion)
                        VALUES (@Numero, CURRENT_TIMESTAMP)
                        ON CONFLICT (numero) DO NOTHING";
                    
                    using var cmd = new NpgsqlCommand(query, connection, transaction);
                    cmd.Parameters.AddWithValue("@Numero", numero);
                    await cmd.ExecuteNonQueryAsync();
                    
                    Console.WriteLine($"Numero {numero} liberado para reutilizacion (codigo: {codigoLocal})");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al liberar numero del local {codigoLocal}: {ex.Message}");
        }
    }

    // ============================================
    // METODOS AUXILIARES - FORMULARIO
    // ============================================

    private void LimpiarFormulario()
    {
        FormNombreComercio = string.Empty;
        FormNombreSrl = string.Empty;
        FormDireccionCentral = string.Empty;
        FormNumeroContacto = string.Empty;
        FormMailContacto = string.Empty;
        FormPais = string.Empty;
        FormObservaciones = string.Empty;
        FormPorcentajeComisionDivisas = 0;
        FormActivo = true;
        LocalesComercio.Clear();
        ArchivosParaSubir.Clear();
        _prefijoComercioActual = string.Empty;
    }

    private async Task CargarDatosEnFormulario(ComercioModel comercio)
    {
        FormNombreComercio = comercio.NombreComercio;
        FormNombreSrl = comercio.NombreSrl;
        FormDireccionCentral = comercio.DireccionCentral;
        FormNumeroContacto = comercio.NumeroContacto;
        FormMailContacto = comercio.MailContacto;
        FormPais = comercio.Pais;
        FormObservaciones = comercio.Observaciones ?? string.Empty;
        FormPorcentajeComisionDivisas = comercio.PorcentajeComisionDivisas;
        FormActivo = comercio.Activo;

        LocalesComercio.Clear();
        foreach (var local in comercio.Locales)
        {
            LocalesComercio.Add(new LocalFormModel
            {
                IdLocal = local.IdLocal,
                IdComercio = comercio.IdComercio,
                CodigoLocal = local.CodigoLocal,
                NombreLocal = local.NombreLocal,
                Pais = local.Pais ?? string.Empty,
                CodigoPostal = local.CodigoPostal ?? string.Empty,
                TipoVia = local.TipoVia ?? string.Empty,
                Direccion = local.Direccion,
                LocalNumero = local.LocalNumero,
                Escalera = local.Escalera,
                Piso = local.Piso,
                Movil = local.Movil,
                Telefono = local.Telefono,
                Email = local.Email,
                Observaciones = local.Observaciones,
                Activo = local.Activo,
                ModuloDivisas = local.ModuloDivisas,
                ModuloPackAlimentos = local.ModuloPackAlimentos,
                ModuloBilletesAvion = local.ModuloBilletesAvion,
                ModuloPackViajes = local.ModuloPackViajes,
                NumeroUsuariosMax = 10
            });
        }
        
        ArchivosParaSubir.Clear();
        
        if (comercio.Locales.Any())
        {
            var primerLocal = comercio.Locales.First();
            if (primerLocal.CodigoLocal.Length >= 4)
            {
                _prefijoComercioActual = primerLocal.CodigoLocal.Substring(0, 4);
                Console.WriteLine($"Prefijo del comercio capturado: {_prefijoComercioActual}");
            }
        }
        else
        {
            _prefijoComercioActual = GenerarPrefijo4Letras(FormNombreComercio);
            Console.WriteLine($"Nuevo prefijo generado: {_prefijoComercioActual}");
        }
        
        await Task.CompletedTask;
    }

    private bool ValidarFormulario(out string mensajeError)
    {
        mensajeError = string.Empty;
        
        if (string.IsNullOrWhiteSpace(FormNombreComercio))
        {
            mensajeError = "El nombre del comercio es requerido";
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(FormMailContacto))
        {
            mensajeError = "El email de contacto es requerido";
            return false;
        }
        
        if (!FormMailContacto.Contains("@"))
        {
            mensajeError = "El formato del email no es valido";
            return false;
        }
        
        if (!LocalesComercio.Any())
        {
            mensajeError = "Debe agregar al menos un local";
            return false;
        }
        
        foreach (var local in LocalesComercio)
        {
            if (string.IsNullOrWhiteSpace(local.NombreLocal))
            {
                mensajeError = "Todos los locales deben tener un nombre";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(local.Pais))
            {
                mensajeError = $"El local '{local.NombreLocal}' debe tener un pais";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(local.CodigoPostal))
            {
                mensajeError = $"El local '{local.NombreLocal}' debe tener codigo postal";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(local.TipoVia))
            {
                mensajeError = $"El local '{local.NombreLocal}' debe tener tipo de via";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(local.Direccion))
            {
                mensajeError = $"El local '{local.NombreLocal}' debe tener una direccion";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(local.LocalNumero))
            {
                mensajeError = $"El local '{local.NombreLocal}' debe tener un numero";
                return false;
            }
        }
        
        return true;
    }

    private async Task CargarArchivosComercio(int idComercio)
    {
        try
        {
            Console.WriteLine($"Cargando archivos del comercio ID: {idComercio}");
            
            ArchivosComercioSeleccionado.Clear();
            
            var archivos = await _archivoService.ObtenerArchivosPorComercio(idComercio);
            
            foreach (var archivo in archivos)
            {
                ArchivosComercioSeleccionado.Add(archivo);
            }
            
            Console.WriteLine($"Archivos cargados: {archivos.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar archivos: {ex.Message}");
        }
    }

    private async Task InicializarFiltros()
    {
        await Task.Delay(100);
        
        ComerciosFiltrados.Clear();
        foreach (var comercio in Comercios.OrderBy(c => c.NombreComercio))
        {
            ComerciosFiltrados.Add(comercio);
        }
    }
}

/// <summary>
/// Modelo de local con informacion del comercio padre para mostrar en busquedas
/// </summary>
public partial class LocalConComercioModel : ObservableObject
{
    // Datos del local
    public int IdLocal { get; set; }
    public string CodigoLocal { get; set; } = string.Empty;
    public string NombreLocal { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string TipoVia { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string LocalNumero { get; set; } = string.Empty;
    public string Escalera { get; set; } = string.Empty;
    public string Piso { get; set; } = string.Empty;
    public string Movil { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    [ObservableProperty]
    private bool _activo;
    
    public bool ModuloDivisas { get; set; }
    public bool ModuloPackAlimentos { get; set; }
    public bool ModuloBilletesAvion { get; set; }
    public bool ModuloPackViajes { get; set; }
    public int CantidadUsuariosFijos { get; set; }
    public int CantidadUsuariosFlooter { get; set; }
    
    // Datos del comercio padre
    public int IdComercio { get; set; }
    public string NombreComercio { get; set; } = string.Empty;
    public string PaisComercio { get; set; } = string.Empty;
    
    // Propiedades calculadas para UI
    public string DireccionCompleta
    {
        get
        {
            var partes = new List<string>();
            if (!string.IsNullOrEmpty(TipoVia)) partes.Add(TipoVia);
            if (!string.IsNullOrEmpty(Direccion)) partes.Add(Direccion);
            if (!string.IsNullOrEmpty(LocalNumero)) partes.Add($"Nº {LocalNumero}");
            if (!string.IsNullOrEmpty(Escalera)) partes.Add($"Piso {Escalera}");
            if (!string.IsNullOrEmpty(Piso)) partes.Add($"Pta {Piso}");
            return partes.Count > 0 ? string.Join(", ", partes) : "Sin direccion";
        }
    }
    
    public int TotalUsuarios => CantidadUsuariosFijos + CantidadUsuariosFlooter;
}