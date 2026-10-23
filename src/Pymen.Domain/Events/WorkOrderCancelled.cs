using Pymen.Domain.Common;

namespace Pymen.Domain.Events;

public sealed record WorkOrderCancelled(
    Guid WorkOrderId,
    Guid TenantId,
    string WorkOrderNumber,
    DateTimeOffset OccurredAt) : IDomainEvent;
