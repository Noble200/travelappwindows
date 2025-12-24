using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Views.Admin.MenuHamburguesa;

namespace Allva.Desktop.ViewModels.Admin
{
    public partial class EdicionFrontOfficeViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _tabSeleccionada = "packs";

        [ObservableProperty]
        private bool _esTabPacksAlimentos = true;

        [ObservableProperty]
        private bool _esTabUltimasNoticias;

        [ObservableProperty]
        private UserControl? _vistaActual;

        public EdicionFrontOfficeViewModel()
        {
            // Iniciar con la tab de Packs de Alimentos
            CambiarTab("packs");
        }

        [RelayCommand]
        private void CambiarTab(string tab)
        {
            TabSeleccionada = tab;

            EsTabPacksAlimentos = tab == "packs";
            EsTabUltimasNoticias = tab == "noticias";

            if (tab == "packs")
            {
                VistaActual = new PacksAlimentosView();
            }
            else if (tab == "noticias")
            {
                VistaActual = new UltimasNoticiasAdminView();
            }
        }
    }
}
