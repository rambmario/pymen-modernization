using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Pymen.Application.Catalog;
using Pymen.Application.Tests.Infrastructure;
using Pymen.Domain.Tenancy;

namespace Pymen.Application.Tests.Catalog;

public sealed class CatalogServiceTests
{
    [Fact]
    public async Task Create_and_get_customer_roundtrips()
    {
        await using var db = CreateDb();
        var tenant = Tenant.Create("Demo", "1");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var tenantContext = new TestTenantContext();
        tenantContext.SetTenant(tenant.Id);
        var sut = new CatalogService(db, tenantContext);

        var created = await sut.CreateCustomerAsync(new CreateCustomerRequest("Cliente Test", "20-11111111-1"));
        var loaded = await sut.GetCustomerAsync(created.Id);

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Cliente Test");
        loaded.TaxId.Should().Be("20-11111111-1");
    }

    [Fact]
    public async Task Get_missing_customer_returns_null()
    {
        await using var db = CreateDb();
        var sut = new CatalogService(db, new TestTenantContext());

        var result = await sut.GetCustomerAsync(Guid.CreateVersion7());
        result.Should().BeNull();
    }

    [Fact]
    public async Task Deactivate_product_marks_inactive()
    {
        await using var db = CreateDb();
        var tenant = Tenant.Create("Demo", "2");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var tenantContext = new TestTenantContext();
        tenantContext.SetTenant(tenant.Id);
        var sut = new CatalogService(db, tenantContext);

        var product = await sut.CreateProductAsync(new CreateProductRequest("SKU-1", "Producto", false));
        var ok = await sut.DeactivateProductAsync(product.Id);
        var loaded = await sut.GetProductAsync(product.Id);

        ok.Should().BeTrue();
        loaded!.IsActive.Should().BeFalse();
    }

    private static TestPymenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestPymenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new TestPymenDbContext(options);
    }
}
