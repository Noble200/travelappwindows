using Avalonia.Controls;
using Avalonia.Interactivity;
using Allva.Desktop.ViewModels;

namespace Allva.Desktop.Views;

public partial class MainDashboardView : UserControl
{
    public MainDashboardView()
    {
        InitializeComponent();
        // Inicializar con el primer botón seleccionado
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        UpdateMenuSelection("dashboard");
        UpdateModuleHeader("dashboard");
    }

    private MainDashboardViewModel? ViewModel => DataContext as MainDashboardViewModel;

    // ============================================
    // NAVEGACIÓN DEL MENÚ PRINCIPAL
    // ============================================

    private void NavigateToDashboard(object? sender, RoutedEventArgs e)
    {
        ViewModel?.NavigateToModule("dashboard");
        UpdateMenuSelection("dashboard");
        UpdateModuleHeader("dashboard");
    }

    private void NavigateToDivisas(object? sender, RoutedEventArgs e)
    {
        ViewModel?.NavigateToModule("divisas");
        UpdateMenuSelection("divisas");
        UpdateModuleHeader("divisas");
    }

    private void NavigateToAlimentos(object? sender, RoutedEventArgs e)
    {
        ViewModel?.NavigateToModule("alimentos");
        UpdateMenuSelection("alimentos");
        UpdateModuleHeader("alimentos");
    }

    private void NavigateToBilletes(object? sender, RoutedEventArgs e)
    {
        ViewModel?.NavigateToModule("billetes");
        UpdateMenuSelection("billetes");
        UpdateModuleHeader("billetes");
    }

    private void NavigateToViajes(object? sender, RoutedEventArgs e)
    {
        ViewModel?.NavigateToModule("viajes");
        UpdateMenuSelection("viajes");
        UpdateModuleHeader("viajes");
    }

    // ============================================
    // BOTONES DE ACCIÓN DEL HEADER
    // ============================================

    private void OpenSettings(object? sender, RoutedEventArgs e)
    {
        // TODO: Implementar apertura de configuración
    }

    private void OpenNotifications(object? sender, RoutedEventArgs e)
    {
        // TODO: Implementar apertura de notificaciones
    }

    private void OpenUserProfile(object? sender, RoutedEventArgs e)
    {
        // TODO: Implementar apertura del perfil de usuario
    }

    // ============================================
    // SESIÓN
    // ============================================

    private void CerrarSesion(object? sender, RoutedEventArgs e)
    {
        ViewModel?.Logout();
    }

    // ============================================
    // UTILIDADES
    // ============================================

    private void UpdateMenuSelection(string selectedModule)
    {
        BtnDashboard.Classes.Set("menu-item-selected", selectedModule == "dashboard");
        BtnDashboard.Classes.Set("menu-item", selectedModule != "dashboard");
        
        BtnDivisas.Classes.Set("menu-item-selected", selectedModule == "divisas");
        BtnDivisas.Classes.Set("menu-item", selectedModule != "divisas");
        
        BtnAlimentos.Classes.Set("menu-item-selected", selectedModule == "alimentos");
        BtnAlimentos.Classes.Set("menu-item", selectedModule != "alimentos");
        
        BtnBilletes.Classes.Set("menu-item-selected", selectedModule == "billetes");
        BtnBilletes.Classes.Set("menu-item", selectedModule != "billetes");
        
        BtnViajes.Classes.Set("menu-item-selected", selectedModule == "viajes");
        BtnViajes.Classes.Set("menu-item", selectedModule != "viajes");
    }

    private void UpdateModuleHeader(string moduleName)
    {
        (string title, string description) = moduleName.ToLower() switch
        {
            "dashboard" => ("Últimas Noticias", "Mantente informado con las últimas novedades"),
            "divisas" => ("Compra de Divisas", "Gestiona operaciones de cambio de moneda"),
            "alimentos" => ("Pack de Alimentos", "Administra paquetes de alimentación"),
            "billetes" => ("Billetes de Avión", "Reserva y gestión de vuelos"),
            "viajes" => ("Packs de Viajes", "Paquetes turísticos completos"),
            _ => ("Últimas Noticias", "Mantente informado con las últimas novedades")
        };

        TxtModuleTitle.Text = title;
        TxtModuleDescription.Text = description;
    }
}