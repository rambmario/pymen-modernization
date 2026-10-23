using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Pymen.Api.IntegrationTests.Infrastructure;

public sealed class PymenApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqlServerFixture _sql;
    private readonly string _connectionString;

    public PymenApiFactory(SqlServerFixture sql)
    {
        _sql = sql;
        // Dedicated catalog so concurrency/isolation tests do not skip demo tenant/user seed.
        var builder = new SqlConnectionStringBuilder(sql.ConnectionString)
        {
            InitialCatalog = "Pymen_Api_Integration"
        };
        _connectionString = builder.ConnectionString;
    }

    public bool IsAvailable => _sql.IsAvailable;

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        TestEnvironment.EnsureLoaded();

        Environment.SetEnvironmentVariable("JWT_SIGNING_KEY", "TEST_ONLY_signing_key_32chars_min!!");
        Environment.SetEnvironmentVariable("DEMO_PASSWORD_ADMIN", "AdminPass123");
        Environment.SetEnvironmentVariable("DEMO_PASSWORD_PLANNER", "PlannerPass123");
        Environment.SetEnvironmentVariable("DEMO_PASSWORD_OPERATOR", "OperatorPass123");

        builder.UseEnvironment("Development");
        builder.UseSetting("SkipSeed", "false");
        builder.UseSetting("ConnectionStrings:Pymen", _connectionString);
        builder.UseSetting("Jwt:SigningKey", "TEST_ONLY_signing_key_32chars_min!!");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Pymen"] = _connectionString,
                ["Jwt:SigningKey"] = "TEST_ONLY_signing_key_32chars_min!!",
                ["SkipSeed"] = "false",
                ["Seed:Enabled"] = "true",
                ["EnableAuthProbe"] = "true",
                // No Service Bus in API integration tests; outbox rows still persist via interceptor.
                ["Outbox:PublisherEnabled"] = "false"
            });
        });
    }
}
