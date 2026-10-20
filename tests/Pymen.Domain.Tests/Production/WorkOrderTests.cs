using FluentAssertions;
using Pymen.Domain.Common;
using Pymen.Domain.Production;
using Pymen.Domain.Tenancy;

namespace Pymen.Domain.Tests.Production;

public class WorkOrderTests
{
    private static (Tenant Tenant, WorkOrder Order) CreateDraftWithLine()
    {
        var tenant = Tenant.Create("Metalúrgica Demo", "1");
        var order = WorkOrder.Create(tenant, Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), 10);
        return (tenant, order);
    }

    [Fact]
    public void Create_assigns_number_from_tenant_sequence()
    {
        var tenant = Tenant.Create("Textil Demo", "12");
        var first = WorkOrder.Create(tenant, Guid.NewGuid());
        var second = WorkOrder.Create(tenant, Guid.NewGuid());

        first.Number.Should().Be("001200000001");
        second.Number.Should().Be("001200000002");
        first.Status.Should().Be(WorkOrderStatus.Draft);
        first.TenantId.Should().Be(tenant.Id);
    }

    [Fact]
    public void Happy_path_Draft_to_Invoiced()
    {
        var (_, order) = CreateDraftWithLine();

        order.StartProduction();
        order.Status.Should().Be(WorkOrderStatus.InProduction);

        order.MarkDelivered();
        order.Status.Should().Be(WorkOrderStatus.Delivered);

        order.MarkInvoiced();
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
            order.StartProduction();
        }

        order.Cancel();
        order.Status.Should().Be(WorkOrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_from_Delivered_throws()
    {
        var (_, order) = CreateDraftWithLine();
        order.StartProduction();
        order.MarkDelivered();

        var act = () => order.Cancel();

        act.Should().Throw<DomainException>().WithMessage("*Cannot cancel*");
    }

    [Fact]
    public void Cancel_from_Invoiced_throws()
    {
        var (_, order) = CreateDraftWithLine();
        order.StartProduction();
        order.MarkDelivered();
        order.MarkInvoiced();

        var act = () => order.Cancel();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void StartProduction_from_Delivered_throws()
    {
        var (_, order) = CreateDraftWithLine();
        order.StartProduction();
        order.MarkDelivered();

        var act = () => order.StartProduction();

        act.Should().Throw<DomainException>().WithMessage("*Cannot transition*");
    }

    [Fact]
    public void MarkDelivered_from_Draft_throws()
    {
        var (_, order) = CreateDraftWithLine();

        var act = () => order.MarkDelivered();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkInvoiced_from_InProduction_throws()
    {
        var (_, order) = CreateDraftWithLine();
        order.StartProduction();

        var act = () => order.MarkInvoiced();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void StartProduction_without_lines_throws()
    {
        var tenant = Tenant.Create("Demo", "1");
        var order = WorkOrder.Create(tenant, Guid.NewGuid());

        var act = () => order.StartProduction();

        act.Should().Throw<DomainException>().WithMessage("*without lines*");
    }

    [Fact]
    public void AddLine_after_Delivered_throws()
    {
        var (_, order) = CreateDraftWithLine();
        order.StartProduction();
        order.MarkDelivered();

        var act = () => order.AddLine(Guid.NewGuid(), 1);

        act.Should().Throw<DomainException>().WithMessage("*Cannot modify*");
    }
}
