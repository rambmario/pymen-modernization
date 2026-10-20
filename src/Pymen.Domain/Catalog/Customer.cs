using Pymen.Domain.Common;

namespace Pymen.Domain.Catalog;

public sealed class Customer : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? TaxId { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Customer()
    {
    }

    public static Customer Create(Guid tenantId, string name, string? taxId = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("TenantId is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Customer name is required.");
        }

        return new Customer
        {
            TenantId = tenantId,
            Name = name.Trim(),
            TaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim(),
            IsActive = true
        };
    }

    public void Update(string name, string? taxId, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Customer name is required.");
        }

        Name = name.Trim();
        TaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim();
        IsActive = isActive;
    }
}
