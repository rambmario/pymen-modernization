using Pymen.Domain.Common;

namespace Pymen.Domain.Inventory;

public sealed class StockBalance : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public decimal Quantity { get; private set; }

    private StockBalance()
    {
    }

    public static StockBalance Create(Guid tenantId, Guid productId, Guid warehouseId, decimal quantity = 0)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("TenantId is required.");
        }

        if (productId == Guid.Empty || warehouseId == Guid.Empty)
        {
            throw new DomainException("Product and warehouse ids are required.");
        }

        if (quantity < 0)
        {
            throw new DomainException("Initial stock quantity cannot be negative.");
        }

        return new StockBalance
        {
            TenantId = tenantId,
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = quantity
        };
    }

    public void Increase(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("Increase amount must be greater than zero.");
        }

        Quantity += amount;
    }

    public void Decrease(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("Decrease amount must be greater than zero.");
        }

        if (amount > Quantity)
        {
            throw new DomainException("Insufficient stock.");
        }

        Quantity -= amount;
    }
}
