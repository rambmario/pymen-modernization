using Pymen.Domain.Common;
using Pymen.Domain.Events;
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

    public static WorkOrder Create(Tenant tenant, Guid customerId, TimeProvider timeProvider, string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(timeProvider);

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
            CreatedAt = timeProvider.GetUtcNow(),
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

    public void StartProduction(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        EnsureTransition(WorkOrderStatus.Draft, WorkOrderStatus.InProduction);
        if (_lines.Count == 0)
        {
            throw new DomainException("Cannot start production without lines.");
        }

        Status = WorkOrderStatus.InProduction;
        StartedAt = timeProvider.GetUtcNow();
    }

    public void MarkDelivered(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        EnsureTransition(WorkOrderStatus.InProduction, WorkOrderStatus.Delivered);
        Status = WorkOrderStatus.Delivered;
        DeliveredAt = timeProvider.GetUtcNow();
        var deliveryNoteNumber = $"RN-{Number}";
        Raise(new WorkOrderDelivered(Id, TenantId, Number, deliveryNoteNumber, DeliveredAt.Value));
    }

    public void MarkInvoiced(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        EnsureTransition(WorkOrderStatus.Delivered, WorkOrderStatus.Invoiced);
        Status = WorkOrderStatus.Invoiced;
        InvoicedAt = timeProvider.GetUtcNow();
    }

    public void Cancel(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (Status is not (WorkOrderStatus.Draft or WorkOrderStatus.InProduction))
        {
            throw new DomainException($"Cannot cancel work order from status {Status}.");
        }

        foreach (var operation in _operations)
        {
            if (operation.Status is WorkOrderOperationStatus.Pending or WorkOrderOperationStatus.Started)
            {
                operation.Cancel();
            }
        }

        Status = WorkOrderStatus.Cancelled;
        CancelledAt = timeProvider.GetUtcNow();
        Raise(new WorkOrderCancelled(Id, TenantId, Number, CancelledAt.Value));
    }

    public void UpdateNotes(string? notes)
    {
        EnsureEditable();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
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
