using Microsoft.EntityFrameworkCore;
using Pymen.Application.Abstractions;
using Pymen.Domain.Catalog;

namespace Pymen.Application.Catalog;

public sealed class CatalogService
{
    private readonly IPymenDbContext _db;
    private readonly ITenantContext _tenant;

    public CatalogService(IPymenDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<CustomerResponse>> ListCustomersAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Customers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CustomerResponse(c.Id, c.Name, c.TaxId, c.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerResponse?> GetCustomerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return customer is null
            ? null
            : new CustomerResponse(customer.Id, customer.Name, customer.TaxId, customer.IsActive);
    }

    public async Task<CustomerResponse> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = Customer.Create(_tenant.TenantId, request.Name, request.TaxId);
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);
        return new CustomerResponse(customer.Id, customer.Name, customer.TaxId, customer.IsActive);
    }

    public async Task<CustomerResponse?> UpdateCustomerAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        customer.Update(request.Name, request.TaxId, request.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
        return new CustomerResponse(customer.Id, customer.Name, customer.TaxId, customer.IsActive);
    }

    public async Task<bool> DeactivateCustomerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (customer is null)
        {
            return false;
        }

        customer.Update(customer.Name, customer.TaxId, isActive: false);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ProductResponse>> ListProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Products
            .AsNoTracking()
            .OrderBy(p => p.Sku)
            .Select(p => new ProductResponse(p.Id, p.Sku, p.Name, p.IsRawMaterial, p.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductResponse?> GetProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return product is null
            ? null
            : new ProductResponse(product.Id, product.Sku, product.Name, product.IsRawMaterial, product.IsActive);
    }

    public async Task<ProductResponse> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = Product.Create(_tenant.TenantId, request.Sku, request.Name, request.IsRawMaterial);
        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);
        return new ProductResponse(product.Id, product.Sku, product.Name, product.IsRawMaterial, product.IsActive);
    }

    public async Task<ProductResponse?> UpdateProductAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.Update(request.Name, request.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
        return new ProductResponse(product.Id, product.Sku, product.Name, product.IsRawMaterial, product.IsActive);
    }

    public async Task<bool> DeactivateProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return false;
        }

        product.Update(product.Name, isActive: false);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
