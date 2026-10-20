using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pymen.Api.IntegrationTests;

public class SolutionSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SolutionSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_endpoint_returns_success()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
