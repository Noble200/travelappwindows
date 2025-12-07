using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Allva.Desktop.ViewModels;

public partial class ConfirmacionImpresionViewModel : ObservableObject
{
    [ObservableProperty]
    private string fechaGeneracion = "";
    
    [ObservableProperty]
    private string horaGeneracion = "";
    
    [ObservableProperty]
    private string nombreUsuario = "";
    
    [ObservableProperty]
    private string codigoLocal = "";
    
    [ObservableProperty]
    private string balanceEuros = "0.00";
    
    [ObservableProperty]
    private string totalDivisas = "0.00";
    
    [ObservableProperty]
    private string salidaEuros = "0.00";
    
    [ObservableProperty]
    private string entradaEuros = "0.00";
    
    [ObservableProperty]
    private string cantidadOperaciones = "0 registros";
    
    [ObservableProperty]
    private string desgloseDivisasTexto = "";
    
    [ObservableProperty]
    private bool sinFiltros = true;
    
    [ObservableProperty]
    private bool tieneDivisas = false;
    
    public ObservableCollection<FiltroAplicadoItem> FiltrosAplicados { get; } = new();
    
    public ConfirmacionImpresionViewModel()
    {
    }
}

public class FiltroAplicadoItem
{
    public string Nombre { get; set; } = "";
    public string Valor { get; set; } = "";
}