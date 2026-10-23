using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Pymen.Domain.Catalog;
using Pymen.Domain.Events;
using Pymen.Domain.Inventory;
using Pymen.Domain.Production;
using Pymen.Domain.Tenancy;

namespace Pymen.Domain.Tests.Events;

public class DomainEventTests
{
    private static readonly FakeTimeProvider Time = new(new DateTimeOffset(2026, 10, 22, 20, 0, 0, TimeSpan.Zero));

    [Fact]
    public void MarkDelivered_raises_WorkOrderDelivered()
    {
        var tenant = Tenant.Create("Demo", "1");
        var order = WorkOrder.Create(tenant, Guid.NewGuid(), Time);
        order.AddLine(Guid.NewGuid(), 1);
        order.StartProduction(Time);

        order.MarkDelivered(Time);

        var delivered = order.DomainEvents.OfType<WorkOrderDelivered>().Should().ContainSingle().Subject;
        delivered.WorkOrderId.Should().Be(order.Id);
        delivered.TenantId.Should().Be(tenant.Id);
        delivered.WorkOrderNumber.Should().Be(order.Number);
        delivered.OccurredAt.Should().Be(order.DeliveredAt);
    }

    [Fact]
    public void Cancel_raises_WorkOrderCancelled()
    {
        var tenant = Tenant.Create("Demo", "1");
        var order = WorkOrder.Create(tenant, Guid.NewGuid(), Time);
        order.AddLine(Guid.NewGuid(), 1);

        order.Cancel(Time);

        var cancelled = order.DomainEvents.OfType<WorkOrderCancelled>().Should().ContainSingle().Subject;
        cancelled.WorkOrderId.Should().Be(order.Id);
        cancelled.TenantId.Should().Be(tenant.Id);
    }

    [Fact]
    public void ProductionReceipt_Apply_raises_ProductionReceiptApplied()
    {
        var tenantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var finishedId = Guid.NewGuid();
        var finishedStock = StockBalance.Create(tenantId, finishedId, warehouseId, 0);
        var receipt = ProductionReceipt.Create(tenantId, finishedId, warehouseId, 2, Time);

        receipt.Apply(finishedStock, [], new Dictionary<Guid, StockBalance>());

        var applied = receipt.DomainEvents.OfType<ProductionReceiptApplied>().Should().ContainSingle().Subject;
        applied.ProductionReceiptId.Should().Be(receipt.Id);
        applied.Quantity.Should().Be(2);
        applied.TenantId.Should().Be(tenantId);
    }
}
