using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pymen.Api.IntegrationTests.Infrastructure;
using Pymen.Application.Abstractions;
using Pymen.Domain.Catalog;
using Pymen.Domain.Production;
using Pymen.Domain.Tenancy;
using Pymen.Infrastructure.Persistence;
using Pymen.Infrastructure.Tenancy;

namespace Pymen.Api.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class TenantIsolationTests
{
    private readonly SqlServerFixture _sql;

    public TenantIsolationTests(SqlServerFixture sql)
    {
        _sql = sql;
    }

    [SkippableFact]
    public async Task Tenant_A_queries_exclude_tenant_B_and_empty_without_tenant()
    {
        Skip.If(!_sql.IsAvailable, "Docker is not available for Testcontainers SQL Server.");

        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AmbientTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());
        services.AddDbContext<PymenDbContext>(options =>
            options.UseSqlServer(_sql.ConnectionString));

        await using var provider = services.BuildServiceProvider();
        await using var seedScope = provider.CreateAsyncScope();
        var seedDb = seedScope.ServiceProvider.GetRequiredService<PymenDbContext>();
        var time = seedScope.ServiceProvider.GetRequiredService<TimeProvider>();
        var seedTenant = seedScope.ServiceProvider.GetRequiredService<AmbientTenantContext>();
        seedTenant.Clear();

        await seedDb.Database.MigrateAsync();

        var tenantA = Tenant.Create("Tenant A Isolation", "11");
        var tenantB = Tenant.Create("Tenant B Isolation", "12");
        seedDb.Tenants.AddRange(tenantA, tenantB);

        var customerA = Customer.Create(tenantA.Id, "Cliente Solo A");
        var customerB = Customer.Create(tenantB.Id, "Cliente Solo B");
        seedDb.Customers.AddRange(customerA, customerB);

        var orderA = WorkOrder.Create(tenantA, customerA.Id, time, "OT A");
        var orderB = WorkOrder.Create(tenantB, customerB.Id, time, "OT B");
        seedDb.WorkOrders.AddRange(orderA, orderB);
        await seedDb.SaveChangesAsync();

        await using (var scopeA = provider.CreateAsyncScope())
        {
            var tenantContext = scopeA.ServiceProvider.GetRequiredService<AmbientTenantContext>();
            var db = scopeA.ServiceProvider.GetRequiredService<PymenDbContext>();
            tenantContext.SetTenant(tenantA.Id);

            var customers = await db.Customers.ToListAsync();
            var workOrders = await db.WorkOrders.ToListAsync();

            customers.Should().ContainSingle(c => c.Id == customerA.Id);
            customers.Should().NotContain(c => c.Id == customerB.Id);
            workOrders.Should().ContainSingle(o => o.Id == orderA.Id);
            workOrders.Should().NotContain(o => o.Id == orderB.Id);
        }

        await using (var scopeNone = provider.CreateAsyncScope())
        {
            var tenantContext = scopeNone.ServiceProvider.GetRequiredService<AmbientTenantContext>();
            var db = scopeNone.ServiceProvider.GetRequiredService<PymenDbContext>();
            tenantContext.Clear();

            (await db.Customers.CountAsync()).Should().Be(0);
            (await db.WorkOrders.CountAsync()).Should().Be(0);
        }
    }
}
