using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Views.Admin.MenuHamburguesa;

public partial class OperacionesAdminView : UserControl
{
    public OperacionesAdminView()
    {
        InitializeComponent();
        DataContext = new OperacionesAdminViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}