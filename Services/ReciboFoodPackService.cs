using System;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Allva.Desktop.Services
{
    /// <summary>
    /// Servicio para generar recibos PDF de Pack de Alimentos
    /// Requiere instalar: dotnet add package QuestPDF
    /// </summary>
    public class ReciboFoodPackService
    {
        // Colores corporativos Allva
        private static readonly string ColorPrimario = "#0b5394";
        private static readonly string ColorSecundario = "#ffd966";
        private static readonly string ColorTexto = "#333333";
        private static readonly string ColorTextoClaro = "#666666";
        private static readonly string ColorFondo = "#f8f9fa";
        private static readonly string ColorBorde = "#dee2e6";
        private static readonly string ColorExito = "#28a745";

        public class DatosReciboFoodPack
        {
            // Datos de operacion
            public string NumeroOperacion { get; set; } = string.Empty;
            public DateTime FechaOperacion { get; set; } = DateTime.Now;
            public string CodigoLocal { get; set; } = string.Empty;
            public string NombreLocal { get; set; } = string.Empty;
            public string NombreUsuario { get; set; } = string.Empty;
            public string NumeroUsuario { get; set; } = string.Empty;

            // Datos del cliente (comprador)
            public string ClienteNombre { get; set; } = string.Empty;
            public string ClienteTipoDocumento { get; set; } = string.Empty;
            public string ClienteNumeroDocumento { get; set; } = string.Empty;
            public string ClienteTelefono { get; set; } = string.Empty;
            public string ClienteDireccion { get; set; } = string.Empty;
            public string ClienteNacionalidad { get; set; } = string.Empty;

            // Datos del beneficiario (quien recibe)
            public string BeneficiarioNombre { get; set; } = string.Empty;
            public string BeneficiarioTipoDocumento { get; set; } = string.Empty;
            public string BeneficiarioNumeroDocumento { get; set; } = string.Empty;
            public string BeneficiarioDireccion { get; set; } = string.Empty;
            public string BeneficiarioTelefono { get; set; } = string.Empty;
            public string BeneficiarioPaisDestino { get; set; } = string.Empty;
            public string BeneficiarioCiudadDestino { get; set; } = string.Empty;

            // Datos del pack
            public string PackNombre { get; set; } = string.Empty;
            public string PackDescripcion { get; set; } = string.Empty;
            public string[] PackProductos { get; set; } = Array.Empty<string>();
            public string PackImagenBase64 { get; set; } = string.Empty;

            // Totales
            public decimal PrecioPack { get; set; }
            public decimal Total { get; set; }
            public string Moneda { get; set; } = "USD";
            public string MetodoPago { get; set; } = "EFECTIVO";

            // Datos de la empresa
            public string EmpresaNombre { get; set; } = "ALLVA SYSTEM";
            public string EmpresaDireccion { get; set; } = string.Empty;
            public string EmpresaTelefono { get; set; } = string.Empty;
            public string EmpresaRUC { get; set; } = string.Empty;
        }

        static ReciboFoodPackService()
        {
            // Configurar licencia de QuestPDF (Community es gratuita para empresas pequenas)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerarReciboPdf(DatosReciboFoodPack datos)
        {
            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(ColorTexto));

                    page.Header().Element(c => CrearEncabezado(c, datos));
                    page.Content().Element(c => CrearContenido(c, datos));
                    page.Footer().Element(c => CrearPiePagina(c, datos));
                });
            });

            return documento.GeneratePdf();
        }

        public void GenerarReciboPdf(DatosReciboFoodPack datos, string rutaArchivo)
        {
            var pdfBytes = GenerarReciboPdf(datos);
            File.WriteAllBytes(rutaArchivo, pdfBytes);
        }

        public Stream GenerarReciboPdfStream(DatosReciboFoodPack datos)
        {
            var pdfBytes = GenerarReciboPdf(datos);
            return new MemoryStream(pdfBytes);
        }

        private void CrearEncabezado(IContainer container, DatosReciboFoodPack datos)
        {
            container.Column(column =>
            {
                // Barra superior decorativa
                column.Item().Row(row =>
                {
                    row.RelativeItem(3).Height(6).Background(ColorPrimario);
                    row.RelativeItem(1).Height(6).Background(ColorSecundario);
                });

                // Encabezado principal
                column.Item().PaddingVertical(12).Row(row =>
                {
                    // Logo y nombre de empresa
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(datos.EmpresaNombre)
                            .FontSize(26)
                            .Bold()
                            .FontColor(ColorPrimario);

                        col.Item().Text("Sistema de Gestion Empresarial")
                            .FontSize(9)
                            .FontColor(ColorTextoClaro);

                        if (!string.IsNullOrEmpty(datos.EmpresaDireccion))
                        {
                            col.Item().PaddingTop(3).Text(datos.EmpresaDireccion)
                                .FontSize(8)
                                .FontColor(ColorTextoClaro);
                        }

                        if (!string.IsNullOrEmpty(datos.EmpresaTelefono))
                        {
                            col.Item().Text($"Tel: {datos.EmpresaTelefono}")
                                .FontSize(8)
                                .FontColor(ColorTextoClaro);
                        }
                    });

                    // Tipo de documento
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Border(2).BorderColor(ColorPrimario).Background(ColorSecundario).Padding(12).Column(innerCol =>
                        {
                            innerCol.Item().AlignCenter().Text("RECIBO")
                                .FontSize(18)
                                .Bold()
                                .FontColor(ColorPrimario);

                            innerCol.Item().AlignCenter().Text("PACK DE ALIMENTOS")
                                .FontSize(11)
                                .Bold()
                                .FontColor(ColorPrimario);

                            innerCol.Item().PaddingTop(5).AlignCenter().Text(datos.NumeroOperacion)
                                .FontSize(12)
                                .Bold()
                                .FontColor(ColorTexto);
                        });
                    });
                });

                // Linea divisoria
                column.Item().Height(2).Background(ColorPrimario);

                // Informacion de operacion
                column.Item().PaddingVertical(8).Background(ColorFondo).Padding(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.Span("Local: ").SemiBold();
                            text.Span(datos.CodigoLocal);
                            if (!string.IsNullOrEmpty(datos.NombreLocal))
                                text.Span($" - {datos.NombreLocal}");
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Usuario: ").SemiBold();
                            text.Span($"{datos.NombreUsuario} ({datos.NumeroUsuario})");
                        });
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.Span("Fecha: ").SemiBold();
                            text.Span(datos.FechaOperacion.ToString("dd/MM/yyyy"));
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Hora: ").SemiBold();
                            text.Span(datos.FechaOperacion.ToString("HH:mm:ss"));
                        });
                    });
                });
            });
        }

        private void CrearContenido(IContainer container, DatosReciboFoodPack datos)
        {
            container.PaddingVertical(8).Column(column =>
            {
                // SECCION: DATOS DEL CLIENTE (COMPRADOR)
                column.Item().Element(c => CrearSeccion(c, "DATOS DEL CLIENTE (COMPRADOR)", col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        // Fila 1
                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Nombre Completo: ").SemiBold();
                            text.Span(datos.ClienteNombre);
                        });

                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Nacionalidad: ").SemiBold();
                            text.Span(datos.ClienteNacionalidad);
                        });

                        // Fila 2
                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Documento: ").SemiBold();
                            text.Span($"{datos.ClienteTipoDocumento} {datos.ClienteNumeroDocumento}");
                        });

                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Telefono: ").SemiBold();
                            text.Span(datos.ClienteTelefono);
                        });

                        // Fila 3 (direccion completa)
                        if (!string.IsNullOrEmpty(datos.ClienteDireccion))
                        {
                            table.Cell().ColumnSpan(2).Element(CeldaInfo).Text(text =>
                            {
                                text.Span("Direccion: ").SemiBold();
                                text.Span(datos.ClienteDireccion);
                            });
                        }
                    });
                }));

                column.Item().Height(8);

                // SECCION: DATOS DEL BENEFICIARIO (DESTINO)
                column.Item().Element(c => CrearSeccion(c, "DATOS DEL BENEFICIARIO (DESTINO)", col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        // Fila 1
                        table.Cell().ColumnSpan(2).Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Nombre Completo: ").SemiBold();
                            text.Span(datos.BeneficiarioNombre);
                        });

                        // Fila 2
                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Documento: ").SemiBold();
                            text.Span($"{datos.BeneficiarioTipoDocumento} {datos.BeneficiarioNumeroDocumento}");
                        });

                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Telefono: ").SemiBold();
                            text.Span(string.IsNullOrEmpty(datos.BeneficiarioTelefono) ? "N/A" : datos.BeneficiarioTelefono);
                        });

                        // Fila 3
                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Pais Destino: ").SemiBold();
                            text.Span(datos.BeneficiarioPaisDestino);
                        });

                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Ciudad: ").SemiBold();
                            text.Span(string.IsNullOrEmpty(datos.BeneficiarioCiudadDestino) ? "N/A" : datos.BeneficiarioCiudadDestino);
                        });

                        // Fila 4 (direccion completa)
                        table.Cell().ColumnSpan(2).Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Direccion de Entrega: ").SemiBold();
                            text.Span(datos.BeneficiarioDireccion);
                        });
                    });
                }));

                column.Item().Height(8);

                // SECCION: DETALLE DEL PACK
                column.Item().Element(c => CrearSeccion(c, "DETALLE DEL PACK DE ALIMENTOS", col =>
                {
                    // Encabezado del pack
                    col.Item().Border(1).BorderColor(ColorBorde).Background(ColorSecundario).Padding(12).Row(row =>
                    {
                        row.RelativeItem().Column(packCol =>
                        {
                            packCol.Item().Text(datos.PackNombre)
                                .FontSize(16)
                                .Bold()
                                .FontColor(ColorPrimario);

                            if (!string.IsNullOrEmpty(datos.PackDescripcion))
                            {
                                packCol.Item().PaddingTop(4).Text(datos.PackDescripcion)
                                    .FontSize(9)
                                    .Italic()
                                    .FontColor(ColorTextoClaro);
                            }
                        });

                        row.AutoItem().AlignMiddle().Text($"{datos.Moneda} {datos.PrecioPack:N2}")
                            .FontSize(18)
                            .Bold()
                            .FontColor(ColorPrimario);
                    });

                    // Lista de productos
                    if (datos.PackProductos.Length > 0)
                    {
                        col.Item().PaddingTop(10).Text("Productos incluidos en el pack:")
                            .SemiBold()
                            .FontSize(10);

                        col.Item().PaddingTop(5).Border(1).BorderColor(ColorBorde).Padding(10).Column(prodCol =>
                        {
                            for (int i = 0; i < datos.PackProductos.Length; i++)
                            {
                                var usarFondoAlterno = i % 2 == 1;
                                
                                if (usarFondoAlterno)
                                {
                                    prodCol.Item().Background(ColorFondo).Padding(4).Row(prodRow =>
                                    {
                                        prodRow.AutoItem().Width(20).Text($"{i + 1}.")
                                            .FontColor(ColorPrimario)
                                            .SemiBold();
                                        prodRow.RelativeItem().Text(datos.PackProductos[i]);
                                    });
                                }
                                else
                                {
                                    prodCol.Item().Padding(4).Row(prodRow =>
                                    {
                                        prodRow.AutoItem().Width(20).Text($"{i + 1}.")
                                            .FontColor(ColorPrimario)
                                            .SemiBold();
                                        prodRow.RelativeItem().Text(datos.PackProductos[i]);
                                    });
                                }
                            }
                        });
                    }
                }));

                column.Item().Height(12);

                // SECCION: RESUMEN DE PAGO
                column.Item().Border(2).BorderColor(ColorPrimario).Column(totalCol =>
                {
                    // Encabezado
                    totalCol.Item().Background(ColorPrimario).Padding(10).Text("RESUMEN DE PAGO")
                        .FontSize(12)
                        .Bold()
                        .FontColor(Colors.White);

                    // Detalles
                    totalCol.Item().Padding(12).Column(detalleCol =>
                    {
                        detalleCol.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Precio del Pack:");
                            row.AutoItem().Text($"{datos.Moneda} {datos.PrecioPack:N2}");
                        });

                        detalleCol.Item().PaddingVertical(5).LineHorizontal(1).LineColor(ColorBorde);

                        detalleCol.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Metodo de Pago:").SemiBold();
                            row.AutoItem().Text(datos.MetodoPago);
                        });
                    });

                    // Total
                    totalCol.Item().Background(ColorSecundario).Padding(12).Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL PAGADO")
                            .FontSize(14)
                            .Bold()
                            .FontColor(ColorPrimario);

                        row.AutoItem().Text($"{datos.Moneda} {datos.Total:N2}")
                            .FontSize(18)
                            .Bold()
                            .FontColor(ColorPrimario);
                    });
                });

                column.Item().Height(12);

                // INFORMACION IMPORTANTE
                column.Item().Background(ColorFondo).Border(1).BorderColor(ColorBorde).Padding(10).Column(infoCol =>
                {
                    infoCol.Item().Text("INFORMACION IMPORTANTE")
                        .SemiBold()
                        .FontSize(10)
                        .FontColor(ColorPrimario);

                    infoCol.Item().PaddingTop(5).Text(
                        "1. Este recibo es comprobante de su compra. Conservelo para cualquier reclamo.\n" +
                        "2. El tiempo estimado de entrega depende del destino seleccionado.\n" +
                        "3. El beneficiario debe presentar documento de identidad al recibir el pack.\n" +
                        "4. Para consultas sobre el estado de su envio, comuniquese con nuestras oficinas.\n" +
                        "5. Los productos incluidos pueden variar segun disponibilidad en destino.")
                        .FontSize(8)
                        .FontColor(ColorTextoClaro)
                        .LineHeight(1.4f);
                });

                // Estado del envio
                column.Item().PaddingTop(10).AlignCenter().Row(row =>
                {
                    row.AutoItem().Border(1).BorderColor(ColorExito).Background(Colors.White).Padding(8).Row(statusRow =>
                    {
                        statusRow.AutoItem().Text("ESTADO: ")
                            .FontSize(10)
                            .SemiBold();
                        statusRow.AutoItem().Text("PENDIENTE DE ENVIO")
                            .FontSize(10)
                            .Bold()
                            .FontColor(ColorExito);
                    });
                });
            });
        }

        private void CrearSeccion(IContainer container, string titulo, Action<ColumnDescriptor> contenido)
        {
            container.Column(column =>
            {
                // Titulo de seccion
                column.Item().Row(row =>
                {
                    row.AutoItem().Width(4).Height(16).Background(ColorPrimario);
                    row.RelativeItem().PaddingLeft(8).AlignMiddle().Text(titulo)
                        .FontSize(11)
                        .Bold()
                        .FontColor(ColorPrimario);
                });

                // Contenido
                column.Item().PaddingTop(6).PaddingLeft(12).Element(c => c.Column(contenido));
            });
        }

        private static IContainer CeldaInfo(IContainer container)
        {
            return container.PaddingVertical(3);
        }

        private void CrearPiePagina(IContainer container, DatosReciboFoodPack datos)
        {
            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(ColorBorde);

                column.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Documento generado electronicamente")
                            .FontSize(8)
                            .FontColor(ColorTextoClaro);

                        col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                            .FontSize(8)
                            .FontColor(ColorTextoClaro);
                    });

                    row.RelativeItem().AlignCenter().Column(col =>
                    {
                        col.Item().Text("Gracias por su preferencia")
                            .FontSize(9)
                            .SemiBold()
                            .FontColor(ColorPrimario);

                        col.Item().Text("ALLVA SYSTEM")
                            .FontSize(8)
                            .FontColor(ColorTextoClaro);
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(8).FontColor(ColorTextoClaro));
                            text.Span("Pagina ");
                            text.CurrentPageNumber();
                            text.Span(" de ");
                            text.TotalPages();
                        });
                    });
                });

                // Barra inferior decorativa
                column.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem(3).Height(4).Background(ColorPrimario);
                    row.RelativeItem(1).Height(4).Background(ColorSecundario);
                });
            });
        }
    }
}