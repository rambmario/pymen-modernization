using FluentAssertions;
using Pymen.Domain.Common;
using Pymen.Domain.Tenancy;

namespace Pymen.Domain.Tests.Tenancy;

public class TenantNumberingTests
{
    [Fact]
    public void Point_of_sale_is_padded_to_four_digits()
    {
        var tenant = Tenant.Create("Demo Co", "7");
        tenant.PointOfSaleCode.Should().Be("0007");
        tenant.AllocateWorkOrderNumber().Should().Be("000700000001");
    }

    [Fact]
    public void Empty_name_throws()
    {
        var act = () => Tenant.Create(" ", "1");
        act.Should().Throw<DomainException>();
    }
}
