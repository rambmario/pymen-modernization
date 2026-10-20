namespace Pymen.Domain.Common;

public interface ITenantOwned
{
    Guid TenantId { get; }
}
