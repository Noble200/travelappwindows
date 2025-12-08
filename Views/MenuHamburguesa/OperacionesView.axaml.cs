using Avalonia.Controls;
using Allva.Desktop.ViewModels;

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
        DataContext = new OperacionesViewModel(idComercio, idLocal, codigoLocal, idUsuario, nombreUsuario);
    }
}