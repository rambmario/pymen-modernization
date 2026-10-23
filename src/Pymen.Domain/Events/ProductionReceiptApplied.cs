using Pymen.Domain.Common;

namespace Pymen.Domain.Events;

public sealed record ProductionReceiptApplied(
    Guid ProductionReceiptId,
    Guid TenantId,
    Guid ProductId,
    Guid WarehouseId,
    decimal Quantity,
    Guid? WorkOrderId,
    DateTimeOffset OccurredAt) : IDomainEvent;
