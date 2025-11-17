using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Allva.Desktop.Services;
using Allva.Desktop.ViewModels;
using Allva.Desktop.Views;
using Velopack;

namespace Allva.Desktop
{
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // ============================================
            // VELOPACK: Debe ser lo PRIMERO
            // ============================================
            VelopackApp.Build().Run();

            // Configurar servicios
            ConfigureServices();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var licenseService = new LicenseService();

                // VERIFICAR SI LA APP ESTÁ ACTIVADA
                if (!licenseService.EstaActivada())
                {
                    // NO ACTIVADA - Mostrar ventana de activación EN PANTALLA COMPLETA
                    var licenseView = new LicenseActivationView
                    {
                        DataContext = new LicenseActivationViewModel(),
                        WindowState = WindowState.Maximized  // ← PANTALLA COMPLETA
                    };

                    desktop.MainWindow = licenseView;
                }
                else
                {
                    // YA ACTIVADA - Ir directo al login EN PANTALLA COMPLETA
                    var loginView = new LoginView
                    {
                        DataContext = new LoginViewModel()
                    };

                    var mainWindow = new Window
                    {
                        Title = "Allva System - Login",
                        WindowState = WindowState.Maximized,  // ← PANTALLA COMPLETA
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        CanResize = true,
                        Content = loginView,
                        Icon = CargarIcono()
                    };

                    desktop.MainWindow = mainWindow;
                    
                    // Verificar actualizaciones en segundo plano
                    Task.Run(CheckForUpdatesInBackground);
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<LocalizationService>();
            services.AddSingleton<NavigationService>();
            services.AddSingleton<UpdateService>();
            services.AddSingleton<LicenseService>();

            services.AddTransient<LoginViewModel>();
            services.AddTransient<LicenseActivationViewModel>();

            Services = services.BuildServiceProvider();
        }

        private async Task CheckForUpdatesInBackground()
        {
            await Task.Delay(5000);

            try
            {
                var updateService = new UpdateService();
                var updateInfo = await updateService.CheckForUpdatesAsync();

                if (updateInfo != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Actualización disponible: {updateInfo.TargetFullRelease.Version}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error verificando actualizaciones: {ex.Message}");
            }
        }

        private WindowIcon? CargarIcono()
        {
            try
            {
                var uri = new Uri("avares://Allva.Desktop/Assets/allva-icon.ico");
                return new WindowIcon(AssetLoader.Open(uri));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"No se pudo cargar el icono: {ex.Message}");
                return null;
            }
        }
    }
}