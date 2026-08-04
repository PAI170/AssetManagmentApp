using AssetManagmentApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AssetManagmentApp.Services;

public static class ProformaPdfService
{
    public static byte[] Generar(Proforma proforma)
    {
        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Rodcast Solutions").FontSize(18).Bold();
                    col.Item().Text("Proforma").FontSize(14).SemiBold();
                    col.Item().PaddingTop(5).LineHorizontal(1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(5);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Proforma No. {proforma.Numero}").Bold();
                            c.Item().Text($"Fecha de generación: {proforma.FechaGeneracion:dd/MM/yyyy}");
                            c.Item().Text($"Período: {proforma.PeriodoDesde:dd/MM/yyyy} - {proforma.PeriodoHasta:dd/MM/yyyy}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignRight().Text("Proyecto").Bold();
                            c.Item().AlignRight().Text(proforma.Proyecto.Nombre);
                            c.Item().AlignRight().Text(proforma.Proyecto.Direccion);
                            c.Item().AlignRight().Text($"Ingeniero a cargo: {proforma.Proyecto.IngenieroACargo}");
                        });
                    });

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Tipo de equipo").Bold();
                            header.Cell().AlignRight().Text("Cantidad").Bold();
                            header.Cell().AlignRight().Text("Días").Bold();
                            header.Cell().AlignRight().Text("Precio/día").Bold();
                            header.Cell().AlignRight().Text("Subtotal").Bold();
                            header.Cell().ColumnSpan(5).PaddingTop(3).LineHorizontal(1);
                        });

                        // La placa es un dato interno del activo; al cliente se le muestra
                        // el tipo de equipo agrupado con la cantidad de unidades facturadas.
                        var lineasAgrupadas = proforma.Detalles
                            .GroupBy(d => (d.TipoEquipoNombre, d.DiasCobrados, d.PrecioPorDiaUsado))
                            .Select(g => new
                            {
                                g.Key.TipoEquipoNombre,
                                Cantidad = g.Count(),
                                g.Key.DiasCobrados,
                                g.Key.PrecioPorDiaUsado,
                                Subtotal = g.Sum(d => d.Subtotal)
                            })
                            .OrderBy(l => l.TipoEquipoNombre);

                        foreach (var linea in lineasAgrupadas)
                        {
                            table.Cell().Text(linea.TipoEquipoNombre);
                            table.Cell().AlignRight().Text(linea.Cantidad.ToString());
                            table.Cell().AlignRight().Text(linea.DiasCobrados.ToString());
                            table.Cell().AlignRight().Text($"₡{linea.PrecioPorDiaUsado:N2}");
                            table.Cell().AlignRight().Text($"₡{linea.Subtotal:N2}");
                        }
                    });

                    col.Item().PaddingTop(10).AlignRight().Text($"Total: ₡{proforma.Total:N2}").FontSize(13).Bold();
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
}
