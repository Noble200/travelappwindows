using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Allva.Desktop.ViewModels;
using System.Linq;

namespace Allva.Desktop.Views.MenuHamburguesa;

public partial class BalancedeCuentasView : UserControl
{
    private int _idComercio;
    private int _idLocal;
    private string _codigoLocal = "";
    private int _idUsuario;
    private string _nombreUsuario = "";

    public BalancedeCuentasView()
    {
        InitializeComponent();
        DataContext = new BalancedeCuentasViewModel();
    }
    
    public BalancedeCuentasView(int idComercio, int idLocal, string codigoLocal)
    {
        InitializeComponent();
        _idComercio = idComercio;
        _idLocal = idLocal;
        _codigoLocal = codigoLocal;
        var vm = new BalancedeCuentasViewModel(idComercio, idLocal, codigoLocal);
        vm.OnVolverAInicio += VolverADashboard;
        DataContext = vm;
    }
    
    public BalancedeCuentasView(int idComercio, int idLocal, string codigoLocal, int idUsuario, string nombreUsuario)
    {
        InitializeComponent();
        _idComercio = idComercio;
        _idLocal = idLocal;
        _codigoLocal = codigoLocal;
        _idUsuario = idUsuario;
        _nombreUsuario = nombreUsuario;
        var vm = new BalancedeCuentasViewModel(idComercio, idLocal, codigoLocal, idUsuario, nombreUsuario);
        vm.OnVolverAInicio += VolverADashboard;
        DataContext = vm;
    }

    private void VolverADashboard()
    {
        var mainDashboard = this.GetVisualAncestors()
            .OfType<MainDashboardView>()
            .FirstOrDefault();

        if (mainDashboard != null)
        {
            mainDashboard.IrAUltimasNoticias();
        }
    }

    private async void OnFilaOperacionClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is OperacionItem operacion)
        {
            if (operacion.EsClickeable && !string.IsNullOrEmpty(operacion.NumeroOperacion))
            {
                var mainWindow = this.GetVisualAncestors()
                    .OfType<Window>()
                    .FirstOrDefault();

                if (mainWindow != null)
                {
                    // Usar constructor existente de DetalleOperacionDivisaView
                    var vm = new DetalleOperacionDivisaViewModel(operacion.NumeroOperacion, _codigoLocal);
                    var dialog = new DetalleOperacionDivisaView(vm);
                    await dialog.ShowDialog(mainWindow);
                }
            }
        }
    }

    private async void OnFilaPackAlimentosClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is OperacionPackAlimentoBalanceItem operacion)
        {
            if (!string.IsNullOrEmpty(operacion.NumeroOperacion))
            {
                var mainWindow = this.GetVisualAncestors()
                    .OfType<Window>()
                    .FirstOrDefault();

                if (mainWindow != null)
                {
                    var dialog = new DetalleOperacionPackAlimentosView(
                        operacion.NumeroOperacion, 
                        _codigoLocal, 
                        _nombreUsuario, 
                        _idUsuario);
                    await dialog.ShowDialog(mainWindow);
                }
            }
        }
    }
}