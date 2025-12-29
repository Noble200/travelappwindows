using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.IO;
using System.Linq;
using Allva.Desktop.Helpers;

namespace Allva.Desktop.Views.PanelDivisas;

public partial class EditarClienteView : UserControl
{
    public EditarClienteView()
    {
        InitializeComponent();
        
        // Configurar botones de seleccion de imagen
        var btnFrontal = this.FindControl<Button>("BtnSeleccionarFrontal");
        var btnTrasera = this.FindControl<Button>("BtnSeleccionarTrasera");
        
        if (btnFrontal != null)
            btnFrontal.Click += BtnSeleccionarFrontal_Click;
        
        if (btnTrasera != null)
            btnTrasera.Click += BtnSeleccionarTrasera_Click;

        // Conectar eventos para formateo de teléfono
        TextBoxFormatHelper.ConfigurarFormatoTelefono(this.FindControl<TextBox>("TxtTelefono"));
    }
    
    private async void BtnSeleccionarFrontal_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar imagen frontal del documento",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Imagenes") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" } }
            }
        });
        
        if (files.Count > 0)
        {
            var file = files.First();
            await using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            
            if (DataContext is ViewModels.CurrencyExchangePanelViewModel vm)
            {
                vm.SetImagenDocumentoFrontal(bytes);
            }
        }
    }
    
    private async void BtnSeleccionarTrasera_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar imagen trasera del documento",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Imagenes") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" } }
            }
        });
        
        if (files.Count > 0)
        {
            var file = files.First();
            await using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            
            if (DataContext is ViewModels.CurrencyExchangePanelViewModel vm)
            {
                vm.SetImagenDocumentoTrasera(bytes);
            }
        }
    }
}
