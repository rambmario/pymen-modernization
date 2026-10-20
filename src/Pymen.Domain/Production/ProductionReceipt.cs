using Pymen.Domain.Catalog;
using Pymen.Domain.Common;
using Pymen.Domain.Inventory;

namespace Pymen.Domain.Production;

/// <summary>
/// Records finished-goods receipt into stock and consumes BOM raw materials.
/// This is the path that moves stock (delivery notes do not).
/// </summary>
public sealed class ProductionReceipt : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? WorkOrderId { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public bool IsApplied { get; private set; }

    private ProductionReceipt()
    {
    }

    public static ProductionReceipt Create(
        Guid tenantId,
        Guid productId,
        Guid warehouseId,
        decimal quantity,
        Guid? workOrderId = null,
        DateTimeOffset? receivedAt = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("TenantId is required.");
        }

        if (productId == Guid.Empty || warehouseId == Guid.Empty)
        {
            throw new DomainException("Product and warehouse ids are required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Receipt quantity must be greater than zero.");
        }

        return new ProductionReceipt
        {
            TenantId = tenantId,
            ProductId = productId,
            WarehouseId = warehouseId,
            WorkOrderId = workOrderId,
            Quantity = quantity,
            ReceivedAt = receivedAt ?? DateTimeOffset.UtcNow,
            IsApplied = false
        };
    }

    public void Apply(
        StockBalance finishedGoodsStock,
        IReadOnlyCollection<BillOfMaterialsLine> bomLines,
        IReadOnlyDictionary<Guid, StockBalance> rawMaterialStocks)
    {
        if (IsApplied)
        {
            throw new DomainException("Production receipt was already applied to stock.");
        }

        ArgumentNullException.ThrowIfNull(finishedGoodsStock);
        ArgumentNullException.ThrowIfNull(bomLines);
        ArgumentNullException.ThrowIfNull(rawMaterialStocks);

        if (finishedGoodsStock.TenantId != TenantId
            || finishedGoodsStock.ProductId != ProductId
            || finishedGoodsStock.WarehouseId != WarehouseId)
        {
            throw new DomainException("Finished-goods stock balance does not match this receipt.");
        }

        foreach (var line in bomLines)
        {
            if (line.TenantId != TenantId || line.FinishedProductId != ProductId)
            {
                throw new DomainException("BOM line does not belong to this finished product/tenant.");
            }

            if (!rawMaterialStocks.TryGetValue(line.RawMaterialProductId, out var rawStock))
            {
                throw new DomainException($"Missing stock balance for raw material {line.RawMaterialProductId}.");
            }

            if (rawStock.TenantId != TenantId || rawStock.WarehouseId != WarehouseId)
            {
                throw new DomainException("Raw-material stock balance tenant/warehouse mismatch.");
            }

            rawStock.Decrease(line.CalculateConsumption(Quantity));
        }

        finishedGoodsStock.Increase(Quantity);
        IsApplied = true;
    }
}
