using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pymen.Api.IntegrationTests;

public class SolutionSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SolutionSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SkipSeed", "true");
            builder.UseSetting("ConnectionStrings:Pymen",
                "Server=127.0.0.1,1433;Database=Pymen_Smoke;User Id=sa;Password=Your_password123;TrustServerCertificate=True;Encrypt=False;Connect Timeout=1");
        });
    }

    [Fact]
    public async Task Liveness_health_endpoint_returns_success()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
