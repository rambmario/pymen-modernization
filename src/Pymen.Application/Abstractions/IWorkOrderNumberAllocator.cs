using Pymen.Domain.Production;

namespace Pymen.Application.Abstractions;

public interface IWorkOrderNumberAllocator
{
    Task<WorkOrder> CreateWorkOrderAsync(
        Guid tenantId,
        Guid customerId,
        string? notes = null,
        CancellationToken cancellationToken = default);
}
