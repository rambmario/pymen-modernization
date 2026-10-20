using Pymen.Domain.Common;

namespace Pymen.Domain.Production;

public sealed class WorkOrderOperation : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public Guid OperationTypeId { get; private set; }
    public WorkOrderOperationStatus Status { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }

    private WorkOrderOperation()
    {
    }

    internal static WorkOrderOperation Create(Guid tenantId, Guid workOrderId, Guid operationTypeId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("TenantId is required.");
        }

        if (workOrderId == Guid.Empty || operationTypeId == Guid.Empty)
        {
            throw new DomainException("Work order and operation type ids are required.");
        }

        return new WorkOrderOperation
        {
            TenantId = tenantId,
            WorkOrderId = workOrderId,
            OperationTypeId = operationTypeId,
            Status = WorkOrderOperationStatus.Pending
        };
    }

    public void Start(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        EnsureTransition(WorkOrderOperationStatus.Pending, WorkOrderOperationStatus.Started);
        Status = WorkOrderOperationStatus.Started;
        StartedAt = timeProvider.GetUtcNow();
    }

    public void Finish(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        EnsureTransition(WorkOrderOperationStatus.Started, WorkOrderOperationStatus.Finished);
        Status = WorkOrderOperationStatus.Finished;
        FinishedAt = timeProvider.GetUtcNow();
    }

    public void Cancel()
    {
        if (Status is WorkOrderOperationStatus.Finished or WorkOrderOperationStatus.Cancelled)
        {
            throw new DomainException($"Cannot cancel operation from status {Status}.");
        }

        Status = WorkOrderOperationStatus.Cancelled;
    }

    private void EnsureTransition(WorkOrderOperationStatus from, WorkOrderOperationStatus to)
    {
        if (Status != from)
        {
            throw new DomainException($"Cannot transition operation from {Status} to {to}.");
        }
    }
}
