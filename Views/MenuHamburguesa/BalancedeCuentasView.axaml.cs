using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels;

namespace Allva.Desktop.Views.MenuHamburguesa;

public partial class BalancedeCuentasView : UserControl
{
    public BalancedeCuentasView()
    {
        InitializeComponent();
    }

    public BalancedeCuentasView(int idComercio, int idLocal, string codigoLocal)
    {
        InitializeComponent();
        DataContext = new BalancedeCuentasViewModel(idComercio, idLocal, codigoLocal);
    }
    
    public BalancedeCuentasView(int idComercio, int idLocal, string codigoLocal, int idUsuario, string nombreUsuario)
    {
        InitializeComponent();
        DataContext = new BalancedeCuentasViewModel(idComercio, idLocal, codigoLocal, idUsuario, nombreUsuario);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}