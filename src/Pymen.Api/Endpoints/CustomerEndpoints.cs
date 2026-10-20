using Microsoft.EntityFrameworkCore;
using Pymen.Api.Filters;
using Pymen.Application.Abstractions;
using Pymen.Application.Auth;
using Pymen.Application.Catalog;
using Pymen.Domain.Catalog;
using Pymen.Infrastructure.Persistence;

namespace Pymen.Api.Endpoints;

public static class CustomerEndpoints
{
    public static RouteGroupBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner));

        group.MapGet("/", async (PymenDbContext db, CancellationToken ct) =>
        {
            var items = await db.Customers
                .OrderBy(c => c.Name)
                .Select(c => new CustomerResponse(c.Id, c.Name, c.TaxId, c.IsActive))
                .ToListAsync(ct);
            return Results.Ok(items);
        });

        group.MapGet("/{id:guid}", async (Guid id, PymenDbContext db, CancellationToken ct) =>
        {
            var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
            return customer is null
                ? Results.NotFound()
                : Results.Ok(new CustomerResponse(customer.Id, customer.Name, customer.TaxId, customer.IsActive));
        });

        group.MapPost("/", async (
                CreateCustomerRequest request,
                PymenDbContext db,
                ITenantContext tenant,
                CancellationToken ct) =>
            {
                var customer = Customer.Create(tenant.TenantId, request.Name, request.TaxId);
                db.Customers.Add(customer);
                await db.SaveChangesAsync(ct);
                return Results.Created($"/api/customers/{customer.Id}",
                    new CustomerResponse(customer.Id, customer.Name, customer.TaxId, customer.IsActive));
            })
            .AddEndpointFilter<ValidationFilter<CreateCustomerRequest>>();

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateCustomerRequest request,
                PymenDbContext db,
                CancellationToken ct) =>
            {
                var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
                if (customer is null)
                {
                    return Results.NotFound();
                }

                customer.Update(request.Name, request.TaxId, request.IsActive);
                await db.SaveChangesAsync(ct);
                return Results.Ok(new CustomerResponse(customer.Id, customer.Name, customer.TaxId, customer.IsActive));
            })
            .AddEndpointFilter<ValidationFilter<UpdateCustomerRequest>>();

        group.MapDelete("/{id:guid}", async (Guid id, PymenDbContext db, CancellationToken ct) =>
        {
            var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (customer is null)
            {
                return Results.NotFound();
            }

            customer.Update(customer.Name, customer.TaxId, isActive: false);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return group;
    }
}
