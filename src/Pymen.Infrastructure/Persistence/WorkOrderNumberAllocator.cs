using Microsoft.EntityFrameworkCore;
using Pymen.Application.Abstractions;
using Pymen.Domain.Common;
using Pymen.Domain.Production;

namespace Pymen.Infrastructure.Persistence;

public sealed class WorkOrderNumberAllocator : IWorkOrderNumberAllocator
{
    private readonly PymenDbContext _db;
    private readonly TimeProvider _timeProvider;

    public WorkOrderNumberAllocator(PymenDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<WorkOrder> CreateWorkOrderAsync(
        Guid tenantId,
        Guid customerId,
        string? notes = null,
        IReadOnlyList<(Guid ProductId, decimal Quantity)>? lines = null,
        IReadOnlyList<Guid>? operationTypeIds = null,
        CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 8;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            _db.ChangeTracker.Clear();

            var tenant = await _db.Tenants
                .SingleOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
                ?? throw new DomainException($"Tenant '{tenantId}' was not found.");

            var customerExists = await _db.Customers
                .IgnoreQueryFilters()
                .AnyAsync(c => c.Id == customerId && c.TenantId == tenantId, cancellationToken);

            if (!customerExists)
            {
                throw new DomainException("Customer was not found for the tenant.");
            }

            var workOrder = WorkOrder.Create(tenant, customerId, _timeProvider, notes);

            if (lines is not null)
            {
                foreach (var (productId, quantity) in lines)
                {
                    workOrder.AddLine(productId, quantity);
                }
            }

            if (operationTypeIds is not null)
            {
                foreach (var operationTypeId in operationTypeIds)
                {
                    workOrder.AddOperation(operationTypeId);
                }
            }

            _db.WorkOrders.Add(workOrder);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return workOrder;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                _db.ChangeTracker.Clear();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var entities = string.Join(", ", ex.Entries.Select(e => e.Metadata.ClrType.Name));
                throw new DomainException(
                    $"Could not allocate a unique work-order number (concurrency on: {entities}).");
            }
        }

        throw new DomainException("Could not allocate a unique work-order number after concurrent retries.");
    }
}
