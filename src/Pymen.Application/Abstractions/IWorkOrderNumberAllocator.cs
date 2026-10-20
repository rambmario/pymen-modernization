using Pymen.Domain.Production;

namespace Pymen.Application.Abstractions;

public interface IWorkOrderNumberAllocator
{
    Task<WorkOrder> CreateWorkOrderAsync(
        Guid tenantId,
        Guid customerId,
        string? notes = null,
        IReadOnlyList<(Guid ProductId, decimal Quantity)>? lines = null,
        IReadOnlyList<Guid>? operationTypeIds = null,
        CancellationToken cancellationToken = default);
}
