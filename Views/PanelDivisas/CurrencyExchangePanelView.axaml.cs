using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Allva.Desktop.ViewModels;

namespace Allva.Desktop.Views.PanelDivisas;

public partial class CurrencyExchangePanelView : UserControl
{
    private CurrencyExchangePanelViewModel? _viewModel;
    
    public CurrencyExchangePanelView()
    {
        InitializeComponent();
        
        _viewModel = DataContext as CurrencyExchangePanelViewModel;
        
        Loaded += OnLoaded;
    }
    
    public CurrencyExchangePanelView(int idLocal, int idComercio)
    {
        InitializeComponent();
        
        _viewModel = new CurrencyExchangePanelViewModel(idLocal, idComercio);
        DataContext = _viewModel;
        
        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _viewModel = DataContext as CurrencyExchangePanelViewModel;
        
        var btnImagen = this.FindControl<Button>("BtnSeleccionarImagen");
        if (btnImagen != null)
        {
            btnImagen.Click += BtnSeleccionarImagen_Click;
        }
    }

    private async void BtnSeleccionarImagen_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar imagen del documento",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Imágenes")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" }
                    },
                    new FilePickerFileType("PDF")
                    {
                        Patterns = new[] { "*.pdf" }
                    }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                await using var stream = await file.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                
                var bytes = memoryStream.ToArray();
                var fileName = file.Name;
                
                _viewModel?.SetImagenDocumento(bytes, fileName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al seleccionar imagen: {ex.Message}");
        }
    }
}