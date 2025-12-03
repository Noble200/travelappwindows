using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
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
        
        // Conectar eventos para formateo de fechas
        var txtFechaNacimiento = this.FindControl<TextBox>("TxtFechaNacimiento");
        var txtFechaCaducidad = this.FindControl<TextBox>("TxtFechaCaducidad");
        
        if (txtFechaNacimiento != null)
            txtFechaNacimiento.AddHandler(TextInputEvent, FormatearFechaInput, RoutingStrategies.Tunnel);
        if (txtFechaCaducidad != null)
            txtFechaCaducidad.AddHandler(TextInputEvent, FormatearFechaInput, RoutingStrategies.Tunnel);
    }
    
    private CurrencyExchangePanelViewModel? ViewModel => DataContext as CurrencyExchangePanelViewModel;
    
    private void FormatearFechaInput(object? sender, TextInputEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        
        var textoActual = textBox.Text ?? "";
        var textoNuevo = e.Text ?? "";
        
        // Solo permitir numeros
        if (!string.IsNullOrEmpty(textoNuevo) && !char.IsDigit(textoNuevo[0]))
        {
            e.Handled = true;
            return;
        }
        
        // Agregar / automaticamente
        var posicion = textBox.CaretIndex;
        var longitudActual = textoActual.Replace("/", "").Length;
        
        if (longitudActual == 2 || longitudActual == 4)
        {
            if (posicion == textoActual.Length && !textoActual.EndsWith("/"))
            {
                textBox.Text = textoActual + "/";
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
    }
    
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
                new FilePickerFileType("Imagenes") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" } }
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
                new FilePickerFileType("Imagenes") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" } }
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