using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models;
using Allva.Desktop.Services;
using Allva.Desktop.Views.Admin;

namespace Allva.Desktop.ViewModels.Admin;

/// <summary>
/// ViewModel para el Panel de Administración
/// Exclusivo para administradores del sistema
/// Los módulos del menú hamburguesa se cargan dinámicamente desde MenuHamburguesaService
/// </summary>
public partial class AdminDashboardViewModel : ObservableObject
{
    private readonly NavigationService? _navigationService;
    private readonly MenuHamburguesaService _menuService;
    private PermisosAdministrador? _permisos;
    
    // ============================================
    // PROPIEDADES OBSERVABLES
    // ============================================

    [ObservableProperty]
    private UserControl? _currentView;

    [ObservableProperty]
    private string _adminName = "Administrador";

    [ObservableProperty]
    private string _selectedModule = "comercios";

    /// <summary>
    /// Items del menú hamburguesa cargados dinámicamente
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<MenuHamburguesaItem> _menuHamburguesaItems = new();

    /// <summary>
    /// Título del módulo seleccionado para mostrar en UI
    /// </summary>
    public string SelectedModuleTitle
    {
        get
        {
            // Primero verificar si es un módulo del menú hamburguesa
            if (_menuService.EsModuloMenuHamburguesa(SelectedModule))
            {
                return _menuService.ObtenerTituloModulo(SelectedModule);
            }
            
            // Módulos principales del sidebar
            return SelectedModule switch
            {
                "comercios" => "GESTIÓN DE COMERCIOS",
                "usuarios" => "GESTIÓN DE USUARIOS",
                "usuarios_allva" => "USUARIOS ALLVA",
                _ => "PANEL DE ADMINISTRACIÓN"
            };
        }
    }

    // ============================================
    // PROPIEDADES PARA VISIBILIDAD DE MÓDULOS
    // ============================================

    public bool MostrarGestionComercios => _permisos?.AccesoGestionComercios ?? true;
    public bool MostrarGestionUsuarios => _permisos?.AccesoGestionUsuariosLocales ?? true;
    public bool MostrarUsuariosAllva => _permisos?.AccesoGestionUsuariosAllva ?? false;

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

    // ============================================
    // MÉTODOS PRIVADOS
    // ============================================

    /// <summary>
    /// Carga los items del menú hamburguesa desde el servicio
    /// </summary>
    private void CargarMenuHamburguesa()
    {
        MenuHamburguesaItems.Clear();
        foreach (var item in _menuService.ObtenerItemsHabilitados())
        {
            MenuHamburguesaItems.Add(item);
        }
    }

    // ============================================
    // COMANDOS
    // ============================================

    /// <summary>
    /// Navega a un módulo específico (sidebar o menú hamburguesa)
    /// </summary>
    [RelayCommand]
    private void NavigateToModule(string? moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
            return;
            
        var module = moduleName.ToLower();
        SelectedModule = module;
        
        // Verificar si es un módulo del menú hamburguesa
        if (_menuService.EsModuloMenuHamburguesa(module))
        {
            CurrentView = _menuService.CrearVistaParaItem(module);
        }
        else
        {
            // Módulos principales del sidebar
            CurrentView = module switch
            {
                "comercios" => new ManageComerciosView(),
                "usuarios" => new ManageUsersView(),
                "usuarios_allva" => new ManageAdministradoresAllvaView(),
                _ => CurrentView
            };
        }
        
        OnPropertyChanged(nameof(SelectedModuleTitle));
    }

    /// <summary>
    /// Navega a un item específico del menú hamburguesa
    /// </summary>
    [RelayCommand]
    private void NavigateToMenuHamburguesaItem(MenuHamburguesaItem? item)
    {
        if (item == null) return;
        NavigateToModule(item.Id);
    }

    /// <summary>
    /// Cierra sesión y vuelve al login
    /// </summary>
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