using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Pymen.Application.Abstractions;
using Pymen.Application.Production;
using Pymen.Application.Tests.Infrastructure;
using Pymen.Domain.Catalog;
using Pymen.Domain.Common;
using Pymen.Domain.Production;
using Pymen.Domain.Tenancy;

namespace Pymen.Application.Tests.Production;

public sealed class WorkOrderServiceTests
{
    [Fact]
    public async Task Get_missing_work_order_returns_null()
    {
        await using var db = CreateDb();
        var tenant = await SeedTenantAsync(db);
        var sut = CreateSut(db, tenant, new InMemoryAllocator(db, TimeProvider.System));

        var result = await sut.GetAsync(Guid.CreateVersion7());
        result.Should().BeNull();
    }

    [Fact]
    public async Task Start_cancel_and_missing_id_share_find_semantics()
    {
        await using var db = CreateDb();
        var tenant = await SeedTenantAsync(db);
        var customer = Customer.Create(tenant.Id, "Cliente");
        var product = Product.Create(tenant.Id, "FG-1", "Finished", false);
        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-10-19T12:00:00Z"));
        var sut = CreateSut(db, tenant, new InMemoryAllocator(db, time), time);

        var created = await sut.CreateAsync(new CreateWorkOrderRequest(
            customer.Id,
            "notes",
            [new WorkOrderLineRequest(product.Id, 2)],
            null));

        var started = await sut.StartAsync(created.Id);
        started.Should().NotBeNull();
        started!.Status.Should().Be(nameof(WorkOrderStatus.InProduction));

        var cancelled = await sut.CancelAsync(created.Id);
        cancelled.Should().NotBeNull();
        cancelled!.Status.Should().Be(nameof(WorkOrderStatus.Cancelled));

        (await sut.StartAsync(Guid.CreateVersion7())).Should().BeNull();
    }

    [Fact]
    public async Task Delivery_note_create_returns_null_when_work_order_missing()
    {
        await using var db = CreateDb();
        var tenant = await SeedTenantAsync(db);
        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-10-19T12:00:00Z"));
        var tenantContext = new TestTenantContext();
        tenantContext.SetTenant(tenant.Id);
        var sut = new DeliveryNoteService(db, tenantContext, time);

        var result = await sut.CreateAsync(new CreateDeliveryNoteRequest(
            Guid.CreateVersion7(),
            "RN-1",
            null,
            [new WorkOrderLineRequest(Guid.CreateVersion7(), 1)]));

        result.Should().BeNull();
    }

    private static WorkOrderService CreateSut(
        TestPymenDbContext db,
        Tenant tenant,
        IWorkOrderNumberAllocator allocator,
        TimeProvider? time = null)
    {
        var tenantContext = new TestTenantContext();
        tenantContext.SetTenant(tenant.Id);
        return new WorkOrderService(db, tenantContext, allocator, time ?? TimeProvider.System);
    }

    private static async Task<Tenant> SeedTenantAsync(TestPymenDbContext db)
    {
        var tenant = Tenant.Create("T", "3");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant;
    }

    private static TestPymenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestPymenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new TestPymenDbContext(options);
    }

    private sealed class InMemoryAllocator : IWorkOrderNumberAllocator
    {
        private readonly TestPymenDbContext _db;
        private readonly TimeProvider _time;

        public InMemoryAllocator(TestPymenDbContext db, TimeProvider time)
        {
            _db = db;
            _time = time;
        }

        public async Task<WorkOrder> CreateWorkOrderAsync(
            Guid tenantId,
            Guid customerId,
            string? notes = null,
            IReadOnlyList<(Guid ProductId, decimal Quantity)>? lines = null,
            IReadOnlyList<Guid>? operationTypeIds = null,
            CancellationToken cancellationToken = default)
        {
            var tenant = await _db.Tenants.SingleAsync(t => t.Id == tenantId, cancellationToken);
            var customerExists = await _db.Customers.AnyAsync(
                c => c.Id == customerId && c.TenantId == tenantId,
                cancellationToken);
            if (!customerExists)
            {
                throw new DomainException("Customer was not found for the tenant.");
            }

            var workOrder = WorkOrder.Create(tenant, customerId, _time, notes);
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
            await _db.SaveChangesAsync(cancellationToken);
            return workOrder;
        }
    }
}
