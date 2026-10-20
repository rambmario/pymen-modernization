using Pymen.Domain.Common;

namespace Pymen.Domain.Catalog;

public sealed class Product : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsRawMaterial { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Product()
    {
    }

    public static Product Create(
        Guid tenantId,
        string sku,
        string name,
        bool isRawMaterial)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("TenantId is required.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("Product SKU is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product name is required.");
        }

        return new Product
        {
            TenantId = tenantId,
            Sku = sku.Trim(),
            Name = name.Trim(),
            IsRawMaterial = isRawMaterial,
            IsActive = true
        };
    }

    public void Update(string name, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product name is required.");
        }

        Name = name.Trim();
        IsActive = isActive;
    }
}
