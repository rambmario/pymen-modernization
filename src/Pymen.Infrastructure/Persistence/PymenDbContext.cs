using Microsoft.EntityFrameworkCore;
using Pymen.Application.Abstractions;
using Pymen.Domain.Catalog;
using Pymen.Domain.Inventory;
using Pymen.Domain.Production;
using Pymen.Domain.Tenancy;

namespace Pymen.Infrastructure.Persistence;

public sealed class PymenDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public PymenDbContext(DbContextOptions<PymenDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<BillOfMaterialsLine> BillOfMaterialsLines => Set<BillOfMaterialsLine>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderLine> WorkOrderLines => Set<WorkOrderLine>();
    public DbSet<WorkOrderOperation> WorkOrderOperations => Set<WorkOrderOperation>();
    public DbSet<OperationType> OperationTypes => Set<OperationType>();
    public DbSet<DeliveryNote> DeliveryNotes => Set<DeliveryNote>();
    public DbSet<DeliveryNoteLine> DeliveryNoteLines => Set<DeliveryNoteLine>();
    public DbSet<ProductionReceipt> ProductionReceipts => Set<ProductionReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PymenDbContext).Assembly);

        modelBuilder.Entity<Customer>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Product>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<BillOfMaterialsLine>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Warehouse>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<StockBalance>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<WorkOrder>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<WorkOrderLine>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<WorkOrderOperation>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<OperationType>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<DeliveryNote>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<DeliveryNoteLine>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<ProductionReceipt>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
    }
}
