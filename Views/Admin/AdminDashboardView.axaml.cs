using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Views.Admin
{
    public partial class AdminDashboardView : UserControl
    {
        public AdminDashboardView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private AdminDashboardViewModel? ViewModel => DataContext as AdminDashboardViewModel;

        private void UserButton_Click(object? sender, RoutedEventArgs e)
        {
            var popup = this.FindControl<Popup>("UserPopup");
            if (popup != null)
            {
                popup.IsOpen = !popup.IsOpen;
            }
        }

        private void MenuButton_Click(object? sender, RoutedEventArgs e)
        {
            var popup = this.FindControl<Popup>("MenuPopup");
            if (popup != null)
            {
                popup.IsOpen = !popup.IsOpen;
            }
        }

        private void MenuHamburguesaItem_Click(object? sender, RoutedEventArgs e)
        {
            var popup = this.FindControl<Popup>("MenuPopup");
            if (popup != null)
            {
                popup.IsOpen = false;
            }
        }
    }
}