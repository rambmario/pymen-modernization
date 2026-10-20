using Pymen.Domain.Common;

namespace Pymen.Domain.Production;

public sealed class DeliveryNoteLine : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid DeliveryNoteId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public int LineNumber { get; private set; }

    private DeliveryNoteLine()
    {
    }

    internal static DeliveryNoteLine Create(
        Guid tenantId,
        Guid deliveryNoteId,
        Guid productId,
        decimal quantity,
        int lineNumber)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("TenantId is required.");
        }

        if (deliveryNoteId == Guid.Empty || productId == Guid.Empty)
        {
            throw new DomainException("Delivery note and product ids are required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Delivery quantity must be greater than zero.");
        }

        return new DeliveryNoteLine
        {
            TenantId = tenantId,
            DeliveryNoteId = deliveryNoteId,
            ProductId = productId,
            Quantity = quantity,
            LineNumber = lineNumber
        };
    }
}
