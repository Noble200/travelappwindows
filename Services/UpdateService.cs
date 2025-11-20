using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Allva.Desktop.Services
{
    public class UpdateService
    {
        private readonly UpdateManager? _updateManager;
        private readonly bool _isUpdateAvailable;

        // ============================================
        // CONFIGURACIÓN PARA RAILWAY
        // ============================================
        
        // ⚠️ IMPORTANTE: Reemplaza esta URL por la que Railway te generó
        // Después de hacer "Generate Domain" en Railway, copia la URL aquí
        private const string RAILWAY_UPDATE_URL = "https://allva-updates-server-production.up.railway.app";
        
        private static string GetUpdateUrl()
        {
            #if DEBUG
            // En desarrollo, puedes usar Railway o local
            return RAILWAY_UPDATE_URL;
            // Descomentar para usar local:
            // return LOCAL_UPDATE_URL;
            #else
            // En producción siempre Railway
            return RAILWAY_UPDATE_URL;
            #endif
        }

        public UpdateService()
        {
            try
            {
                var updateUrl = GetUpdateUrl();
                
                #if DEBUG
                Console.WriteLine("🔧 Sistema de Actualizaciones - Allva System");
                Console.WriteLine($"📡 Servidor: {updateUrl}");
                #endif
                
                _updateManager = new UpdateManager(
                    new SimpleWebSource(updateUrl)
                );
                
                _isUpdateAvailable = true;
                
                #if DEBUG
                Console.WriteLine("✓ Sistema de actualizaciones inicializado correctamente");
                #endif
            }
            catch (Exception ex)
            {
                #if DEBUG
                Console.WriteLine($"✗ Error inicializando actualizaciones: {ex.Message}");
                #endif
                _updateManager = null;
                _isUpdateAvailable = false;
            }
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            if (!_isUpdateAvailable || _updateManager == null)
            {
                #if DEBUG
                Console.WriteLine("⚠️ Sistema de actualizaciones no disponible");
                #endif
                return null;
            }

            try
            {
                #if DEBUG
                Console.WriteLine("🔍 Verificando actualizaciones en Railway...");
                #endif
                
                var updateInfo = await _updateManager.CheckForUpdatesAsync();
                
                #if DEBUG
                if (updateInfo != null)
                {
                    Console.WriteLine($"✓ ¡Actualización disponible!");
                    Console.WriteLine($"   Versión actual: {CurrentVersion}");
                    Console.WriteLine($"   Versión nueva: {updateInfo.TargetFullRelease.Version}");
                }
                else
                {
                    Console.WriteLine($"✓ La aplicación está actualizada (versión {CurrentVersion})");
                }
                #endif
                
                return updateInfo;
            }
            catch (Exception ex)
            {
                #if DEBUG
                Console.WriteLine($"✗ Error verificando actualizaciones: {ex.Message}");
                
                // Diagnóstico de errores comunes
                if (ex.Message.Contains("404"))
                {
                    Console.WriteLine("   📌 Causa: Archivo RELEASES no encontrado en el servidor");
                    Console.WriteLine($"   📌 Verifica: {GetUpdateUrl()}/RELEASES");
                }
                else if (ex.Message.Contains("timeout") || ex.Message.Contains("timed out"))
                {
                    Console.WriteLine("   📌 Causa: Servidor Railway dormido (se despierta automáticamente)");
                    Console.WriteLine("   📌 Espera 30 segundos e intenta nuevamente");
                }
                else if (ex.Message.Contains("could not be resolved") || ex.Message.Contains("DNS"))
                {
                    Console.WriteLine("   📌 Causa: No hay conexión a internet o DNS no resuelve");
                }
                #endif
                
                return null;
            }
        }

        public async Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progressCallback = null)
        {
            if (_updateManager == null || updateInfo == null)
            {
                #if DEBUG
                Console.WriteLine("⚠️ No se puede descargar: parámetros inválidos");
                #endif
                return;
            }

            try
            {
                #if DEBUG
                Console.WriteLine("📥 Descargando actualización desde Railway...");
                #endif
                
                await _updateManager.DownloadUpdatesAsync(updateInfo, progressCallback);
                
                #if DEBUG
                Console.WriteLine("✓ Actualización descargada correctamente");
                #endif
            }
            catch (Exception ex)
            {
                #if DEBUG
                Console.WriteLine($"✗ Error descargando actualización: {ex.Message}");
                #endif
                throw;
            }
        }

        public void ApplyUpdatesAndRestart(UpdateInfo updateInfo)
        {
            if (_updateManager == null || updateInfo == null)
            {
                #if DEBUG
                Console.WriteLine("⚠️ No se puede aplicar: parámetros inválidos");
                #endif
                return;
            }

            try
            {
                #if DEBUG
                Console.WriteLine("🔄 Aplicando actualización y reiniciando aplicación...");
                #endif
                
                _updateManager.ApplyUpdatesAndRestart(updateInfo);
            }
            catch (Exception ex)
            {
                #if DEBUG
                Console.WriteLine($"✗ Error aplicando actualización: {ex.Message}");
                #endif
                throw;
            }
        }

        public string CurrentVersion => _updateManager?.CurrentVersion?.ToString() ?? "1.0.0";

        public bool IsUpdateSystemAvailable => _isUpdateAvailable;
        
        public string UpdateUrl => GetUpdateUrl();
    }
}