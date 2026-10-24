using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pymen.Application.Abstractions;
using Pymen.Domain.Catalog;
using Pymen.Domain.Common;
using Pymen.Domain.Inventory;
using Pymen.Domain.Production;
using Pymen.Domain.Tenancy;
using Pymen.Infrastructure.Identity;
using Pymen.Infrastructure.Messaging;

namespace Pymen.Infrastructure.Persistence;

public sealed class PymenDbContext : IdentityDbContext<ApplicationUser>, IPymenDbContext
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
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PymenDbContext).Assembly);

        modelBuilder.Entity<ApplicationUser>(builder =>
        {
            builder.Property(x => x.TenantId).IsRequired();
            builder.HasIndex(x => x.TenantId);
            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        IgnoreDomainEventsOnEntities(modelBuilder);
        ApplyFailClosedTenantFilters(modelBuilder);
    }

    private static void IgnoreDomainEventsOnEntities(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            modelBuilder.Entity(entityType.ClrType).Ignore(nameof(Entity.DomainEvents));
        }
    }

    private void ApplyFailClosedTenantFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var method = typeof(PymenDbContext)
                .GetMethod(nameof(SetTenantQueryFilter), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(this, [modelBuilder]);
        }
    }

    private void SetTenantQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantOwned
    {
        Expression<Func<TEntity, bool>> filter =
            e => e.TenantId == _tenantContext.CurrentTenantIdOrEmpty;

        modelBuilder.Entity<TEntity>().HasQueryFilter(filter);
    }
}
