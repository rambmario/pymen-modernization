using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Pymen.Domain.Common;
using Pymen.Domain.Production;
using Pymen.Domain.Tenancy;

namespace Pymen.Domain.Tests.Production;

public class WorkOrderOperationTests
{
    private static readonly FakeTimeProvider Time = new(new DateTimeOffset(2026, 10, 19, 15, 0, 0, TimeSpan.Zero));

    private static WorkOrderOperation CreatePendingOperation()
    {
        var tenant = Tenant.Create("Demo", "1");
        var order = WorkOrder.Create(tenant, Guid.NewGuid(), Time);
        order.AddLine(Guid.NewGuid(), 1);
        return order.AddOperation(Guid.NewGuid());
    }

    [Fact]
    public void Happy_path_Pending_to_Finished()
    {
        var op = CreatePendingOperation();
        op.Status.Should().Be(WorkOrderOperationStatus.Pending);

        op.Start(Time);
        op.Status.Should().Be(WorkOrderOperationStatus.Started);
        op.StartedAt.Should().Be(Time.GetUtcNow());

        var later = new FakeTimeProvider(Time.GetUtcNow().AddMinutes(30));
        op.Finish(later);
        op.Status.Should().Be(WorkOrderOperationStatus.Finished);
        op.FinishedAt.Should().Be(later.GetUtcNow());
    }

    [Fact]
    public void Cancel_from_Pending_succeeds()
    {
        var op = CreatePendingOperation();
        op.Cancel();
        op.Status.Should().Be(WorkOrderOperationStatus.Cancelled);
    }

    [Fact]
    public void Cancel_from_Started_succeeds()
    {
        var op = CreatePendingOperation();
        op.Start(Time);
        op.Cancel();
        op.Status.Should().Be(WorkOrderOperationStatus.Cancelled);
    }

    [Fact]
    public void Cancel_from_Finished_throws()
    {
        var op = CreatePendingOperation();
        op.Start(Time);
        op.Finish(Time);

        var act = () => op.Cancel();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Finish_from_Pending_throws()
    {
        var op = CreatePendingOperation();

        var act = () => op.Finish(Time);

        act.Should().Throw<DomainException>().WithMessage("*Cannot transition*");
    }

    [Fact]
    public void Start_from_Finished_throws()
    {
        var op = CreatePendingOperation();
        op.Start(Time);
        op.Finish(Time);

        var act = () => op.Start(Time);

        act.Should().Throw<DomainException>();
    }
}
