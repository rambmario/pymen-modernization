using FluentAssertions;
using Pymen.Domain;

namespace Pymen.Domain.Tests;

public class SolutionSmokeTests
{
    [Fact]
    public void Domain_assembly_is_loadable()
    {
        typeof(AssemblyMarker).Assembly.GetName().Name.Should().Be("Pymen.Domain");
    }
}
