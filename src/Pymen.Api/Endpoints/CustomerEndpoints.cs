using Pymen.Api.Filters;
using Pymen.Application.Auth;
using Pymen.Application.Catalog;

namespace Pymen.Api.Endpoints;

public static class CustomerEndpoints
{
    public static RouteGroupBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner));

        group.MapGet("/", async (CatalogService catalog, CancellationToken ct) =>
            Results.Ok(await catalog.ListCustomersAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, CatalogService catalog, CancellationToken ct) =>
        {
            var customer = await catalog.GetCustomerAsync(id, ct);
            return customer is null ? Results.NotFound() : Results.Ok(customer);
        });

        group.MapPost("/", async (CreateCustomerRequest request, CatalogService catalog, CancellationToken ct) =>
            {
                var created = await catalog.CreateCustomerAsync(request, ct);
                return Results.Created($"/api/customers/{created.Id}", created);
            })
            .AddEndpointFilter<ValidationFilter<CreateCustomerRequest>>();

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateCustomerRequest request,
                CatalogService catalog,
                CancellationToken ct) =>
            {
                var updated = await catalog.UpdateCustomerAsync(id, request, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            })
            .AddEndpointFilter<ValidationFilter<UpdateCustomerRequest>>();

        group.MapDelete("/{id:guid}", async (Guid id, CatalogService catalog, CancellationToken ct) =>
            await catalog.DeactivateCustomerAsync(id, ct) ? Results.NoContent() : Results.NotFound());

        return group;
    }
}
