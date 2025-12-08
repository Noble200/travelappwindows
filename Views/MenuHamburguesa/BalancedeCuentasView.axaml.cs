using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels;

namespace Allva.Desktop.Views.MenuHamburguesa;

public partial class BalancedeCuentasView : UserControl
{
    private string _codigoLocal = "";
    
    public BalancedeCuentasView()
    {
        InitializeComponent();
    }
    
    public BalancedeCuentasView(int idComercio, int idLocal, string codigoLocal, int idUsuario, string nombreUsuario) : this()
    {
        _codigoLocal = codigoLocal;
        DataContext = new BalancedeCuentasViewModel(idComercio, idLocal, codigoLocal, idUsuario, nombreUsuario);
    }
    
    private async void OnFilaOperacionClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border) return;
        if (border.DataContext is not OperacionItem operacion) return;
        
        // Solo abrir popup si tiene numero de operacion (es una compra de divisa)
        if (string.IsNullOrEmpty(operacion.NumeroOperacion)) return;
        
        // Obtener codigo local
        var codigoLocal = _codigoLocal;
        if (string.IsNullOrEmpty(codigoLocal))
        {
            var vm = DataContext as BalancedeCuentasViewModel;
            if (vm != null && !string.IsNullOrEmpty(vm.LocalInfo))
            {
                var inicio = vm.LocalInfo.IndexOf("- ");
                var fin = vm.LocalInfo.IndexOf(")");
                if (inicio >= 0 && fin > inicio)
                {
                    codigoLocal = vm.LocalInfo.Substring(inicio + 2, fin - inicio - 2);
                }
            }
        }
        
        // Crear ViewModel del popup
        var detalleVm = new DetalleOperacionDivisaViewModel(operacion.NumeroOperacion, codigoLocal);
        var ventana = new DetalleOperacionDivisaView(detalleVm);
        
        // Mostrar como dialogo modal
        Window? mainWindow = null;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            mainWindow = desktop.MainWindow;
        }
        
        if (mainWindow != null)
        {
            await ventana.ShowDialog(mainWindow);
        }
        else
        {
            ventana.Show();
        }
    }
}