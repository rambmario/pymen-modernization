using FluentAssertions;
using Pymen.Domain.Tenancy;

namespace Pymen.Domain.Tests;

public class SolutionSmokeTests
{
    [Fact]
    public void Domain_assembly_is_loadable()
    {
        typeof(Tenant).Assembly.GetName().Name.Should().Be("Pymen.Domain");
    }
}
