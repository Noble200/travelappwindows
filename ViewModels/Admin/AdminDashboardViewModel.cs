using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models;
using Allva.Desktop.Services;
using Allva.Desktop.Views.Admin;

namespace Allva.Desktop.ViewModels.Admin;

public partial class AdminDashboardViewModel : ObservableObject
{
    private readonly NavigationService? _navigationService;
    private readonly MenuHamburguesaService _menuService;
    private Services.PermisosAdministrador? _permisos;

    [ObservableProperty]
    private UserControl? _currentView;

    [ObservableProperty]
    private string _adminName = "Administrador";

    [ObservableProperty]
    private string _localCode = "CENTRAL";

    [ObservableProperty]
    private string _selectedModule = "comercios";

    [ObservableProperty]
    private ObservableCollection<MenuHamburguesaItem> _menuHamburguesaItems = new();

    // ============================================
    // SIDEBAR COLAPSABLE
    // ============================================

    [ObservableProperty]
    private bool _sidebarVisible = true;

    partial void OnSidebarVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(SidebarWidth));
    }

    public double SidebarWidth => SidebarVisible ? 240 : 0;

    [RelayCommand]
    private void MostrarSidebar()
    {
        SidebarVisible = true;
    }

    [RelayCommand]
    private void OcultarSidebar()
    {
        SidebarVisible = false;
    }

    // ============================================
    // PROPIEDADES
    // ============================================

    public string SelectedModuleTitle
    {
        get
        {
            if (_menuService.EsModuloMenuHamburguesa(SelectedModule))
            {
                return _menuService.ObtenerTituloModulo(SelectedModule);
            }
            
            return SelectedModule switch
            {
                "informes" => "INFORMES Y ANALITICAS",
                "comercios" => "GESTION DE COMERCIOS",
                "usuarios" => "GESTION DE USUARIOS",
                "divisas" => "MARGEN BENEFICIOS",
                "suscripciones" => "PAGO SUSCRIPCIONES",
                "balance" => "BALANCE DE CUENTAS",
                "operaciones" => "OPERACIONES",
                _ => "PANEL DE ADMINISTRACION"
            };
        }
    }

    public bool MostrarInformes => true;
    public bool MostrarGestionComercios => _permisos?.AccesoGestionComercios ?? true;
    public bool MostrarGestionUsuarios => _permisos?.AccesoGestionUsuariosLocales ?? true;
    public bool MostrarDivisas => true;
    public bool MostrarSuscripciones => true;
    public bool MostrarBalance => true;

    // ============================================
    // CONSTRUCTORES
    // ============================================

    public AdminDashboardViewModel()
    {
        _menuService = MenuHamburguesaService.Instance;
        CargarMenuHamburguesa();
        NavigateToModule("comercios");
    }

    public AdminDashboardViewModel(string adminName)
    {
        _menuService = MenuHamburguesaService.Instance;
        AdminName = adminName;
        CargarMenuHamburguesa();
        NavigateToModule("comercios");
    }
    
    public AdminDashboardViewModel(LoginSuccessData loginData)
    {
        _menuService = MenuHamburguesaService.Instance;
        AdminName = loginData.UserName;
        LocalCode = loginData.LocalCode;
        _permisos = loginData.Permisos;
        CargarMenuHamburguesa();
        NavigateToModule("comercios");
    }
    
    public AdminDashboardViewModel(string adminName, NavigationService navigationService)
    {
        _menuService = MenuHamburguesaService.Instance;
        AdminName = adminName;
        _navigationService = navigationService;
        CargarMenuHamburguesa();
        NavigateToModule("comercios");
    }

    private void CargarMenuHamburguesa()
    {
        MenuHamburguesaItems.Clear();
        foreach (var item in _menuService.ObtenerItemsHabilitados())
        {
            MenuHamburguesaItems.Add(item);
        }
    }

    // ============================================
    // COMANDOS DE NAVEGACION
    // ============================================

    [RelayCommand]
    private void NavigateToModule(string? moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
            return;
            
        var module = moduleName.ToLower();
        SelectedModule = module;
        
        if (_menuService.EsModuloMenuHamburguesa(module))
        {
            CurrentView = _menuService.CrearVistaParaItem(module);
        }
        else
        {
            CurrentView = module switch
            {
                "informes" => new InformesAnaliticasView(),
                "comercios" => new ManageComerciosView(),
                "usuarios" => new ManageUsersView(),
                "divisas" => new ComisionesView(),
                "suscripciones" => new PagoSuscripcionesView(),
                "balance" => new BalanceAdminView(),
                "operaciones" => new OperacionesAdminView(),
                _ => CurrentView
            };
        }
        
        OnPropertyChanged(nameof(SelectedModuleTitle));
    }

    [RelayCommand]
    private void NavigateToMenuHamburguesaItem(MenuHamburguesaItem? item)
    {
        if (item == null) return;
        NavigateToModule(item.Id);
    }

    [RelayCommand]
    private void Logout()
    {
        if (_navigationService != null)
        {
            _navigationService.NavigateTo("Login");
        }
        else
        {
            var navigationService = new NavigationService();
            navigationService.NavigateToLogin();
        }
    }
}