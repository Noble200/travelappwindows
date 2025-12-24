using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using Allva.Desktop.Services;

namespace Allva.Desktop.ViewModels;

public partial class DetalleOperacionPackAlimentosViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    private readonly string _numeroOperacion;
    private readonly string _codigoLocal;
    private readonly string _nombreUsuario;
    private readonly int _numeroUsuario;
    private Window? _ventana;

    // Datos de la operación
    [ObservableProperty] private string numeroOperacion = "";
    [ObservableProperty] private string fechaOperacion = "";
    [ObservableProperty] private string horaOperacion = "";
    [ObservableProperty] private string usuarioOperacion = "";

    // Datos del cliente
    [ObservableProperty] private string clienteNombre = "";
    [ObservableProperty] private string clienteDocumento = "";
    [ObservableProperty] private string clienteTelefono = "";

    // Datos del beneficiario
    [ObservableProperty] private string beneficiarioNombre = "";
    [ObservableProperty] private string beneficiarioDocumento = "";
    [ObservableProperty] private string beneficiarioDireccion = "";
    [ObservableProperty] private string beneficiarioTelefono = "";
    [ObservableProperty] private string beneficiarioPais = "";
    [ObservableProperty] private string beneficiarioCiudad = "";

    // Datos del pack
    [ObservableProperty] private string packNombre = "";
    [ObservableProperty] private string packDescripcion = "";
    [ObservableProperty] private string packProductos = "";

    // Totales
    [ObservableProperty] private string importeTotal = "";
    [ObservableProperty] private string moneda = "EUR";
    [ObservableProperty] private string estadoEnvio = "";
    [ObservableProperty] private string estadoColor = "#6c757d";

    // Estado
    [ObservableProperty] private bool estaCargando = false;
    [ObservableProperty] private string mensaje = "";
    [ObservableProperty] private bool esMensajeError = false;

    public string MensajeColor => EsMensajeError ? "#dc3545" : "#28a745";

    partial void OnEsMensajeErrorChanged(bool value)
    {
        OnPropertyChanged(nameof(MensajeColor));
    }

    // Datos internos para PDF
    private ReciboFoodPackService.DatosReciboFoodPack? _datosRecibo;

    public DetalleOperacionPackAlimentosViewModel(string numeroOperacion, string codigoLocal, string nombreUsuario, int numeroUsuario)
    {
        _numeroOperacion = numeroOperacion;
        _codigoLocal = codigoLocal;
        _nombreUsuario = nombreUsuario;
        _numeroUsuario = numeroUsuario;
        NumeroOperacion = numeroOperacion;
    }

    public void SetVentana(Window ventana)
    {
        _ventana = ventana;
    }

    public async Task CargarDatosAsync()
    {
        try
        {
            EstaCargando = true;
            Mensaje = "";

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var query = @"
                SELECT 
                    o.id_operacion,
                    o.fecha_operacion,
                    o.hora_operacion,
                    o.importe_total,
                    o.moneda,
                    u.nombre as usuario_nombre,
                    u.apellidos as usuario_apellido,
                    c.nombre as cliente_nombre,
                    c.apellidos as cliente_apellido,
                    c.segundo_nombre as cliente_segundo_nombre,
                    c.segundo_apellido as cliente_segundo_apellido,
                    c.documento_tipo as cliente_doc_tipo,
                    c.documento_numero as cliente_doc_numero,
                    c.telefono as cliente_telefono,
                    cb.nombre as benef_nombre,
                    cb.apellido as benef_apellido,
                    cb.tipo_documento as benef_doc_tipo,
                    cb.numero_documento as benef_doc_numero,
                    cb.telefono as benef_telefono,
                    CONCAT_WS(', ', cb.calle, cb.numero, cb.piso, cb.ciudad) as benef_direccion,
                    cb.ciudad as benef_ciudad,
                    cb.pais as benef_pais,
                    opa.nombre_pack,
                    opa.estado_envio,
                    opa.pais_destino,
                    opa.ciudad_destino
                FROM operaciones o
                LEFT JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
                LEFT JOIN usuarios u ON o.id_usuario = u.id_usuario
                LEFT JOIN clientes c ON o.id_cliente = c.id_cliente
                LEFT JOIN clientes_beneficiarios cb ON opa.id_beneficiario = cb.id_beneficiario
                WHERE o.numero_operacion = @numeroOperacion
                  AND o.modulo = 'PACK_ALIMENTOS'";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@numeroOperacion", _numeroOperacion);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var fecha = reader.IsDBNull(1) ? DateTime.Today : reader.GetDateTime(1);
                var hora = reader.IsDBNull(2) ? TimeSpan.Zero : reader.GetTimeSpan(2);
                var importe = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3);
                var monedaVal = reader.IsDBNull(4) ? "EUR" : reader.GetString(4);

                // Usuario
                var usuarioNom = reader.IsDBNull(5) ? "" : reader.GetString(5);
                var usuarioApe = reader.IsDBNull(6) ? "" : reader.GetString(6);
                UsuarioOperacion = $"{usuarioNom} {usuarioApe}".Trim();

                // Cliente
                var clienteNom = reader.IsDBNull(7) ? "" : reader.GetString(7);
                var clienteApe = reader.IsDBNull(8) ? "" : reader.GetString(8);
                var clienteNom2 = reader.IsDBNull(9) ? "" : reader.GetString(9);
                var clienteApe2 = reader.IsDBNull(10) ? "" : reader.GetString(10);
                var clienteDocTipo = reader.IsDBNull(11) ? "" : reader.GetString(11);
                var clienteDocNum = reader.IsDBNull(12) ? "" : reader.GetString(12);
                var clienteTel = reader.IsDBNull(13) ? "" : reader.GetString(13);

                ClienteNombre = $"{clienteNom} {clienteNom2} {clienteApe} {clienteApe2}".Trim();
                ClienteDocumento = $"{clienteDocTipo}: {clienteDocNum}";
                ClienteTelefono = string.IsNullOrEmpty(clienteTel) ? "N/A" : clienteTel;

                // Beneficiario
                var benefNom = reader.IsDBNull(14) ? "" : reader.GetString(14);
                var benefApe = reader.IsDBNull(15) ? "" : reader.GetString(15);
                var benefDocTipo = reader.IsDBNull(16) ? "" : reader.GetString(16);
                var benefDocNum = reader.IsDBNull(17) ? "" : reader.GetString(17);
                var benefTel = reader.IsDBNull(18) ? "" : reader.GetString(18);
                var benefDir = reader.IsDBNull(19) ? "" : reader.GetString(19);
                var benefCiudad = reader.IsDBNull(20) ? "" : reader.GetString(20);
                var benefPais = reader.IsDBNull(21) ? "" : reader.GetString(21);

                BeneficiarioNombre = $"{benefNom} {benefApe}".Trim();
                BeneficiarioDocumento = $"{benefDocTipo}: {benefDocNum}";
                BeneficiarioTelefono = string.IsNullOrEmpty(benefTel) ? "N/A" : benefTel;
                BeneficiarioDireccion = benefDir;
                BeneficiarioPais = benefPais;
                BeneficiarioCiudad = benefCiudad;

                // Pack
                var packNom = reader.IsDBNull(22) ? "Pack Alimentos" : reader.GetString(22);
                var estado = reader.IsDBNull(23) ? "PENDIENTE" : reader.GetString(23);
                var paisDest = reader.IsDBNull(24) ? benefPais : reader.GetString(24);
                var ciudadDest = reader.IsDBNull(25) ? benefCiudad : reader.GetString(25);

                PackNombre = packNom;
                PackDescripcion = "";
                EstadoEnvio = ObtenerTextoEstado(estado);
                EstadoColor = ObtenerColorEstado(estado);

                FechaOperacion = fecha.ToString("dd/MM/yyyy");
                HoraOperacion = hora.ToString(@"hh\:mm");
                ImporteTotal = $"{importe:N2}";
                Moneda = monedaVal;

                // Preparar datos para PDF
                _datosRecibo = new ReciboFoodPackService.DatosReciboFoodPack
                {
                    NumeroOperacion = _numeroOperacion,
                    FechaOperacion = fecha.Add(hora),
                    CodigoLocal = _codigoLocal,
                    NombreUsuario = _nombreUsuario,
                    NumeroUsuario = _numeroUsuario.ToString(),
                    ClienteNombre = ClienteNombre,
                    ClienteTipoDocumento = clienteDocTipo,
                    ClienteNumeroDocumento = clienteDocNum,
                    ClienteTelefono = ClienteTelefono,
                    ClienteDireccion = "",
                    ClienteNacionalidad = "",
                    BeneficiarioNombre = BeneficiarioNombre,
                    BeneficiarioTipoDocumento = benefDocTipo,
                    BeneficiarioNumeroDocumento = benefDocNum,
                    BeneficiarioDireccion = BeneficiarioDireccion,
                    BeneficiarioTelefono = BeneficiarioTelefono,
                    BeneficiarioPaisDestino = paisDest,
                    BeneficiarioCiudadDestino = ciudadDest,
                    PackNombre = PackNombre,
                    PackDescripcion = PackDescripcion,
                    PackProductos = Array.Empty<string>(),
                    PrecioPack = importe,
                    Total = importe,
                    Moneda = monedaVal,
                    MetodoPago = "EFECTIVO"
                };

                // Cargar productos del pack
                await CargarProductosPackAsync(conn);
            }
            else
            {
                Mensaje = "No se encontró la operación";
                EsMensajeError = true;
            }
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al cargar datos: {ex.Message}";
            EsMensajeError = true;
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private async Task CargarProductosPackAsync(NpgsqlConnection conn)
    {
        try
        {
            var query = @"
                SELECT pa.nombre_producto, pa.cantidad, pa.unidad
                FROM operaciones o
                INNER JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
                INNER JOIN packs_alimentos p ON opa.id_pack = p.id_pack
                INNER JOIN pack_productos pp ON p.id_pack = pp.id_pack
                INNER JOIN productos_alimentos pa ON pp.id_producto = pa.id_producto
                WHERE o.numero_operacion = @numeroOperacion";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@numeroOperacion", _numeroOperacion);

            await using var reader = await cmd.ExecuteReaderAsync();

            var productos = new System.Collections.Generic.List<string>();
            while (await reader.ReadAsync())
            {
                var nombre = reader.IsDBNull(0) ? "" : reader.GetString(0);
                var cantidad = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                var unidad = reader.IsDBNull(2) ? "" : reader.GetString(2);
                productos.Add($"{nombre} ({cantidad} {unidad})");
            }

            PackProductos = string.Join("\n", productos);
            if (_datosRecibo != null)
            {
                _datosRecibo.PackProductos = productos.ToArray();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar productos: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ReimprimirAsync()
    {
        if (_datosRecibo == null)
        {
            Mensaje = "No hay datos para reimprimir";
            EsMensajeError = true;
            return;
        }

        if (_ventana == null)
        {
            Mensaje = "No se puede abrir el dialogo de guardar";
            EsMensajeError = true;
            return;
        }

        try
        {
            EstaCargando = true;
            Mensaje = "";

            var nombreSugerido = $"Recibo_FoodPack_{_numeroOperacion}.pdf";

            var archivo = await _ventana.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Guardar Recibo PDF",
                SuggestedFileName = nombreSugerido,
                DefaultExtension = "pdf",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Documento PDF") { Patterns = new[] { "*.pdf" } }
                }
            });

            if (archivo == null)
                return;

            // Generar PDF en hilo separado para no bloquear la UI
            var pdfService = new ReciboFoodPackService();
            var pdfBytes = await Task.Run(() => pdfService.GenerarReciboPdf(_datosRecibo));

            await using var stream = await archivo.OpenWriteAsync();
            await stream.WriteAsync(pdfBytes);

            Mensaje = "Recibo PDF generado correctamente";
            EsMensajeError = false;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al generar PDF: {ex.Message}";
            EsMensajeError = true;
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private void Cerrar()
    {
        _ventana?.Close();
    }

    private string ObtenerTextoEstado(string estado)
    {
        return estado.ToUpper() switch
        {
            "PENDIENTE" => "Pendiente pago",
            "ENVIADO" => "Enviado",
            "ANULADO" => "Anulado",
            _ => estado
        };
    }

    private string ObtenerColorEstado(string estado)
    {
        return estado.ToUpper() switch
        {
            "PENDIENTE" => "#ffc107",
            "ENVIADO" => "#28a745",
            "ANULADO" => "#dc3545",
            _ => "#6c757d"
        };
    }
}