using AssetManagmentApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AssetManagmentApp.Services;

public static class ProformaPdfService
{
    private const string NombreEmpresa = "ACA Asesores Técnicos para Centroamérica y el Caribe";
    private const string CedulaEmpresa = "3-101-157521";
    private const string TelefonoEmpresa = "+506 8703-5064";
    private const string CorreoEmpresa = "facturacion@acaasesores.com";
    private const string DireccionEmpresa = "San José, Santa Ana, Río Oro";
    private const string NombreContacto = "Rodolfo Bonilla";
    private const decimal PorcentajeIva = 0.13m;

    // El logo se coloca en wwwroot/images/logo-aca.png; si aún no existe (antes de que
    // lo suban), el encabezado simplemente se genera sin imagen en vez de fallar.
    private static readonly string RutaLogo = Path.Combine(AppContext.BaseDirectory, "wwwroot", "images", "logo-aca.jpg");

    public static byte[] Generar(Proforma proforma)
    {
        var subtotal = proforma.Total;
        var iva = Math.Round(subtotal * PorcentajeIva, 2);
        var total = subtotal + iva;
        var fechaVencimiento = proforma.FechaGeneracion.AddDays(30);

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginVertical(1.5f, Unit.Centimetre);
                page.MarginHorizontal(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(80).Height(60).Element(logoContainer =>
                        {
                            if (File.Exists(RutaLogo))
                            {
                                logoContainer.Image(RutaLogo).FitArea();
                            }
                        });

                        row.RelativeItem().PaddingLeft(10).Column(c =>
                        {
                            c.Item().Text(NombreEmpresa).FontSize(13).Bold();
                            c.Item().Text($"Cédula: {CedulaEmpresa}");
                            c.Item().Text($"Tel.: {TelefonoEmpresa}");
                            c.Item().Text($"E-mail: {CorreoEmpresa}");
                            c.Item().Text(DireccionEmpresa);
                        });

                        row.ConstantItem(150).Border(1).Padding(6).Column(c =>
                        {
                            c.Item().AlignCenter().Text("PROFORMA").Bold().FontSize(12);
                            c.Item().PaddingTop(4).Text($"Fecha: {proforma.FechaGeneracion:dd/MM/yyyy}");
                            c.Item().Text("Moneda: USD");
                            c.Item().Text(proforma.TipoCambio is { } tipoCambio
                                ? $"Tipo de cambio: ₡{tipoCambio:N2}"
                                : "Tipo de cambio: N/D");
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(6);

                    col.Item().Border(1).Padding(6).Row(row =>
                    {
                        row.RelativeItem().Text($"Proforma: {proforma.Numero}").Bold();
                        row.RelativeItem().Text($"Período: {proforma.PeriodoDesde:dd/MM/yyyy} - {proforma.PeriodoHasta:dd/MM/yyyy}");
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).Padding(6).Column(c =>
                        {
                            c.Item().Text("Proyecto:").Bold();
                            c.Item().Text(proforma.Proyecto.Nombre);
                            c.Item().PaddingTop(4).Text("Dirección:").Bold();
                            c.Item().Text(proforma.Proyecto.Direccion);
                        });

                        row.RelativeItem().Border(1).Padding(6).Column(c =>
                        {
                            c.Item().Text("Medio de pago:").Bold();
                            c.Item().Text("Transferencia - Depósito Bancario");
                            c.Item().PaddingTop(4).Text("Fecha de vencimiento:").Bold();
                            c.Item().Text(fechaVencimiento.ToString("dd/MM/yyyy"));
                        });
                    });

                    col.Item().Border(1).Padding(6).Row(row =>
                    {
                        row.RelativeItem().Text($"Contacto: {NombreContacto}");
                        row.RelativeItem().AlignRight().Text($"Teléfono: {TelefonoEmpresa}");
                    });

                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1f);    // Código
                            columns.RelativeColumn(5f);    // Descripción
                            columns.RelativeColumn(1.3f);  // Cantidad
                            columns.RelativeColumn(1.3f);  // Días
                            columns.RelativeColumn(1.6f);  // Subtotal
                        });

                        table.Header(header =>
                        {
                            header.Cell().PaddingRight(4).AlignCenter().Text("Código").Bold();
                            header.Cell().PaddingRight(4).Text("Descripción").Bold();
                            header.Cell().PaddingRight(4).AlignCenter().Text("Cantidad").Bold();
                            header.Cell().PaddingRight(4).AlignCenter().Text("Días").Bold();
                            header.Cell().AlignCenter().Text("Subtotal").Bold();
                            header.Cell().ColumnSpan(5).PaddingTop(3).LineHorizontal(1);
                        });

                        // La placa es un dato interno del activo; al cliente se le muestra
                        // el tipo de equipo agrupado con la cantidad de unidades facturadas.
                        var lineasAgrupadas = proforma.Detalles
                            .GroupBy(d => (d.TipoEquipoNombre, d.CodigoAlquiler, d.DiasCobrados, d.PrecioPorDiaUsado))
                            .Select(g => new
                            {
                                g.Key.TipoEquipoNombre,
                                g.Key.CodigoAlquiler,
                                Cantidad = g.Count(),
                                g.Key.DiasCobrados,
                                g.Key.PrecioPorDiaUsado,
                                Subtotal = g.Sum(d => d.Subtotal)
                            })
                            .OrderBy(l => l.TipoEquipoNombre);

                        foreach (var linea in lineasAgrupadas)
                        {
                            table.Cell().PaddingRight(4).PaddingTop(2).AlignCenter().Text(linea.CodigoAlquiler ?? "-");
                            table.Cell().PaddingRight(4).PaddingTop(2).Text(DescripcionLinea(linea.TipoEquipoNombre, proforma.Proyecto));
                            table.Cell().PaddingRight(4).PaddingTop(2).AlignCenter().Text(linea.Cantidad.ToString());
                            table.Cell().PaddingRight(4).PaddingTop(2).AlignCenter().Text(linea.DiasCobrados.ToString());
                            table.Cell().PaddingTop(2).AlignCenter().Text(linea.Subtotal.ToMoneda());
                        }
                    });

                    col.Item().PaddingTop(60).Row(row =>
                    {
                        row.RelativeItem(3).Column(c =>
                        {
                            c.Item().Text("Observaciones:").Bold();
                            c.Item().PaddingTop(2).Border(1).MinHeight(60);
                        });

                        row.ConstantItem(50);

                        row.RelativeItem(2).PaddingTop(25).Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Subtotal:");
                                r.RelativeItem().AlignRight().Text(subtotal.ToMoneda());
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("IVA (13%):");
                                r.RelativeItem().AlignRight().Text(iva.ToMoneda());
                            });
                            c.Item().PaddingTop(3).BorderTop(1).PaddingTop(3).Row(r =>
                            {
                                r.RelativeItem().Text("Total:").Bold().FontSize(12);
                                r.RelativeItem().AlignRight().Text(total.ToMoneda()).Bold().FontSize(12);
                            });
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Documento generado el ").FontSize(8);
                    x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                });
            });
        });

        return documento.GeneratePdf();
    }

    // "Batidoras (159, 2, 426, 5)": el código de alquiler va en su propia columna;
    // acá solo se agregan los códigos de requisa del proyecto (si están cargados)
    // entre paréntesis al final del nombre.
    private static string DescripcionLinea(string tipoEquipoNombre, Proyecto proyecto)
    {
        var sufijo = proyecto.CodigoProyecto is null
            ? ""
            : $" ({proyecto.CodigoProyecto}, {proyecto.Modelo}, {proyecto.Obra}, {proyecto.Actividad})";

        return $"{tipoEquipoNombre}{sufijo}";
    }
}
