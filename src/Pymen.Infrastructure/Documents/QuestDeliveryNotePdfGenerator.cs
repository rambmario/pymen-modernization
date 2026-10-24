using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Pymen.Infrastructure.Documents;

public sealed class QuestDeliveryNotePdfGenerator : IDeliveryNotePdfGenerator
{
    static QuestDeliveryNotePdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(DeliveryNotePdfModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text(model.TenantName).SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2);
                    col.Item().Text("Remito de entrega").FontSize(14).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"Numero de remito: {model.DeliveryNoteNumber}").SemiBold();
                    col.Item().Text($"Orden de trabajo: {model.WorkOrderNumber}");
                    col.Item().Text($"Cliente: {model.CustomerName}");
                    col.Item().Text($"Fecha de entrega: {model.DeliveredAt.UtcDateTime:yyyy-MM-dd HH:mm} UTC");

                    col.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(70);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellHeader).Text("#");
                            header.Cell().Element(CellHeader).Text("SKU");
                            header.Cell().Element(CellHeader).Text("Producto");
                            header.Cell().Element(CellHeader).AlignRight().Text("Cant.");
                        });

                        foreach (var line in model.Lines)
                        {
                            table.Cell().Element(CellBody).Text(line.LineNumber.ToString());
                            table.Cell().Element(CellBody).Text(line.ProductSku);
                            table.Cell().Element(CellBody).Text(line.ProductName);
                            table.Cell().Element(CellBody).AlignRight().Text(line.Quantity.ToString("0.##"));
                        }
                    });
                });

                page.Footer().AlignCenter().Text("Pymen — documento demo (sin valor fiscal)");
            });
        });

        return document
            .WithMetadata(new DocumentMetadata
            {
                Title = model.DeliveryNoteNumber,
                Author = model.TenantName,
                Subject = $"Remito {model.DeliveryNoteNumber}"
            })
            .GeneratePdf();
    }

    private static IContainer CellHeader(IContainer container) =>
        container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

    private static IContainer CellBody(IContainer container) =>
        container.PaddingVertical(3);
}
