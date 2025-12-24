using Avalonia.Controls;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Views.Admin.MenuHamburguesa;

public partial class UsuariosAllvaView : UserControl
{
    public UsuariosAllvaView()
    {
        InitializeComponent();
        DataContext = new ManageAdministradoresAllvaViewModel();
    }
}
