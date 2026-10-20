using Pymen.Domain.Common;
using Pymen.Domain.Tenancy;

namespace Pymen.Domain.Production;

public sealed class WorkOrder : Entity, ITenantOwned
{
    private readonly List<WorkOrderLine> _lines = [];
    private readonly List<WorkOrderOperation> _operations = [];

    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public WorkOrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? InvoicedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<WorkOrderLine> Lines => _lines.AsReadOnly();
    public IReadOnlyCollection<WorkOrderOperation> Operations => _operations.AsReadOnly();

    private WorkOrder()
    {
    }

    public static WorkOrder Create(Tenant tenant, Guid customerId, string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        if (customerId == Guid.Empty)
        {
            throw new DomainException("CustomerId is required.");
        }

        var number = tenant.AllocateWorkOrderNumber();

        return new WorkOrder
        {
            TenantId = tenant.Id,
            CustomerId = customerId,
            Number = number,
            Status = WorkOrderStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }

    public WorkOrderLine AddLine(Guid productId, decimal quantity)
    {
        EnsureEditable();

        var line = WorkOrderLine.Create(
            TenantId,
            Id,
            productId,
            quantity,
            _lines.Count + 1);

        _lines.Add(line);
        return line;
    }

    public WorkOrderOperation AddOperation(Guid operationTypeId)
    {
        EnsureEditable();

        var operation = WorkOrderOperation.Create(TenantId, Id, operationTypeId);
        _operations.Add(operation);
        return operation;
    }

    public void StartProduction(DateTimeOffset? at = null)
    {
        EnsureTransition(WorkOrderStatus.Draft, WorkOrderStatus.InProduction);
        if (_lines.Count == 0)
        {
            throw new DomainException("Cannot start production without lines.");
        }

        Status = WorkOrderStatus.InProduction;
        StartedAt = at ?? DateTimeOffset.UtcNow;
    }

    public void MarkDelivered(DateTimeOffset? at = null)
    {
        EnsureTransition(WorkOrderStatus.InProduction, WorkOrderStatus.Delivered);
        Status = WorkOrderStatus.Delivered;
        DeliveredAt = at ?? DateTimeOffset.UtcNow;
    }

    public void MarkInvoiced(DateTimeOffset? at = null)
    {
        EnsureTransition(WorkOrderStatus.Delivered, WorkOrderStatus.Invoiced);
        Status = WorkOrderStatus.Invoiced;
        InvoicedAt = at ?? DateTimeOffset.UtcNow;
    }

    public void Cancel(DateTimeOffset? at = null)
    {
        if (Status is not (WorkOrderStatus.Draft or WorkOrderStatus.InProduction))
        {
            throw new DomainException($"Cannot cancel work order from status {Status}.");
        }

        Status = WorkOrderStatus.Cancelled;
        CancelledAt = at ?? DateTimeOffset.UtcNow;
    }

    private void EnsureEditable()
    {
        if (Status is not (WorkOrderStatus.Draft or WorkOrderStatus.InProduction))
        {
            throw new DomainException($"Cannot modify work order in status {Status}.");
        }
    }

    private void EnsureTransition(WorkOrderStatus from, WorkOrderStatus to)
    {
        if (Status != from)
        {
            throw new DomainException($"Cannot transition work order from {Status} to {to}.");
        }
    }
}
