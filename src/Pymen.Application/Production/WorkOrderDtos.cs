namespace Pymen.Application.Production;

public sealed record WorkOrderListQuery(
    string? Status = null,
    Guid? CustomerId = null,
    int Page = 1,
    int PageSize = 20);

public sealed record WorkOrderLineRequest(Guid ProductId, decimal Quantity);

public sealed record WorkOrderOperationRequest(Guid OperationTypeId);

public sealed record CreateWorkOrderRequest(
    Guid CustomerId,
    string? Notes,
    IReadOnlyList<WorkOrderLineRequest> Lines,
    IReadOnlyList<WorkOrderOperationRequest>? Operations);

public sealed record UpdateWorkOrderRequest(
    string? Notes,
    IReadOnlyList<WorkOrderLineRequest>? LinesToAdd,
    IReadOnlyList<WorkOrderOperationRequest>? OperationsToAdd);

public sealed record WorkOrderLineResponse(Guid Id, Guid ProductId, decimal Quantity, int LineNumber);

public sealed record WorkOrderOperationResponse(
    Guid Id,
    Guid OperationTypeId,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt);

public sealed record WorkOrderResponse(
    Guid Id,
    string Number,
    Guid CustomerId,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? InvoicedAt,
    DateTimeOffset? CancelledAt,
    IReadOnlyList<WorkOrderLineResponse> Lines,
    IReadOnlyList<WorkOrderOperationResponse> Operations);

public sealed record CreateDeliveryNoteRequest(
    Guid WorkOrderId,
    string Number,
    string? Notes,
    IReadOnlyList<WorkOrderLineRequest> Lines);

public sealed record DeliveryNoteLineResponse(Guid Id, Guid ProductId, decimal Quantity, int LineNumber);

public sealed record DeliveryNoteResponse(
    Guid Id,
    Guid WorkOrderId,
    string Number,
    DateTimeOffset DeliveredAt,
    string? Notes,
    IReadOnlyList<DeliveryNoteLineResponse> Lines);
