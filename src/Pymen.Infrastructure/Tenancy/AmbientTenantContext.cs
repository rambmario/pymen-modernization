using Pymen.Application.Abstractions;

namespace Pymen.Infrastructure.Tenancy;

public sealed class AmbientTenantContext : ITenantContext
{
    private Guid? _tenantId;

    public Guid TenantId =>
        _tenantId ?? throw new InvalidOperationException("Tenant context is not set.");

    public Guid CurrentTenantIdOrEmpty => _tenantId ?? Guid.Empty;

    public bool HasTenant => _tenantId.HasValue;

    public void SetTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        _tenantId = tenantId;
    }

    public void Clear() => _tenantId = null;
}
