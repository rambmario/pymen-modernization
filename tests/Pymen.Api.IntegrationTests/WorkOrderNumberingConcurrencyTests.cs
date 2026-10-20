using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pymen.Application.Abstractions;
using Pymen.Domain.Catalog;
using Pymen.Domain.Tenancy;
using Pymen.Infrastructure.Persistence;
using Pymen.Infrastructure.Tenancy;
using Testcontainers.MsSql;

namespace Pymen.Api.IntegrationTests;

public sealed class WorkOrderNumberingConcurrencyTests : IAsyncLifetime
{
    private MsSqlContainer? _sql;
    private bool _dockerAvailable;

    public async Task InitializeAsync()
    {
        try
        {
            _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("Your_password123")
                .Build();
            await _sql.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception ex) when (ex is DockerUnavailableException || ex is AggregateException)
        {
            _dockerAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_sql is not null)
        {
            await _sql.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Parallel_creates_produce_distinct_numbers()
    {
        Skip.If(!_dockerAvailable || _sql is null, "Docker is not available for Testcontainers SQL Server.");

        var sql = _sql!;
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AmbientTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());
        services.AddScoped<IWorkOrderNumberAllocator, WorkOrderNumberAllocator>();
        services.AddDbContext<PymenDbContext>(options =>
            options.UseSqlServer(sql.GetConnectionString()));

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
                return await allocator.CreateWorkOrderAsync(tenantId, customerId);
            }))
            .ToArray();

        var orders = await Task.WhenAll(tasks);

        orders.Select(o => o.Number).Should().OnlyHaveUniqueItems();
        orders.Should().AllSatisfy(o => o.TenantId.Should().Be(tenantId));
    }
}
