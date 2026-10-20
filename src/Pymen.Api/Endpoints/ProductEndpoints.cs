using Pymen.Api.Filters;
using Pymen.Application.Auth;
using Pymen.Application.Catalog;

namespace Pymen.Api.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner));

        group.MapGet("/", async (CatalogService catalog, CancellationToken ct) =>
            Results.Ok(await catalog.ListProductsAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, CatalogService catalog, CancellationToken ct) =>
        {
            var product = await catalog.GetProductAsync(id, ct);
            return product is null ? Results.NotFound() : Results.Ok(product);
        });

        group.MapPost("/", async (CreateProductRequest request, CatalogService catalog, CancellationToken ct) =>
            {
                var created = await catalog.CreateProductAsync(request, ct);
                return Results.Created($"/api/products/{created.Id}", created);
            })
            .AddEndpointFilter<ValidationFilter<CreateProductRequest>>();

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateProductRequest request,
                CatalogService catalog,
                CancellationToken ct) =>
            {
                var updated = await catalog.UpdateProductAsync(id, request, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            })
            .AddEndpointFilter<ValidationFilter<UpdateProductRequest>>();

        group.MapDelete("/{id:guid}", async (Guid id, CatalogService catalog, CancellationToken ct) =>
            await catalog.DeactivateProductAsync(id, ct) ? Results.NoContent() : Results.NotFound());

        return group;
    }
}
