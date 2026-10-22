using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Pymen.Domain.Common;
using Pymen.Domain.Production;
using Pymen.Domain.Tenancy;

namespace Pymen.Domain.Tests.Production;

public class WorkOrderTests
{
    private static readonly FakeTimeProvider Time = new(new DateTimeOffset(2026, 10, 19, 15, 0, 0, TimeSpan.Zero));

    private static (Tenant Tenant, WorkOrder Order) CreateDraftWithLine()
    {
        var tenant = Tenant.Create("Metalúrgica Demo", "1");
        var order = WorkOrder.Create(tenant, Guid.NewGuid(), Time);
        order.AddLine(Guid.NewGuid(), 10);
        return (tenant, order);
    }

    [Fact]
    public void Create_assigns_number_from_tenant_sequence()
    {
        var tenant = Tenant.Create("Textil Demo", "12");
        var first = WorkOrder.Create(tenant, Guid.NewGuid(), Time);
        var second = WorkOrder.Create(tenant, Guid.NewGuid(), Time);

        first.Number.Should().Be("001200000001");
        second.Number.Should().Be("001200000002");
        first.Status.Should().Be(WorkOrderStatus.Draft);
        first.TenantId.Should().Be(tenant.Id);
        first.CreatedAt.Should().Be(Time.GetUtcNow());
    }

    [Fact]
    public void Happy_path_Draft_to_Invoiced()
    {
        var (_, order) = CreateDraftWithLine();

        order.StartProduction(Time);
        order.Status.Should().Be(WorkOrderStatus.InProduction);

        order.MarkDelivered(Time);
        order.Status.Should().Be(WorkOrderStatus.Delivered);

        order.MarkInvoiced(Time);
        order.Status.Should().Be(WorkOrderStatus.Invoiced);
    }

    [Theory]
    [InlineData(WorkOrderStatus.Draft)]
    [InlineData(WorkOrderStatus.InProduction)]
    public void Cancel_allowed_from_Draft_or_InProduction(WorkOrderStatus from)
    {
        var (_, order) = CreateDraftWithLine();
        if (from == WorkOrderStatus.InProduction)
        {
            order.StartProduction(Time);
        }

        order.Cancel(Time);
        order.Status.Should().Be(WorkOrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_cancels_pending_and_started_operations()
    {
        var (_, order) = CreateDraftWithLine();
        var pending = order.AddOperation(Guid.NewGuid());
        var started = order.AddOperation(Guid.NewGuid());
        var finished = order.AddOperation(Guid.NewGuid());

        started.Start(Time);
        finished.Start(Time);
        finished.Finish(Time);

        order.StartProduction(Time);
        order.Cancel(Time);

        pending.Status.Should().Be(WorkOrderOperationStatus.Cancelled);
        started.Status.Should().Be(WorkOrderOperationStatus.Cancelled);
        finished.Status.Should().Be(WorkOrderOperationStatus.Finished);
        order.Status.Should().Be(WorkOrderStatus.Cancelled);
    }

    [Fact]
    public void Timestamps_come_from_TimeProvider()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var tenant = Tenant.Create("Demo", "1");
        var order = WorkOrder.Create(tenant, Guid.NewGuid(), clock);
        order.AddLine(Guid.NewGuid(), 1);

        order.CreatedAt.Should().Be(clock.GetUtcNow());

        clock.Advance(TimeSpan.FromHours(1));
        order.StartProduction(clock);
        order.StartedAt.Should().Be(clock.GetUtcNow());

        clock.Advance(TimeSpan.FromHours(2));
        order.MarkDelivered(clock);
        order.DeliveredAt.Should().Be(clock.GetUtcNow());
    }

    [Fact]
    public void Cancel_from_Delivered_throws()
    {
        var (_, order) = CreateDraftWithLine();
        order.StartProduction(Time);
        order.MarkDelivered(Time);

        var act = () => order.Cancel(Time);

        act.Should().Throw<DomainException>().WithMessage("*Cannot cancel*");
    }

    [Fact]
    public void Cancel_from_Invoiced_throws()
    {
        var (_, order) = CreateDraftWithLine();
        order.StartProduction(Time);
        order.MarkDelivered(Time);
        order.MarkInvoiced(Time);

        var act = () => order.Cancel(Time);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void StartProduction_from_Delivered_throws()
    {
        var (_, order) = CreateDraftWithLine();
        order.StartProduction(Time);
        order.MarkDelivered(Time);

        var act = () => order.StartProduction(Time);

        act.Should().Throw<DomainException>().WithMessage("*Cannot transition*");
    }

    [Fact]
    public void MarkDelivered_from_Draft_throws()
    {
        var (_, order) = CreateDraftWithLine();

        var act = () => order.MarkDelivered(Time);

        act.Should().Throw<DomainException>();
    }

    /// <summary>
    /// Owner decision #6: delivery is allowed with unfinished operations (Pending/Started).
    /// Operations keep their status; MarkDelivered does not cancel or finish them.
    /// </summary>
    [Fact]
    public void MarkDelivered_with_pending_or_started_operations_succeeds_and_leaves_operations_unchanged()
    {
        var (_, order) = CreateDraftWithLine();
        var pending = order.AddOperation(Guid.NewGuid());
        var started = order.AddOperation(Guid.NewGuid());
        started.Start(Time);

        order.StartProduction(Time);
        order.MarkDelivered(Time);

        order.Status.Should().Be(WorkOrderStatus.Delivered);
        pending.Status.Should().Be(WorkOrderOperationStatus.Pending);
        started.Status.Should().Be(WorkOrderOperationStatus.Started);
    }

    [Fact]
    public void MarkInvoiced_from_InProduction_throws()
    {
        var (_, order) = CreateDraftWithLine();
        order.StartProduction(Time);

        var act = () => order.MarkInvoiced(Time);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void StartProduction_without_lines_throws()
    {
        var tenant = Tenant.Create("Demo", "1");
        var order = WorkOrder.Create(tenant, Guid.NewGuid(), Time);

        var act = () => order.StartProduction(Time);

        act.Should().Throw<DomainException>().WithMessage("*without lines*");
    }

    [Fact]
    public void AddLine_after_Delivered_throws()
    {
        var (_, order) = CreateDraftWithLine();
        order.StartProduction(Time);
        order.MarkDelivered(Time);

        var act = () => order.AddLine(Guid.NewGuid(), 1);

        act.Should().Throw<DomainException>().WithMessage("*Cannot modify*");
    }
}
