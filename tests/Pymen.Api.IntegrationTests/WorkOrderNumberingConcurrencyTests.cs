using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pymen.Api.IntegrationTests.Infrastructure;
using Pymen.Application.Abstractions;
using Pymen.Domain.Catalog;
using Pymen.Domain.Tenancy;
using Pymen.Infrastructure.Persistence;
using Pymen.Infrastructure.Tenancy;

namespace Pymen.Api.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class WorkOrderNumberingConcurrencyTests
{
    private readonly SqlServerFixture _sql;

    public WorkOrderNumberingConcurrencyTests(SqlServerFixture sql)
    {
        _sql = sql;
    }

    [SkippableFact]
    public async Task Parallel_creates_produce_distinct_numbers()
    {
        Skip.If(!_sql.IsAvailable, "Docker is not available for Testcontainers SQL Server.");

        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AmbientTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());
        services.AddScoped<IWorkOrderNumberAllocator, WorkOrderNumberAllocator>();
        services.AddDbContext<PymenDbContext>(options =>
            options.UseSqlServer(_sql.ConnectionString));

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PymenDbContext>();
        await db.Database.MigrateAsync();

        var tenant = Tenant.Create("Concurrency Demo", "9");
        var customer = Customer.Create(tenant.Id, "Cliente Concurrency");
        db.Tenants.Add(tenant);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var tenantId = tenant.Id;
        var customerId = customer.Id;

        var tasks = Enumerable.Range(0, 2)
            .Select(_ => Task.Run(async () =>
            {
                await using var inner = provider.CreateAsyncScope();
                var allocator = inner.ServiceProvider.GetRequiredService<IWorkOrderNumberAllocator>();
                return await allocator.CreateWorkOrderAsync(tenantId, customerId, notes: null);
            }))
            .ToArray();

        var orders = await Task.WhenAll(tasks);

        orders.Select(o => o.Number).Should().OnlyHaveUniqueItems();
        orders.Should().AllSatisfy(o => o.TenantId.Should().Be(tenantId));
    }
}
