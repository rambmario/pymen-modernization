using Microsoft.EntityFrameworkCore;
using Pymen.Domain.Catalog;
using Pymen.Domain.Production;
using Pymen.Domain.Tenancy;

namespace Pymen.Application.Abstractions;

public interface IPymenDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Product> Products { get; }
    DbSet<WorkOrder> WorkOrders { get; }
    DbSet<DeliveryNote> DeliveryNotes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
