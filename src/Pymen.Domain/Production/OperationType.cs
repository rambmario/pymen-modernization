using Pymen.Domain.Common;

namespace Pymen.Domain.Production;

public sealed class OperationType : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private OperationType()
    {
    }

    public static OperationType Create(Guid tenantId, string code, string name)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("TenantId is required.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Operation type code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Operation type name is required.");
        }

        return new OperationType
        {
            TenantId = tenantId,
            Code = code.Trim(),
            Name = name.Trim()
        };
    }
}
