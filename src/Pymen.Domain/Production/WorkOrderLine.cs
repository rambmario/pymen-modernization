using Pymen.Domain.Common;

namespace Pymen.Domain.Production;

public sealed class WorkOrderLine : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public int LineNumber { get; private set; }

    private WorkOrderLine()
    {
    }

    internal static WorkOrderLine Create(
        Guid tenantId,
        Guid workOrderId,
        Guid productId,
        decimal quantity,
        int lineNumber)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("TenantId is required.");
        }

        if (workOrderId == Guid.Empty || productId == Guid.Empty)
        {
            throw new DomainException("Work order and product ids are required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Line quantity must be greater than zero.");
        }

        if (lineNumber <= 0)
        {
            throw new DomainException("Line number must be greater than zero.");
        }

        return new WorkOrderLine
        {
            TenantId = tenantId,
            WorkOrderId = workOrderId,
            ProductId = productId,
            Quantity = quantity,
            LineNumber = lineNumber
        };
    }
}
