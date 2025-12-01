using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Allva.Desktop.ViewModels;

namespace Allva.Desktop.Views.PanelDivisas;

public partial class NuevoClienteView : UserControl
{
    public NuevoClienteView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Conectar eventos de botones de imagen
        var btnFrontal = this.FindControl<Button>("BtnSeleccionarFrontal");
        var btnTrasera = this.FindControl<Button>("BtnSeleccionarTrasera");
        
        if (btnFrontal != null)
            btnFrontal.Click += OnSeleccionarImagenFrontalClick;
        if (btnTrasera != null)
            btnTrasera.Click += OnSeleccionarImagenTraseraClick;
    }
    
    private CurrencyExchangePanelViewModel? ViewModel => DataContext as CurrencyExchangePanelViewModel;
    
    private async void OnSeleccionarImagenFrontalClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar imagen frontal del documento",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Imágenes") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" } }
            }
        });
        
        if (files.Count > 0)
        {
            await using var stream = await files[0].OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            ViewModel?.SetImagenDocumentoFrontal(memoryStream.ToArray());
        }
    }
    
    private async void OnSeleccionarImagenTraseraClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar imagen trasera del documento",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Imágenes") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" } }
            }
        });
        
        if (files.Count > 0)
        {
            await using var stream = await files[0].OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            ViewModel?.SetImagenDocumentoTrasera(memoryStream.ToArray());
        }
    }
}