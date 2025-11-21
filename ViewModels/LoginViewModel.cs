using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Services;
using Npgsql;
using BCrypt.Net;

namespace Allva.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla de inicio de sesión
    /// Soporta login de usuarios normales Y administradores Allva
    /// </summary>
    public partial class LoginViewModel : ObservableObject
    {
        // ============================================
        // CONFIGURACIÓN DE BASE DE DATOS
        // ============================================
        
        private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";
        
        // ============================================
        // CONFIGURACIÓN DE LOGS
        // ============================================
        
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "AllvaSystem_Login_Debug.log"
        );
        
        private static void WriteLog(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logMessage = $"[{timestamp}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, logMessage);
            }
            catch (Exception ex)
            {
                // Si falla el log, no hacer nada para no interrumpir el flujo
                Console.WriteLine($"Error escribiendo log: {ex.Message}");
            }
        }

        // ============================================
        // LOCALIZACIÓN
        // ============================================

        public LocalizationService Localization => LocalizationService.Instance;

        // ============================================
        // PROPIEDADES OBSERVABLES
        // ============================================

        [ObservableProperty]
        private string _numeroUsuario = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _codigoLocal = string.Empty;

        [ObservableProperty]
        private bool _recordarCredenciales;
        
        [ObservableProperty]
        private bool _recordarSesion;

        [ObservableProperty]
        private bool _mostrarPassword;

        [ObservableProperty]
        private bool _cargando;
        
        // Alias para compatibilidad con LoginView.axaml
        public bool IsLoading => Cargando;
        
        // Notificar cambios en IsLoading cuando cambia Cargando
        partial void OnCargandoChanged(bool value)
        {
            OnPropertyChanged(nameof(IsLoading));
        }

        [ObservableProperty]
        private string _mensajeError = string.Empty;

        [ObservableProperty]
        private bool _mostrarMensajeError;

        // ============================================
        // PROPIEDADES PARA ACTUALIZACIÓN
        // ============================================

        [ObservableProperty]
        private string _mensajeActualizacion = string.Empty;

        // Propiedad manual para MostrarMensajeActualizacion (sin ObservableProperty)
        private bool _mostrarMensajeActualizacion;
        public bool MostrarMensajeActualizacion
        {
            get => _mostrarMensajeActualizacion;
            set => SetProperty(ref _mostrarMensajeActualizacion, value);
        }

        [ObservableProperty]
        private int _progresoActualizacion;

        public void ActivarMensajeActualizacion(string mensaje, int progreso)
        {
            MensajeActualizacion = mensaje;
            ProgresoActualizacion = progreso;
            MostrarMensajeActualizacion = true;
            Cargando = true;
        }

        public void DesactivarMensajeActualizacion()
        {
            MostrarMensajeActualizacion = false;
            MensajeActualizacion = string.Empty;
            ProgresoActualizacion = 0;
            Cargando = false;
        }

        // ============================================
        // COMANDOS
        // ============================================

        /// <summary>
        /// Comando para iniciar sesión
        /// LÓGICA:
        /// - Admin Allva: código local = "CENTRAL"
        /// - Usuario Floater: código local = "FLOATER"  
        /// - Usuario Fijo: código local = código real del local
        /// </summary>
        [RelayCommand]
        private async Task IniciarSesion()
        {
            WriteLog("========================================");
            WriteLog("INICIO DE PROCESO DE LOGIN");
            WriteLog($"Usuario ingresado: '{NumeroUsuario}'");
            WriteLog($"Código local ingresado: '{CodigoLocal}'");
            WriteLog($"Longitud password: {Password?.Length ?? 0}");
            
            // Limpiar mensajes anteriores
            MensajeError = string.Empty;
            MostrarMensajeError = false;

            // Validar campos básicos
            if (!ValidarCampos())
            {
                WriteLog("Validación de campos falló");
                return;
            }

            WriteLog("Validación de campos exitosa");
            Cargando = true;

            try
            {
                var codigoLocalUpper = CodigoLocal.Trim().ToUpper();
                
                // =============================================
                // CASO 1: ADMINISTRADOR ALLVA (código = CENTRAL)
                // =============================================
                if (codigoLocalUpper == "CENTRAL")
                {
                    WriteLog("Código 'CENTRAL' detectado - Intentando como Administrador Allva...");
                    
                    var resultadoAdmin = await AutenticarAdministradorAllva(NumeroUsuario!, Password!);
                    
                    if (resultadoAdmin.Exitoso)
                    {
                        WriteLog("✓ Login exitoso como Administrador Allva");
                        var loginData = new LoginSuccessData
                        {
                            UserName = resultadoAdmin.NombreCompleto,
                            UserNumber = NumeroUsuario,
                            LocalCode = "CENTRAL",
                            Token = $"token-{Guid.NewGuid()}",
                            IsSystemAdmin = true,
                            UserType = "ADMIN_ALLVA",
                            RoleName = "Administrador_Allva",
                            
                            Permisos = new PermisosAdministrador
                            {
                                AccesoGestionComercios = resultadoAdmin.AccesoGestionComercios,
                                AccesoGestionUsuariosLocales = resultadoAdmin.AccesoGestionUsuariosLocales,
                                AccesoGestionUsuariosAllva = resultadoAdmin.AccesoGestionUsuariosAllva,
                                AccesoAnalytics = resultadoAdmin.AccesoAnalytics,
                                AccesoConfiguracionSistema = resultadoAdmin.AccesoConfiguracionSistema,
                                AccesoFacturacionGlobal = resultadoAdmin.AccesoFacturacionGlobal,
                                AccesoAuditoria = resultadoAdmin.AccesoAuditoria
                            }
                        };

                        WriteLog("Navegando a AdminDashboard...");
                        var navigationService = new NavigationService();
                        navigationService.NavigateToAdminDashboard(loginData);
                        return;
                    }
                    else
                    {
                        WriteLog($"✗ Falló autenticación como Admin: {resultadoAdmin.MensajeError}");
                        MostrarError(resultadoAdmin.MensajeError);
                        return;
                    }
                }
                
                // =============================================
                // CASO 2: USUARIO FLOATER (código = FLOATER)
                // =============================================
                if (codigoLocalUpper == "FLOATER")
                {
                    WriteLog("Código 'FLOATER' detectado - Intentando como usuario Floater...");
                    
                    var resultadoFloater = await AutenticarUsuarioFloater(NumeroUsuario!, Password!);
                    
                    if (resultadoFloater.Exitoso)
                    {
                        WriteLog($"✓ Login exitoso como Floater: {resultadoFloater.NombreCompleto}");
                        var loginData = new LoginSuccessData
                        {
                            UserName = resultadoFloater.NombreCompleto,
                            UserNumber = NumeroUsuario,
                            LocalCode = "FLOATER",
                            Token = $"token-{Guid.NewGuid()}",
                            IsSystemAdmin = false,
                            UserType = "FLOATER",
                            RoleName = "Usuario_Floater"
                        };

                        WriteLog("Navegando a MainDashboard...");
                        var navigationService = new NavigationService();
                        navigationService.NavigateTo("MainDashboard", loginData);
                        return;
                    }
                    else
                    {
                        WriteLog($"✗ Falló autenticación como Floater: {resultadoFloater.MensajeError}");
                        MostrarError(resultadoFloater.MensajeError);
                        return;
                    }
                }
                
                // =============================================
                // CASO 3: USUARIO FIJO (código = código real del local)
                // =============================================
                WriteLog($"Código '{codigoLocalUpper}' detectado - Intentando como usuario fijo...");
                
                var resultadoNormal = await AutenticarUsuarioFijo(NumeroUsuario!, Password!, codigoLocalUpper);
                
                if (resultadoNormal.Exitoso)
                {
                    WriteLog($"✓ Login exitoso como usuario fijo: {resultadoNormal.NombreCompleto}");
                    var loginData = new LoginSuccessData
                    {
                        UserName = resultadoNormal.NombreCompleto,
                        UserNumber = NumeroUsuario,
                        LocalCode = codigoLocalUpper,
                        Token = $"token-{Guid.NewGuid()}",
                        IsSystemAdmin = false,
                        UserType = "USUARIO_LOCAL",
                        RoleName = "Usuario_Local"
                    };

                    WriteLog("Navegando a MainDashboard...");
                    var navigationService = new NavigationService();
                    navigationService.NavigateTo("MainDashboard", loginData);
                }
                else
                {
                    WriteLog($"✗ Falló autenticación usuario fijo: {resultadoNormal.MensajeError}");
                    MostrarError(resultadoNormal.MensajeError);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"✗✗✗ EXCEPCIÓN CRÍTICA: {ex.GetType().Name}");
                WriteLog($"Mensaje: {ex.Message}");
                WriteLog($"StackTrace: {ex.StackTrace}");
                MostrarError($"Error de conexión: {ex.Message}");
            }
            finally
            {
                Cargando = false;
                WriteLog("FIN DE PROCESO DE LOGIN");
                WriteLog("========================================");
            }
        }
                
        /// <summary>
        /// Alias para LoginCommand (compatibilidad con LoginView.axaml)
        /// </summary>
        public IAsyncRelayCommand LoginCommand => IniciarSesionCommand;

        /// <summary>
        /// Comando para mostrar/ocultar contraseña
        /// </summary>
        [RelayCommand]
        private void ToggleMostrarPassword()
        {
            MostrarPassword = !MostrarPassword;
        }
        
        /// <summary>
        /// Comando para cambiar idioma (stub - no implementado aún)
        /// </summary>
        [RelayCommand]
        private void CambiarIdioma(string idioma)
        {
            // TODO: Implementar cambio de idioma
        }
        
        /// <summary>
        /// Comando para recuperar contraseña (stub - no implementado aún)
        /// </summary>
        [RelayCommand]
        private async Task RecuperarPassword()
        {
            // TODO: Implementar recuperación de contraseña
            await Task.CompletedTask;
        }

        // ============================================
        // MÉTODOS DE AUTENTICACIÓN
        // ============================================

        /// <summary>
        /// Autentica administradores de Allva (jose_noble, maria_gonzalez, etc)
        /// Busca en tabla administradores_allva
        /// </summary>
        private async Task<ResultadoAutenticacion> AutenticarAdministradorAllva(string nombreUsuario, string password)
        {
            WriteLog($"  → AutenticarAdministradorAllva iniciado para: {nombreUsuario}");
            try
            {
                WriteLog("  → Abriendo conexión a BD...");
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();
                WriteLog("  → Conexión BD abierta exitosamente");

                var query = @"
                    SELECT 
                        nombre_usuario,
                        nombre,
                        apellidos,
                        password_hash,
                        activo,
                        acceso_gestion_comercios,
                        acceso_gestion_usuarios_locales,
                        acceso_gestion_usuarios_allva,
                        acceso_analytics,
                        acceso_configuracion_sistema,
                        acceso_facturacion_global,
                        acceso_auditoria
                    FROM administradores_allva
                    WHERE nombre_usuario = @NombreUsuario";

                WriteLog($"  → Ejecutando query para admin: {nombreUsuario}");
                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@NombreUsuario", nombreUsuario);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    WriteLog("  → No se encontró el usuario en administradores_allva");
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario administrador no encontrado"
                    };
                }

                WriteLog("  → Usuario encontrado en BD");

                // Verificar si está activo (índice 4)
                bool activo = reader.GetBoolean(4);
                WriteLog($"  → Usuario activo: {activo}");
                if (!activo)
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario administrador inactivo"
                    };
                }

                // Verificar contraseña con BCrypt (índice 3)
                var passwordHash = reader.GetString(3);
                WriteLog($"  → Hash almacenado: {passwordHash.Substring(0, 20)}...");
                WriteLog("  → Verificando contraseña con BCrypt...");
                
                bool passwordCorrecta = BCrypt.Net.BCrypt.Verify(password, passwordHash);
                WriteLog($"  → Resultado verificación BCrypt: {passwordCorrecta}");
                
                if (!passwordCorrecta)
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Contraseña incorrecta"
                    };
                }

                // Concatenar nombre completo (índices 1 y 2)
                var nombre = reader.GetString(1);
                var apellidos = reader.GetString(2);
                var nombreCompleto = $"{nombre} {apellidos}";
                WriteLog($"  → Login exitoso para: {nombreCompleto}");

                // Login exitoso
                return new ResultadoAutenticacion
                {
                    Exitoso = true,
                    NombreCompleto = nombreCompleto,
                    EsAdministradorAllva = true,
                    AccesoGestionComercios = reader.GetBoolean(5),
                    AccesoGestionUsuariosLocales = reader.GetBoolean(6),
                    AccesoGestionUsuariosAllva = reader.GetBoolean(7),
                    AccesoAnalytics = reader.GetBoolean(8),
                    AccesoConfiguracionSistema = reader.GetBoolean(9),
                    AccesoFacturacionGlobal = reader.GetBoolean(10),
                    AccesoAuditoria = reader.GetBoolean(11)
                };
            }
            catch (Exception ex)
            {
                WriteLog($"  → ✗✗✗ ERROR en AutenticarAdministradorAllva: {ex.GetType().Name}");
                WriteLog($"  → Mensaje: {ex.Message}");
                WriteLog($"  → StackTrace: {ex.StackTrace}");
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = $"Error al autenticar administrador: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Autentica usuarios FLOATER (código local = "FLOATER")
        /// Busca en tabla usuarios donde es_flooter = true
        /// </summary>
        private async Task<ResultadoAutenticacion> AutenticarUsuarioFloater(string numeroUsuario, string password)
        {
            WriteLog($"  → AutenticarUsuarioFloater iniciado para: {numeroUsuario}");
            
            try
            {
                WriteLog("  → Abriendo conexión a BD...");
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();
                WriteLog("  → Conexión BD abierta exitosamente");

                // Buscar usuario floater por numero_usuario
                var query = @"
                    SELECT 
                        u.id_usuario,
                        u.numero_usuario,
                        CONCAT(u.nombre, ' ', u.apellidos) as nombre_completo,
                        u.password_hash,
                        u.activo,
                        u.es_flooter
                    FROM usuarios u
                    WHERE u.numero_usuario = @NumeroUsuario
                    AND u.es_flooter = true";

                WriteLog($"  → Ejecutando query para usuario floater...");
                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@NumeroUsuario", numeroUsuario);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    WriteLog("  → No se encontró usuario floater con ese número");
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario floater no encontrado"
                    };
                }

                WriteLog("  → Usuario floater encontrado en BD");

                // Verificar si está activo (índice 4)
                bool usuarioActivo = reader.GetBoolean(4);
                WriteLog($"  → Usuario activo: {usuarioActivo}");
                if (!usuarioActivo)
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario inactivo"
                    };
                }

                // Verificar contraseña con BCrypt (índice 3)
                var passwordHash = reader.GetString(3);
                WriteLog($"  → Hash almacenado: {passwordHash.Substring(0, Math.Min(20, passwordHash.Length))}...");
                WriteLog("  → Verificando contraseña con BCrypt...");
                
                bool passwordCorrecta = BCrypt.Net.BCrypt.Verify(password, passwordHash);
                WriteLog($"  → Resultado verificación BCrypt: {passwordCorrecta}");
                
                if (!passwordCorrecta)
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Contraseña incorrecta"
                    };
                }

                // Login exitoso
                var nombreCompleto = reader.GetString(2);
                WriteLog($"  → ✓ Login exitoso para floater: {nombreCompleto}");
                
                return new ResultadoAutenticacion
                {
                    Exitoso = true,
                    NombreCompleto = nombreCompleto,
                    EsAdministradorAllva = false
                };
            }
            catch (Exception ex)
            {
                WriteLog($"  → ✗✗✗ ERROR en AutenticarUsuarioFloater: {ex.GetType().Name}");
                WriteLog($"  → Mensaje: {ex.Message}");
                WriteLog($"  → StackTrace: {ex.StackTrace}");
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = $"Error al autenticar floater: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Autentica usuarios FIJOS (código local = código real del local)
        /// Busca en tabla usuarios donde es_flooter = false y id_local coincide
        /// </summary>
        private async Task<ResultadoAutenticacion> AutenticarUsuarioFijo(string numeroUsuario, string password, string codigoLocal)
        {
            WriteLog($"  → AutenticarUsuarioFijo iniciado");
            WriteLog($"  → Parámetros: Usuario={numeroUsuario}, Local={codigoLocal}");
            
            try
            {
                WriteLog("  → Abriendo conexión a BD...");
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();
                WriteLog("  → Conexión BD abierta exitosamente");

                // Buscar usuario FIJO en el local especificado
                var query = @"
                    SELECT 
                        u.id_usuario,
                        u.numero_usuario,
                        CONCAT(u.nombre, ' ', u.apellidos) as nombre_completo,
                        u.password_hash,
                        u.activo,
                        l.codigo_local,
                        l.nombre_local,
                        l.activo AS local_activo
                    FROM usuarios u
                    INNER JOIN locales l ON u.id_local = l.id_local
                    WHERE u.numero_usuario = @NumeroUsuario
                    AND l.codigo_local = @CodigoLocal
                    AND (u.es_flooter = false OR u.es_flooter IS NULL)";

                WriteLog($"  → Ejecutando query para usuario fijo...");
                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@NumeroUsuario", numeroUsuario);
                cmd.Parameters.AddWithValue("@CodigoLocal", codigoLocal);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    WriteLog("  → No se encontró usuario fijo en ese local");
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario no encontrado o no tiene acceso a este local"
                    };
                }

                WriteLog("  → Usuario fijo encontrado en BD");
                return ProcesarResultadoUsuarioConLocal(reader, password);
            }
            catch (Exception ex)
            {
                WriteLog($"  → ✗✗✗ ERROR en AutenticarUsuarioFijo: {ex.GetType().Name}");
                WriteLog($"  → Mensaje: {ex.Message}");
                WriteLog($"  → StackTrace: {ex.StackTrace}");
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = $"Error al autenticar usuario: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Procesa el resultado de la query de usuario fijo (con validación de local)
        /// Índices: 0=id_usuario, 1=numero_usuario, 2=nombre_completo, 3=password_hash, 
        ///          4=activo, 5=codigo_local, 6=nombre_local, 7=local_activo
        /// </summary>
        private ResultadoAutenticacion ProcesarResultadoUsuarioConLocal(NpgsqlDataReader reader, string password)
        {
            // Verificar si el usuario está activo (índice 4)
            bool usuarioActivo = reader.GetBoolean(4);
            WriteLog($"  → Usuario activo: {usuarioActivo}");
            if (!usuarioActivo)
            {
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = "Usuario inactivo"
                };
            }

            // Verificar si el local está activo (índice 7)
            bool localActivo = reader.GetBoolean(7);
            WriteLog($"  → Local activo: {localActivo}");
            if (!localActivo)
            {
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = "Local inactivo"
                };
            }

            // Verificar contraseña con BCrypt (índice 3)
            var passwordHash = reader.GetString(3);
            WriteLog($"  → Hash almacenado: {passwordHash.Substring(0, Math.Min(20, passwordHash.Length))}...");
            WriteLog("  → Verificando contraseña con BCrypt...");
            
            bool passwordCorrecta = BCrypt.Net.BCrypt.Verify(password, passwordHash);
            WriteLog($"  → Resultado verificación BCrypt: {passwordCorrecta}");
            
            if (!passwordCorrecta)
            {
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = "Contraseña incorrecta"
                };
            }

            // Login exitoso
            var nombreCompleto = reader.GetString(2);
            WriteLog($"  → ✓ Login exitoso para: {nombreCompleto}");
            
            return new ResultadoAutenticacion
            {
                Exitoso = true,
                NombreCompleto = nombreCompleto,
                EsAdministradorAllva = false
            };
        }

        // ============================================
        // VALIDACIONES
        // ============================================

        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(NumeroUsuario))
            {
                WriteLog("  → Validación falló: NumeroUsuario vacío");
                MostrarError("El número de usuario es requerido");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                WriteLog("  → Validación falló: Password vacío");
                MostrarError("La contraseña es requerida");
                return false;
            }

            if (Password.Length < 6)
            {
                WriteLog($"  → Validación falló: Password muy corto ({Password.Length} caracteres)");
                MostrarError("La contraseña debe tener al menos 6 caracteres");
                return false;
            }

            // SIEMPRE requerir código de local/tipo
            // - Admin Allva: debe poner "CENTRAL"
            // - Floater: debe poner "FLOATER"
            // - Usuario fijo: debe poner el código real de su local
            if (string.IsNullOrWhiteSpace(CodigoLocal))
            {
                WriteLog("  → Validación falló: CodigoLocal vacío");
                MostrarError("El código de oficina es requerido");
                return false;
            }

            return true;
        }

        private void MostrarError(string mensaje)
        {
            MensajeError = mensaje;
            MostrarMensajeError = true;

            // Ocultar mensaje después de 5 segundos
            Task.Delay(5000).ContinueWith(_ =>
            {
                MostrarMensajeError = false;
            });
        }
    }

    // ============================================
    // CLASES DE SOPORTE
    // ============================================

    /// <summary>
    /// Resultado de la autenticación
    /// </summary>
    public class ResultadoAutenticacion
    {
        public bool Exitoso { get; set; }
        public string MensajeError { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public bool EsAdministradorAllva { get; set; }
        
        // Permisos de administrador
        public bool AccesoGestionComercios { get; set; }
        public bool AccesoGestionUsuariosLocales { get; set; }
        public bool AccesoGestionUsuariosAllva { get; set; }
        public bool AccesoAnalytics { get; set; }
        public bool AccesoConfiguracionSistema { get; set; }
        public bool AccesoFacturacionGlobal { get; set; }
        public bool AccesoAuditoria { get; set; }
    }
}