using Pymen.Application.Abstractions;

namespace Pymen.Application.Tests.Infrastructure;

public sealed class TestTenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }
    public Guid CurrentTenantIdOrEmpty => HasTenant ? TenantId : Guid.Empty;
    public bool HasTenant { get; private set; }

    public void SetTenant(Guid tenantId)
    {
        TenantId = tenantId;
        HasTenant = true;
    }
}
