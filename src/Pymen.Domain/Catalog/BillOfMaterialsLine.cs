using Pymen.Domain.Common;

namespace Pymen.Domain.Catalog;

/// <summary>
/// Recipe line: quantity of a raw-material product required per finished-good unit.
/// </summary>
public sealed class BillOfMaterialsLine : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid FinishedProductId { get; private set; }
    public Guid RawMaterialProductId { get; private set; }
    public decimal QuantityPerUnit { get; private set; }

    private BillOfMaterialsLine()
    {
    }

    public static BillOfMaterialsLine Create(
        Guid tenantId,
        Guid finishedProductId,
        Guid rawMaterialProductId,
        decimal quantityPerUnit)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("TenantId is required.");
        }

        if (finishedProductId == Guid.Empty || rawMaterialProductId == Guid.Empty)
        {
            throw new DomainException("Finished and raw-material product ids are required.");
        }

        if (finishedProductId == rawMaterialProductId)
        {
            throw new DomainException("A product cannot be a component of itself.");
        }

        if (quantityPerUnit <= 0)
        {
            throw new DomainException("BOM quantity must be greater than zero.");
        }

        return new BillOfMaterialsLine
        {
            TenantId = tenantId,
            FinishedProductId = finishedProductId,
            RawMaterialProductId = rawMaterialProductId,
            QuantityPerUnit = quantityPerUnit
        };
    }

    public decimal CalculateConsumption(decimal producedQuantity)
    {
        if (producedQuantity <= 0)
        {
            throw new DomainException("Produced quantity must be greater than zero.");
        }

        return producedQuantity * QuantityPerUnit;
    }
}
