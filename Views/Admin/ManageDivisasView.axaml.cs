using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Views.Admin;

public partial class ManageDivisasView : UserControl
{
    public ManageDivisasView()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = new ManageDivisasViewModel();
    }
}