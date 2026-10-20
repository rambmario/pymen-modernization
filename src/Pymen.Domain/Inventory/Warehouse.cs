using Pymen.Domain.Common;

namespace Pymen.Domain.Inventory;

public sealed class Warehouse : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private Warehouse()
    {
    }

    public static Warehouse Create(Guid tenantId, string code, string name)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("TenantId is required.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Warehouse code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Warehouse name is required.");
        }

        return new Warehouse
        {
            TenantId = tenantId,
            Code = code.Trim(),
            Name = name.Trim()
        };
    }
}
