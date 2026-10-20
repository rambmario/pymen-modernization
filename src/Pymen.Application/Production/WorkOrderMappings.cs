using Pymen.Domain.Production;

namespace Pymen.Application.Production;

public static class WorkOrderMappings
{
    public static WorkOrderResponse ToResponse(this WorkOrder workOrder) =>
        new(
            workOrder.Id,
            workOrder.Number,
            workOrder.CustomerId,
            workOrder.Status.ToString(),
            workOrder.Notes,
            workOrder.CreatedAt,
            workOrder.StartedAt,
            workOrder.DeliveredAt,
            workOrder.InvoicedAt,
            workOrder.CancelledAt,
            workOrder.Lines.Select(l => new WorkOrderLineResponse(l.Id, l.ProductId, l.Quantity, l.LineNumber)).ToList(),
            workOrder.Operations.Select(o => new WorkOrderOperationResponse(
                o.Id,
                o.OperationTypeId,
                o.Status.ToString(),
                o.StartedAt,
                o.FinishedAt)).ToList());

    public static DeliveryNoteResponse ToResponse(this DeliveryNote note) =>
        new(
            note.Id,
            note.WorkOrderId,
            note.Number,
            note.DeliveredAt,
            note.Notes,
            note.Lines.Select(l => new DeliveryNoteLineResponse(l.Id, l.ProductId, l.Quantity, l.LineNumber)).ToList());
}
