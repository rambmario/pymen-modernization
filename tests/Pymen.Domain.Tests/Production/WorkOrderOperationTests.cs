using FluentAssertions;
using Pymen.Domain.Common;
using Pymen.Domain.Production;
using Pymen.Domain.Tenancy;

namespace Pymen.Domain.Tests.Production;

public class WorkOrderOperationTests
{
    private static WorkOrderOperation CreatePendingOperation()
    {
        var tenant = Tenant.Create("Demo", "1");
        var order = WorkOrder.Create(tenant, Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), 1);
        return order.AddOperation(Guid.NewGuid());
    }

    [Fact]
    public void Happy_path_Pending_to_Finished()
    {
        var op = CreatePendingOperation();
        op.Status.Should().Be(WorkOrderOperationStatus.Pending);

        op.Start();
        op.Status.Should().Be(WorkOrderOperationStatus.Started);
        op.StartedAt.Should().NotBeNull();

        op.Finish();
        op.Status.Should().Be(WorkOrderOperationStatus.Finished);
        op.FinishedAt.Should().NotBeNull();
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
        op.Start();
        op.Cancel();
        op.Status.Should().Be(WorkOrderOperationStatus.Cancelled);
    }

    [Fact]
    public void Cancel_from_Finished_throws()
    {
        var op = CreatePendingOperation();
        op.Start();
        op.Finish();

        var act = () => op.Cancel();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Finish_from_Pending_throws()
    {
        var op = CreatePendingOperation();

        var act = () => op.Finish();

        act.Should().Throw<DomainException>().WithMessage("*Cannot transition*");
    }

    [Fact]
    public void Start_from_Finished_throws()
    {
        var op = CreatePendingOperation();
        op.Start();
        op.Finish();

        var act = () => op.Start();

        act.Should().Throw<DomainException>();
    }
}
