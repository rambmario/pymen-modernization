namespace Pymen.Application.Abstractions;

public interface ITenantContext
{
    Guid TenantId { get; }

    /// <summary>
    /// Tenant id for fail-closed query filters. Empty when no tenant is set (matches nothing).
    /// </summary>
    Guid CurrentTenantIdOrEmpty { get; }

    bool HasTenant { get; }
}
