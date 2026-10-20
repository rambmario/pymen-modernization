using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Pymen.Api.IntegrationTests.Infrastructure;
using Pymen.Infrastructure.Persistence;

namespace Pymen.Api.IntegrationTests;

public class SolutionSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SolutionSmokeTests(WebApplicationFactory<Program> factory)
    {
        TestEnvironment.EnsureLoaded();
        var connection = DotEnv.BuildLocalSqlConnectionString("Pymen_Smoke", connectTimeoutSeconds: 1);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SkipSeed", "true");
            builder.UseSetting("ConnectionStrings:Pymen", connection);
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
