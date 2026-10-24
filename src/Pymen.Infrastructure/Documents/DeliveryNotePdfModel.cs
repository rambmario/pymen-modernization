namespace Pymen.Infrastructure.Documents;

public sealed record DeliveryNotePdfLine(int LineNumber, string ProductSku, string ProductName, decimal Quantity);

public sealed record DeliveryNotePdfModel(
    string TenantName,
    string DeliveryNoteNumber,
    string WorkOrderNumber,
    string CustomerName,
    DateTimeOffset DeliveredAt,
    IReadOnlyList<DeliveryNotePdfLine> Lines);
