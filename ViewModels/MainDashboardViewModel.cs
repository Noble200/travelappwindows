using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Allva.Desktop.Views;
using Allva.Desktop.Views.PanelDivisas;
using Allva.Desktop.Services;

namespace Allva.Desktop.ViewModels;

/// <summary>
/// ViewModel para el Dashboard principal con menú de navegación
/// Almacena los datos de sesión del usuario logueado
/// </summary>
public partial class MainDashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private UserControl? _currentView;

    [ObservableProperty]
    private string _userName = "Usuario";

    [ObservableProperty]
    private string _localCode = "CENTRAL";

    [ObservableProperty]
    private string _selectedModule = "dashboard";

    // ============================================
    // DATOS DE SESIÓN DEL USUARIO
    // ============================================
    
    private int _idUsuario = 0;
    private int _idLocal = 0;
    private int _idComercio = 0;
    private string _numeroUsuario = "";
    
    public int IdUsuario => _idUsuario;
    public int IdLocal => _idLocal;
    public int IdComercio => _idComercio;
    public string NumeroUsuario => _numeroUsuario;

    public MainDashboardViewModel()
    {
        // Cargar vista inicial (Últimas Noticias)
        NavigateToModule("dashboard");
    }

    /// <summary>
    /// Constructor con datos de usuario (básico)
    /// </summary>
    public MainDashboardViewModel(string userName, string localCode)
    {
        UserName = userName;
        LocalCode = localCode;
        NavigateToModule("dashboard");
    }
    
    /// <summary>
    /// Constructor COMPLETO con todos los datos de sesión
    /// Este es el que debe usarse desde el Login
    /// </summary>
    public MainDashboardViewModel(int idUsuario, string nombreUsuario, string numeroUsuario, 
                                   int idLocal, string codigoLocal, int idComercio)
    {
        _idUsuario = idUsuario;
        _idLocal = idLocal;
        _idComercio = idComercio;
        _numeroUsuario = numeroUsuario;
        
        UserName = nombreUsuario;
        LocalCode = codigoLocal;
        
        NavigateToModule("dashboard");
    }

    /// <summary>
    /// Método para establecer los datos de sesión después de crear el ViewModel
    /// Útil si se usa el constructor vacío
    /// </summary>
    public void SetSesionData(int idUsuario, string nombreUsuario, string numeroUsuario,
                               int idLocal, string codigoLocal, int idComercio)
    {
        _idUsuario = idUsuario;
        _idLocal = idLocal;
        _idComercio = idComercio;
        _numeroUsuario = numeroUsuario;
        
        UserName = nombreUsuario;
        LocalCode = codigoLocal;
    }

    /// <summary>
    /// Navega a un módulo específico
    /// </summary>
    public void NavigateToModule(string moduleName)
    {
        SelectedModule = moduleName;

        CurrentView = moduleName.ToLower() switch
        {
            "dashboard" or "ultimasnoticias" => CreateLatestNewsView(),
            "divisas" => CreateCurrencyExchangeView(),
            "alimentos" => CreateFoodPacksView(),
            "billetes" => CreateFlightTicketsView(),
            "viajes" => CreateTravelPacksView(),
            _ => CreateLatestNewsView()
        };
    }

    /// <summary>
    /// Cierra sesión y vuelve al login
    /// </summary>
    public void Logout()
    {
        var navigationService = new NavigationService();
        navigationService.NavigateToLogin();
    }

    // ============================================
    // MÉTODOS PRIVADOS PARA CREAR VISTAS
    // ============================================

    private UserControl CreateLatestNewsView()
    {
        var view = new LatestNewsView();
        view.DataContext = new LatestNewsViewModel();
        return view;
    }

    private UserControl CreateCurrencyExchangeView()
    {
        var view = new CurrencyExchangePanelView();
        // CAMBIO: Pasar los datos de sesión al ViewModel de divisas
        var viewModel = new CurrencyExchangePanelViewModel(
            _idLocal, 
            _idComercio, 
            _idUsuario, 
            UserName, 
            _numeroUsuario
        );
        view.DataContext = viewModel;
        return view;
    }

    private UserControl CreateFoodPacksView()
    {
        var view = new FoodPacksView();
        view.DataContext = new FoodPacksViewModel();
        return view;
    }

    private UserControl CreateFlightTicketsView()
    {
        var view = new FlightTicketsView();
        view.DataContext = new FlightTicketsViewModel();
        return view;
    }

    private UserControl CreateTravelPacksView()
    {
        var view = new TravelPacksView();
        view.DataContext = new TravelPacksViewModel();
        return view;
    }
}