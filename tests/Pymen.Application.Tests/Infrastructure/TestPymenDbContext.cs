using Microsoft.EntityFrameworkCore;
using Pymen.Application.Abstractions;
using Pymen.Domain.Catalog;
using Pymen.Domain.Production;
using Pymen.Domain.Tenancy;

namespace Pymen.Application.Tests.Infrastructure;

public sealed class TestPymenDbContext : DbContext, IPymenDbContext
{
    public TestPymenDbContext(DbContextOptions<TestPymenDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<DeliveryNote> DeliveryNotes => Set<DeliveryNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrder>(b =>
        {
            b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.WorkOrderId);
            b.HasMany(x => x.Operations).WithOne().HasForeignKey(x => x.WorkOrderId);
            b.Navigation(x => x.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
            b.Navigation(x => x.Operations).HasField("_operations").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<DeliveryNote>(b =>
        {
            b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.DeliveryNoteId);
            b.Navigation(x => x.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // InMemory ignores rowversion semantics; keep property mapped.
        modelBuilder.Entity<Tenant>().Property(x => x.RowVersion).IsRequired(false);
    }
}
