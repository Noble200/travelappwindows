using System;
using System.IO;
using System.Threading.Tasks;
using Allva.Desktop.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Allva.Desktop.Services;

public class ReciboDivisasPdfService
{
    public async Task<string> GenerarReciboPdfAsync(
        string numeroOperacion,
        DateTime fechaOperacion,
        ClienteModel cliente,
        string divisaOrigen,
        string nombreDivisa,
        decimal cantidadRecibida,
        decimal totalEntregado,
        decimal tasaCambio,
        int idLocal)
    {
        return await Task.Run(() =>
        {
            QuestPDF.Settings.License = LicenseType.Community;
            
            var tempDir = Path.Combine(Path.GetTempPath(), "AllvaRecibos");
            Directory.CreateDirectory(tempDir);
            
            var fileName = $"Recibo_{numeroOperacion}_{fechaOperacion:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(tempDir, fileName);
            
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));
                    
                    // Encabezado
                    page.Header().Element(header =>
                    {
                        header.Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Allva")
                                    .FontSize(28)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken3);
                                
                                col.Item().Text("Compra de Divisas")
                                    .FontSize(14)
                                    .FontColor(Colors.Grey.Darken1);
                            });
                            
                            row.ConstantItem(200).AlignRight().Column(col =>
                            {
                                col.Item().Text("RECIBO DE OPERACIÓN")
                                    .FontSize(14)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken3);
                                
                                col.Item().Text(numeroOperacion)
                                    .FontSize(16)
                                    .Bold();
                                
                                col.Item().Text($"Fecha: {fechaOperacion:dd/MM/yyyy HH:mm}")
                                    .FontSize(10)
                                    .FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });
                    
                    // Contenido
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().PaddingBottom(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        
                        col.Item().Text("DATOS DEL CLIENTE")
                            .FontSize(12)
                            .Bold()
                            .FontColor(Colors.Blue.Darken3);
                        
                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(120);
                                columns.RelativeColumn();
                            });
                            
                            table.Cell().Text("Nombre:").Bold();
                            table.Cell().Text(cliente.NombreCompleto);
                            
                            table.Cell().Text("Documento:").Bold();
                            table.Cell().Text(cliente.DocumentoCompleto);
                            
                            table.Cell().Text("Teléfono:").Bold();
                            table.Cell().Text(cliente.Telefono ?? "-");
                            
                            table.Cell().Text("Dirección:").Bold();
                            table.Cell().Text(cliente.Direccion ?? "-");
                            
                            table.Cell().Text("Nacionalidad:").Bold();
                            table.Cell().Text(cliente.Nacionalidad ?? "-");
                        });
                        
                        col.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        
                        col.Item().Text("DETALLE DE LA OPERACIÓN")
                            .FontSize(12)
                            .Bold()
                            .FontColor(Colors.Blue.Darken3);
                        
                        col.Item().PaddingTop(10).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(15).Column(opCol =>
                        {
                            opCol.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Divisa recibida:").Bold();
                                row.RelativeItem().AlignRight().Text($"{divisaOrigen} - {nombreDivisa}");
                            });
                            
                            opCol.Item().PaddingTop(8).Row(row =>
                            {
                                row.RelativeItem().Text("Cantidad recibida:").Bold();
                                row.RelativeItem().AlignRight().Text($"{cantidadRecibida:N2} {divisaOrigen}")
                                    .FontSize(14)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken3);
                            });
                            
                            opCol.Item().PaddingTop(8).Row(row =>
                            {
                                row.RelativeItem().Text("Tasa de cambio:").Bold();
                                row.RelativeItem().AlignRight().Text($"1 {divisaOrigen} = {tasaCambio:N4} EUR");
                            });
                            
                            opCol.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            
                            opCol.Item().PaddingTop(15).Row(row =>
                            {
                                row.RelativeItem().Text("TOTAL ENTREGADO:").Bold().FontSize(14);
                                row.RelativeItem().AlignRight().Text($"{totalEntregado:N2} EUR")
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor(Colors.Green.Darken2);
                            });
                        });
                        
                        col.Item().PaddingTop(30);
                        
                        col.Item().Background(Colors.Grey.Lighten4).Padding(15).Column(aviso =>
                        {
                            aviso.Item().Text("INFORMACIÓN IMPORTANTE")
                                .FontSize(10)
                                .Bold();
                            
                            aviso.Item().PaddingTop(5).Text(
                                "Este documento es un comprobante de la operación de cambio de divisas realizada. " +
                                "Conserve este recibo como justificante de la transacción. " +
                                "En caso de cualquier consulta, contacte con su oficina Allva.")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken2);
                        });
                    });
                    
                    // Pie
                    page.Footer().AlignCenter().Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(10).Text($"Allva - Sistema de Gestión Comercial | Local: {idLocal} | Documento generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
            }).GeneratePdf(filePath);
            
            return filePath;
        });
    }
}