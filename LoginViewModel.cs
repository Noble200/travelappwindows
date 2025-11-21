using System;
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
    /// Soporta login de:
    /// - Administradores Allva (código = CENTRAL)
    /// - Usuarios Floater (código = FLOATER)
    /// - Usuarios Fijos (código = código real del local)
    /// </summary>
    public partial class LoginViewModel : ObservableObject
    {
        // ============================================
        // CONFIGURACIÓN DE BASE DE DATOS
        // ============================================
        
        private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

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
        
        public bool IsLoading => Cargando;
        
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
            MensajeError = string.Empty;
            MostrarMensajeError = false;

            if (!ValidarCampos())
                return;

            Cargando = true;

            try
            {
                var codigoLocalUpper = CodigoLocal.Trim().ToUpper();
                
                // =============================================
                // CASO 1: ADMINISTRADOR ALLVA (código = CENTRAL)
                // =============================================
                if (codigoLocalUpper == "CENTRAL")
                {
                    var resultadoAdmin = await AutenticarAdministradorAllva(NumeroUsuario!, Password!);
                    
                    if (resultadoAdmin.Exitoso)
                    {
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

                        var navigationService = new NavigationService();
                        navigationService.NavigateToAdminDashboard(loginData);
                        return;
                    }
                    else
                    {
                        MostrarError(resultadoAdmin.MensajeError);
                        return;
                    }
                }
                
                // =============================================
                // CASO 2: USUARIO FLOATER (código = FLOATER)
                // =============================================
                if (codigoLocalUpper == "FLOATER")
                {
                    var resultadoFloater = await AutenticarUsuarioFloater(NumeroUsuario!, Password!);
                    
                    if (resultadoFloater.Exitoso)
                    {
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

                        var navigationService = new NavigationService();
                        navigationService.NavigateTo("MainDashboard", loginData);
                        return;
                    }
                    else
                    {
                        MostrarError(resultadoFloater.MensajeError);
                        return;
                    }
                }
                
                // =============================================
                // CASO 3: USUARIO FIJO (código = código real del local)
                // =============================================
                var resultadoNormal = await AutenticarUsuarioFijo(NumeroUsuario!, Password!, codigoLocalUpper);
                
                if (resultadoNormal.Exitoso)
                {
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

                    var navigationService = new NavigationService();
                    navigationService.NavigateTo("MainDashboard", loginData);
                }
                else
                {
                    MostrarError(resultadoNormal.MensajeError);
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error de conexión: {ex.Message}");
            }
            finally
            {
                Cargando = false;
            }
        }
        
        public IAsyncRelayCommand LoginCommand => IniciarSesionCommand;

        [RelayCommand]
        private void ToggleMostrarPassword()
        {
            MostrarPassword = !MostrarPassword;
        }
        
        [RelayCommand]
        private void CambiarIdioma(string idioma)
        {
            // TODO: Implementar cambio de idioma
        }
        
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
        /// Autentica administradores de Allva
        /// </summary>
        private async Task<ResultadoAutenticacion> AutenticarAdministradorAllva(string nombreUsuario, string password)
        {
            try
            {
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

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

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@NombreUsuario", nombreUsuario);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario administrador no encontrado"
                    };
                }

                if (!reader.GetBoolean(4))
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario administrador inactivo"
                    };
                }

                var passwordHash = reader.GetString(3);
                if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Contraseña incorrecta"
                    };
                }

                var nombreCompleto = $"{reader.GetString(1)} {reader.GetString(2)}";

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
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = $"Error al autenticar administrador: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Autentica usuarios FLOATER (código local = "FLOATER")
        /// </summary>
        private async Task<ResultadoAutenticacion> AutenticarUsuarioFloater(string numeroUsuario, string password)
        {
            try
            {
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

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

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@NumeroUsuario", numeroUsuario);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario floater no encontrado"
                    };
                }

                if (!reader.GetBoolean(4))
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario inactivo"
                    };
                }

                var passwordHash = reader.GetString(3);
                if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Contraseña incorrecta"
                    };
                }

                return new ResultadoAutenticacion
                {
                    Exitoso = true,
                    NombreCompleto = reader.GetString(2),
                    EsAdministradorAllva = false
                };
            }
            catch (Exception ex)
            {
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = $"Error al autenticar floater: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Autentica usuarios FIJOS (código local = código real del local)
        /// </summary>
        private async Task<ResultadoAutenticacion> AutenticarUsuarioFijo(string numeroUsuario, string password, string codigoLocal)
        {
            try
            {
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

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

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@NumeroUsuario", numeroUsuario);
                cmd.Parameters.AddWithValue("@CodigoLocal", codigoLocal);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario no encontrado o no tiene acceso a este local"
                    };
                }

                if (!reader.GetBoolean(4))
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario inactivo"
                    };
                }

                if (!reader.GetBoolean(7))
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Local inactivo"
                    };
                }

                var passwordHash = reader.GetString(3);
                if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Contraseña incorrecta"
                    };
                }

                return new ResultadoAutenticacion
                {
                    Exitoso = true,
                    NombreCompleto = reader.GetString(2),
                    EsAdministradorAllva = false
                };
            }
            catch (Exception ex)
            {
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = $"Error al autenticar usuario: {ex.Message}"
                };
            }
        }

        // ============================================
        // VALIDACIONES
        // ============================================

        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(NumeroUsuario))
            {
                MostrarError("El número de usuario es requerido");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                MostrarError("La contraseña es requerida");
                return false;
            }

            if (Password.Length < 6)
            {
                MostrarError("La contraseña debe tener al menos 6 caracteres");
                return false;
            }

            if (string.IsNullOrWhiteSpace(CodigoLocal))
            {
                MostrarError("El código de oficina es requerido");
                return false;
            }

            return true;
        }

        private void MostrarError(string mensaje)
        {
            MensajeError = mensaje;
            MostrarMensajeError = true;

            Task.Delay(5000).ContinueWith(_ =>
            {
                MostrarMensajeError = false;
            });
        }
    }

    // ============================================
    // CLASES DE SOPORTE
    // ============================================

    public class ResultadoAutenticacion
    {
        public bool Exitoso { get; set; }
        public string MensajeError { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public bool EsAdministradorAllva { get; set; }
        
        public bool AccesoGestionComercios { get; set; }
        public bool AccesoGestionUsuariosLocales { get; set; }
        public bool AccesoGestionUsuariosAllva { get; set; }
        public bool AccesoAnalytics { get; set; }
        public bool AccesoConfiguracionSistema { get; set; }
        public bool AccesoFacturacionGlobal { get; set; }
        public bool AccesoAuditoria { get; set; }
    }
}