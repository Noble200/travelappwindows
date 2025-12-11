using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Allva.Desktop.ViewModels;
using System.Linq;

namespace Allva.Desktop.Views.MenuHamburguesa;

public partial class OperacionesView : UserControl
{
    public OperacionesView()
    {
        InitializeComponent();
        DataContext = new OperacionesViewModel();
    }
    
    public OperacionesView(int idComercio, int idLocal, string codigoLocal)
    {
        InitializeComponent();
        DataContext = new OperacionesViewModel(idComercio, idLocal, codigoLocal);
    }
    
    public OperacionesView(int idComercio, int idLocal, string codigoLocal, int idUsuario, string nombreUsuario)
    {
        InitializeComponent();
        var vm = new OperacionesViewModel(idComercio, idLocal, codigoLocal, idUsuario, nombreUsuario);
        vm.OnVolverAInicio += VolverADashboard;
        DataContext = vm;
    }

    private void VolverADashboard()
    {
        // Buscar el MainDashboardView padre
        var mainDashboard = this.GetVisualAncestors()
            .OfType<MainDashboardView>()
            .FirstOrDefault();

        if (mainDashboard != null)
        {
            // Llamar al método público para navegar
            mainDashboard.IrAUltimasNoticias();
        }
    }
}