using Microsoft.EntityFrameworkCore;
using Pymen.Api.Filters;
using Pymen.Application.Abstractions;
using Pymen.Application.Auth;
using Pymen.Application.Catalog;
using Pymen.Domain.Catalog;
using Pymen.Infrastructure.Persistence;

namespace Pymen.Api.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner));

        group.MapGet("/", async (PymenDbContext db, CancellationToken ct) =>
        {
            var items = await db.Products
                .OrderBy(p => p.Sku)
                .Select(p => new ProductResponse(p.Id, p.Sku, p.Name, p.IsRawMaterial, p.IsActive))
                .ToListAsync(ct);
            return Results.Ok(items);
        });

        group.MapGet("/{id:guid}", async (Guid id, PymenDbContext db, CancellationToken ct) =>
        {
            var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
            return product is null
                ? Results.NotFound()
                : Results.Ok(new ProductResponse(product.Id, product.Sku, product.Name, product.IsRawMaterial, product.IsActive));
        });

        group.MapPost("/", async (
                CreateProductRequest request,
                PymenDbContext db,
                ITenantContext tenant,
                CancellationToken ct) =>
            {
                var product = Product.Create(tenant.TenantId, request.Sku, request.Name, request.IsRawMaterial);
                db.Products.Add(product);
                await db.SaveChangesAsync(ct);
                return Results.Created($"/api/products/{product.Id}",
                    new ProductResponse(product.Id, product.Sku, product.Name, product.IsRawMaterial, product.IsActive));
            })
            .AddEndpointFilter<ValidationFilter<CreateProductRequest>>();

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateProductRequest request,
                PymenDbContext db,
                CancellationToken ct) =>
            {
                var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
                if (product is null)
                {
                    return Results.NotFound();
                }

                product.Update(request.Name, request.IsActive);
                await db.SaveChangesAsync(ct);
                return Results.Ok(new ProductResponse(product.Id, product.Sku, product.Name, product.IsRawMaterial, product.IsActive));
            })
            .AddEndpointFilter<ValidationFilter<UpdateProductRequest>>();

        group.MapDelete("/{id:guid}", async (Guid id, PymenDbContext db, CancellationToken ct) =>
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (product is null)
            {
                return Results.NotFound();
            }

            product.Update(product.Name, isActive: false);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return group;
    }
}
