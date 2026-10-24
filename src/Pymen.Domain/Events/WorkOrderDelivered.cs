using Pymen.Domain.Common;

namespace Pymen.Domain.Events;

public sealed record WorkOrderDelivered(
    Guid WorkOrderId,
    Guid TenantId,
    string WorkOrderNumber,
    string DeliveryNoteNumber,
    DateTimeOffset OccurredAt) : IDomainEvent;
